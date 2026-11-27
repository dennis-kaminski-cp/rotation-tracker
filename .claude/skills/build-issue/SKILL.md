---
name: build-issue
description: Implement a GitHub issue's approved spec and design in its own worktree (BDD outside, TDD inside), review it, and open a draft PR; resume a build or work review comments on its PR. Use when told to build or implement an issue, or by work-issue, refine-issue or design-issue.
---

# Build issue

Input: an issue number N whose spec (and design, unless refine skipped it) the owner approved. Build implements decisions; it does not make them.

Read `docs/agent-workflow.md` first: its **Rules for agent runs** apply throughout.

## 1. Read and claim

```sh
gh issue view N --json number,title,body,state,author,labels,comments,subIssues,parent
gh pr list --state open --search "N in:body" --json number,headRefName,isDraft,url
```

Entry stages: waiting `stage:ready` (no sub-issues; a parent goes to `orchestrate-issue`), in progress `stage:building`, waiting `stage:review`. Run the entry check.

- **New build** (`stage:ready`, no open PR): concurrency check first: `gh issue list --label stage:building --json number`. Two or more: ask **build anyway** / **stop**. Then claim to `stage:building`, title the thread `[build]: #N <title>`, go to 2.
- **Resume** (`stage:building`, or an open PR for N): title the thread, go to 2 with the existing branch, then pick up where the branch stands (commits, `dotnet test`) or, with review comments on the PR, go to 6.

## 2. Worktree

Builds never happen in the project root. Check `t3_worktree_status`:

- Already in a worktree on this issue's branch (a thread `orchestrate-issue` launched, or after a handoff): continue in 3.
- Otherwise move the thread, as the **last call of the turn** (the move ends this session):
  - New build: `t3_worktree_handoff` with `branch` `<type>/N-<slug>` (`feat` for feature, `fix` for bug, `chore` for chore; slug from the title, ≤ 5 words), `baseRef` `main`, `startFromOrigin` true.
  - Resume: `branch` the PR's `headRefName`, `baseRef` `origin/<headRefName>`, `startFromOrigin` true. If the handoff refuses (branch exists), show the error and ask.
  - `continuationPrompt`: `Continue the build-issue skill for issue #N.`

No T3 tools: show the branch you would create and ask the owner to start a worktree thread.

## 3. Build

Inputs: Scenarios, Decisions, and the `## Design` section (Domain signatures, Data, Slices, UI, Tests, ADR). Follow `CLAUDE.md` (**Feature workflow**: BDD outside, TDD inside; structure; hard rule: metadata only):

1. Write the `.feature` file from the Scenarios and its steps; run it, it must fail for the right reason (red).
2. Unit tests for each invariant listed under Tests, then the domain code until they pass.
3. Migration when Data says so (`dotnet ef migrations add <Name> ...`, name from the design).
4. The slices and pages until the scenarios pass (green).
5. `CONTEXT.md` lines from Tests → Glossary; the ADR to `docs/adr/` when the design names one.
6. Refactor; `dotnet test` (needs Docker; not running: ask the owner to start it) green.

Commit in small conventional commits (`feat:`, `test:`, ...) without issue numbers.

**Deviations.** When the design doesn't hold up against the code (a signature won't work, a pattern conflicts): a small one you decide, and note it for the PR. One that changes Scope, Scenarios, the domain model or a page: stop and ask **adjust and continue** (edit the issue's Design, add a line under Decisions) / **back to design** (`stage:refined`, offer to run `design-issue` here).

## 4. Review

Inner loop, at most 2 rounds: a review subagent that hasn't seen your reasoning (`git-diff-code-reviewer`, sonnet) gets the diff against `origin/main`, the issue's spec and design, `CLAUDE.md`, and checks correctness, the hard rule, the patterns, and spec coverage (every scenario in the feature file, every invariant tested). Fix CRITICAL and HIGH, rerun `dotnet test`. Stop when none are left or after round 2; what is left goes into the PR.

## 5. Draft PR

`pull_request_create` (it pushes and links; no T3: `git push -u origin HEAD` and `gh pr create --draft --body-file`), `draft` true, title as a conventional commit (`feat: <what>`), body:

```markdown
Closes #N

## What
Two or three lines, in glossary terms.

## Deviations
From the approved design, each with why. Or "none".

## Review
Findings left after the inner review, or "none".

## Check it
`/preview`, then: one line per scenario, what to click and what to see.
```

Then `stage:building` → `stage:review` and comment on the issue: "Draft PR #<pr> is up for review." Show the PR link and the CI state (`gh pr checks`, once; no waiting loop). Ask: **start a preview** (run the `preview` skill here) / **stop**. Merging is the owner's.

## 6. Review comments

The owner reviews on GitHub and adds `agent` (or asks in this thread). Read the PR's reviews and inline comments:

```sh
gh pr view <pr> --json reviews,comments
gh api repos/{owner}/{repo}/pulls/<pr>/comments
```

The owner's comments are the decisions; others' are context. Set `stage:building`, work each comment (same build and review rules), push, reply on each comment with what changed (or why not), back to `stage:review`. A comment that changes Scope or Design: ask as in Deviations.

Done when the draft PR exists with `Closes #N`, `dotnet test` was green on the last commit, the issue is in `stage:review` with the comment, and the worktree has no uncommitted changes.

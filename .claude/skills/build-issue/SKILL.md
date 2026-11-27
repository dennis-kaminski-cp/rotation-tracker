---
name: build-issue
description: Build one GitHub issue's approved spec and design into a pull request in its own worktree: early draft PR, the build per CLAUDE.md, a fresh-context review with at most 2 rounds, then a ready PR; resume a build or work review comments on its PR. Use when told to build or implement an issue, or by work-issue, refine-issue, design-issue or a parent's orchestrate-issue thread.
---

# Build issue

Input: an issue number N whose spec (and design, unless refine skipped it) the owner approved. One issue, one worktree, one PR. Build implements decisions; it does not make them. How to write the code is `CLAUDE.md`'s job, not this file's.

Read `docs/agent-workflow.md` first: its **Rules for agent runs** apply throughout.

**Started by a parent's thread** (`orchestrate-issue`; its thread id is in your brief): every report section 5 and 6 ask for also goes to that thread, once, at the end, with the T3 tool `t3_thread_send` (mode `queue`): PR link, review verdict, commits pushed after the review, the Noticed list, what is open. The parent doesn't poll; a report you don't send stalls it.

## 1. Read and check

```sh
gh issue view N --json number,title,body,state,author,labels,comments,subIssues,parent,blockedBy,closedByPullRequestsReferences
gh pr list --state open --json number,headRefName,isDraft,url --jq '[.[] | select(.headRefName | test("^[a-z]+/N-"))]'
```

N's PRs are its open PRs on a `<type>/N-…` branch (section 2) plus `closedByPullRequestsReferences`. A PR closed unmerged is an abandoned build: ignore it. Never search PR bodies for N: that matches every PR that mentions the number.

Entry stages: waiting `stage:ready` (no sub-issues; a parent goes to `orchestrate-issue`), in progress `stage:building`, waiting `stage:review`. Run the entry check. A sub-issue: read the parent too; its Decisions are part of the spec.

Ask (**go on** / **stop**, naming what holds) when:

- an issue it is blocked by is still open;
- a PR in `closedByPullRequestsReferences` is already merged;
- it is a sub-issue, a sibling before it in the parent's order is still open, and no parent thread started you;
- two or more other issues are already in `stage:building` (`gh issue list --label stage:building --json number`).

**Resume** when an open PR for N exists: its branch is the build. Any PR on another branch than the one you would resume: show it and ask. No open PR: a new build, whatever the stage.

Claim to `stage:building` (a resume of a PR in review too), title the thread `[build]: #N <title>`.

## 2. Worktree

Builds never happen in the project root. Check `t3_worktree_status`:

- In a worktree on this issue's branch (a thread the parent launched, or after a handoff): new build → `git fetch origin`, `git status` clean and `git log origin/main..HEAD` empty, otherwise stop and say the build needs a fresh worktree. Go to 3.
- In any other worktree: the handoff can't move a thread that already has one. Say the run has to start from the project root, and stop.
- In the project root: move the thread, as the **last call of the turn** (the move ends this session), `continuationPrompt` `Continue the build-issue skill for issue #N.`:
  - New build: `t3_worktree_handoff`, `branch` `<type>/N-<slug>` (`feat` for feature, `fix` for bug, `chore` for chore; slug ≤ 5 words from the title), `baseRef` `origin/main`, `startFromOrigin` true.
  - Resume: `branch` the PR's `headRefName`, `baseRef` `origin/<headRefName>`, `startFromOrigin` true. If the handoff refuses, show the error and ask.

No T3 tools: show the branch you would create and ask the owner to start a worktree thread.

## 3. Build

Follow `CLAUDE.md` fully: the feature workflow (BDD outside, TDD inside), structure, the hard rule, ownership. What the issue adds:

- The Scenarios become the `.feature` file; Design → Tests lists the unit tests, the `CONTEXT.md` lines and the ADR; Design → Data names the migration.
- **Early draft PR.** After the first commit (the failing feature file), push and open a draft PR (`pull_request_create`, `draft` true; no T3: `git push -u origin HEAD` and `gh pr create --draft --body-file`), title as a conventional commit, body `Closes #N` and the sections of section 4 as "TBD". CI then runs on every push.
- Commits: small, conventional, no issue numbers. Push as you go.
- **Deviations.** The design doesn't hold up against the code: a small one you decide and list in the PR. One that changes Scope, Scenarios, the domain model or a page: stop and ask **adjust and continue** (edit the issue's Design, add a line under Decisions) / **back to design** (`stage:refined`, offer to run `design-issue` here). The answer goes into the issue before the build goes on.

## 4. PR body

Fill these before the review and keep them current:

```markdown
Closes #N

## Summary
The smallest view that makes the change clear: a call tree, file tree or diff sketch. No prose recap.

## Evidence
`dotnet test` result; which scenarios pass; screenshots or the flow driven where a page changed.

## Deviations
From the approved design, each with why. Or "none".

## Merge danger
Door: one-way (a migration that drops or rewrites data) or two-way. Blast radius: one line. For a one-way door: what to watch on merge.

## Noticed
Outside the task, not touched: at most 3 by impact, each with a proposed issue or why not; nits in one line. Or "none". Never security findings (public repo).
```

## 5. Review

After `dotnet test` is green and everything is pushed, start a fresh-context reviewer: Agent tool, `subagent_type` `general-purpose`, `model` `opus`, `effort` `high`, foreground (pinned here on purpose: it overrides any model-by-task default, and works from any thread model). Give it only this prompt, so it is not steered by your reasoning:

> Review PR <url> in the worktree at `<path>` with the `review-pr` skill. Return all findings verbatim.

By severity:

- **CRITICAL, HIGH**: fix each (tests never weakened to satisfy a finding), rerun `dotnet test`, push, and review again with a **new** reviewer. Only these start another round. At most 2 rounds; still open after the second: fix what you can, push, and name what is open and unreviewed.
- **MEDIUM**: fix it when it is in code this PR changes; otherwise it goes under Noticed.
- **LOW**: fix it only when it is in code this PR changes and takes a line or two, in the same push as other fixes (after the last round: one push, no further review). Everything else goes into your report in the thread, not the PR. LOWs never start a round, and you don't ask about them.
- A finding you disagree with: say why in your report; no change.
- Security findings: tell the owner in the thread only.

## 6. Finish

Check that `gh pr view <pr> --json closingIssuesReferences` lists N; if not, put `Closes #N` back at the top of the body. Mark the PR ready (`gh pr ready <pr>`), set `stage:building` → `stage:review`, comment on the issue "PR #<pr> is ready for review." Report in the thread: PR link, CI state (`gh pr checks`, once, no waiting loop), the rounds (findings per round, what you fixed), commits after the last review, the LOWs you left, the Noticed list.

Then ask, in one `AskUserQuestion` call: **start a preview** (run the `preview` skill here) / **stop**; and, when Noticed proposes issues, which to file (multi-select). File each chosen one after a duplicate search (`gh issue list --search "<words>"`): `gh issue create --label <type>,stage:inbox`. Started by a parent's thread: ask nothing; the report goes there and the parent asks. Merging is the owner's; never merge, never watch the PR.

## 7. Review comments

The thread stays the place for follow-ups. The owner reviews on GitHub and adds `agent` to the issue (or asks here). Read the PR's reviews and inline comments:

```sh
gh pr view <pr> --json reviews,comments
gh api repos/{owner}/{repo}/pulls/<pr>/comments
```

The owner's comments are the decisions; others' are context. Set `stage:building`, work each one under section 3's rules, `dotnet test`, push, reply on each comment with what changed (or why not, leaving it open for the owner). Never resolve a thread yourself. A comment that changes Scope or Design: ask as in Deviations. Then back to `stage:review` and report as in 6.

Done when the PR is ready, its `closingIssuesReferences` list N, every section is filled, `dotnet test` was green on the last commit, the issue is in `stage:review` with the comment, the worktree has no uncommitted changes, and the report is in the thread (and in the parent's thread, if one started you).

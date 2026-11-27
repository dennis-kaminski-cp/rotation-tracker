---
name: orchestrate-issue
description: Carry a parent GitHub issue's sub-issues to done in build order from the parent's thread: refine and design each one here just in time, start each build as its own worktree thread (at most 2 at once), collect their reports, close the parent when all are done. Use when told to orchestrate or work a parent issue, or by work-issue or refine-issue for an approved split.
---

# Orchestrate issue

Input: a parent issue N in `stage:ready` whose split the owner approved. This thread is the group's home: sub-issues are refined and designed here, one at a time, just in time (what the earlier ones become changes the later ones); builds run as separate worktree threads so they can run in parallel and report back here.

Read `docs/agent-workflow.md` first: its **Rules for agent runs** apply throughout.

## 1. Read

```sh
gh issue view N --json number,title,body,state,author,labels,subIssues
gh issue view <M> --json number,title,state,labels,blockedBy      # each sub-issue
gh pr list --state all --search "<M> in:body" --json number,state,isDraft,url
```

Entry stage: `stage:ready` with sub-issues. The parent keeps `stage:ready` while it is worked; there is no claim beyond removing `agent`. Run the entry check, title the thread `[orchestrate]: #N <title>`.

The order is the one in the parent body; blocked-by edges are the hard constraints within it.

## 2. Status

Show the group (`send_to_user`), one line per sub-issue in order: number, title, stage, PR and its state, open blockers, build thread link. Refresh it after every step below.

## 3. Next step

Take the first open sub-issue M in order that can move, and act by its stage:

| Stage of M | Act |
|---|---|
| `stage:inbox`, `stage:refining` | run `refine-issue` for M here |
| `stage:refined`, `stage:designing` | run `design-issue` for M here, once every blocker of M has a PR (its design builds on theirs) |
| `stage:ready` | start its build (section 4), once every blocker is closed (merged) and fewer than 2 builds run |
| `stage:building`, `stage:review` | nothing: its build thread owns it |

Several can move: refine and design the next ones while builds run. Nothing can move: say what the group waits on (a build, a merge, the owner) and end the turn.

**Stage skills run for a sub-issue here** follow their own rules (questions, approval, stage labels) with one change: their **continue here** comes back to this section instead of running the next skill. A build never moves this thread into a worktree; it gets its own thread.

## 4. Start a build

```
t3_thread_launch
  title: [build]: #M <title>
  workspaceStrategy: { type: worktree, branch: <type>/M-<slug>, baseRef: origin/main, startFromOrigin: true }
  message: Use the build-issue skill for issue #M. Started by the parent thread <this thread's id> (orchestrate-issue for #N): send your reports there.
```

Same branch naming as `build-issue`. Show its link in the status. Don't poll it: its report arrives here as a new message.

## 5. Reports

A build thread's report (PR link, verdict, open points, Noticed): put it in the status, pass open points and Noticed on to the owner, and go to section 3. The PR is the owner's to review and merge; a merge closes M, which can unblock the next one. When the owner says a PR is merged (or `agent` on the parent starts a new run), read again from section 1.

Review comments on a sub-issue's PR go to its own build thread (the owner adds `agent` on M, or writes there), not here.

## 6. Close

All sub-issues closed: ask **close #N** / **not yet** (naming the sub-issues and their PRs). On close: comment "All sub-issues are done." and `gh issue close N`.

Stop at any point: the parent stays in `stage:ready`; `agent` on it resumes from section 1 in a new thread.

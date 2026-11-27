---
name: orchestrate-issue
description: Carry a parent GitHub issue's sub-issues to done in build order from the parent's thread: refine and design each one here just in time, start each build as its own worktree thread (at most 2 at once), hand over their PRs, close the parent when all are done. Use when told to orchestrate or work a parent issue, or by work-issue or refine-issue for an approved split.
---

# Orchestrate issue

Input: a parent issue N in `stage:ready` whose split the owner approved. This thread is the group's home and writes no code: sub-issues are refined and designed here, one at a time, just in time (what the earlier ones become changes the later ones); builds run as separate worktree threads so they can run in parallel and report back here.

Read `docs/agent-workflow.md` first: its **Rules for agent runs** apply throughout.

## 1. Read

```sh
gh issue view N --json number,title,body,state,author,labels,subIssues
gh issue view <M> --json number,title,state,labels,blockedBy      # each sub-issue
gh pr list --state all --search "<M> in:body" --json number,state,isDraft,url
```

Entry stage: `stage:ready` with sub-issues. The parent keeps `stage:ready` while it is worked; there is no claim beyond removing `agent`. Run the entry check, title the thread `[orchestrate]: #N <title>`. Link only N to this thread, never a sub-issue or a PR: a linked PR settles the thread when it merges.

Order: the one in the parent body; blocked-by edges are the hard constraints within it. No order stated, or an open sub-issue missing from it: ask, your proposed order first.

## 2. Status

Show the group (`send_to_user`), one line per sub-issue in order: number, title, stage, PR and its state, open blockers, build thread link. Refresh it after every step below.

## 3. Next step

Take the first open sub-issue M in order that can move, and act by its stage:

| Stage of M | Act |
|---|---|
| `stage:inbox`, `stage:refining` | run `refine-issue` for M here |
| `stage:refined`, `stage:designing` | run `design-issue` for M here, once every blocker of M has a PR (its design builds on theirs) |
| `stage:ready` | start its build (section 4), once every blocker is closed (merged) and fewer than 2 builds run |
| `stage:building`, `stage:review` | nothing: its build thread owns it; an open PR with no build thread of this run goes to section 5 |

Several can move: refine and design the next ones while builds run. Nothing can move: say what the group waits on (a build, a merge, the owner) and end the turn.

**Stage skills run for a sub-issue here** follow their own rules (questions, approval, stage labels) with one change: their **continue here** comes back to this section instead of running the next skill. A build never moves this thread into a worktree; it gets its own thread.

**After a merge**, before M's build starts: if the merged PR touched what M builds on, recheck M against `main` (`refine-issue`'s "already drafted" check, and its Design); changes go through the owner as usual.

**A spike that kills a sub-issue**: close it as not planned with the result, propose the replacement here and ask; create it after the owner agrees, and update the parent's list and order.

## 4. Start a build

This thread's id: `currentThreadId` from T3 `t3_thread_list`.

```
t3_thread_launch
  title: [build]: #M <title>
  workspaceStrategy: { type: worktree, branch: <type>/M-<slug>, baseRef: origin/main, startFromOrigin: true }
  message: Use the build-issue skill for issue #M. Started by the parent thread <currentThreadId> (orchestrate-issue for #N): send your reports there.
```

Same branch naming as `build-issue`. Add to the message only what the build can't read from the issues (a decision made in this thread); never the build steps. Then end the turn: no `t3_thread_wait`, no reads of the build thread for progress. Its report arrives here as a new message. No T3 tools: tell the owner to add `agent` to M instead.

## 5. Hand over

A build thread's report wakes this thread; it is a wake-up, not evidence or approval. Check the PR yourself (`gh pr view <pr> --json state,isDraft`, `gh pr checks <pr>`, once), then give the owner its link, the review verdict, CI state, open points and the Noticed list, and update the status.

- CI red: send the build thread back to fix it (`t3_thread_send`, mode `queue`) and end the turn until it reports again.
- LOW findings are the build's call: don't triage them, and never send a build back for one.
- A commit pushed after its review: pass it on as reported; the verdict doesn't cover it.

The PR is the owner's to review and merge. Review comments go to its build thread (the owner adds `agent` on M, or writes there), not here. Nothing wakes this thread on a merge: the owner says so here (or `agent` on N starts a new run); confirm with `gh pr view <pr> --json state`, then read again from section 1.

**A build that stops without a report** (a crash, a question in its own thread) leaves this thread idle. When the owner says "check the build", read that thread once (`t3_thread_read`):

- a question: tell the owner it waits for them there; end the turn;
- a ready PR: hand over as above;
- a draft PR or no PR (stopped half-way): ask **resume it** (`t3_thread_send` "Continue the build-issue skill for issue #M.") / **start a new build thread** / **stop the group**.

## 6. Close

All sub-issues closed: ask **close #N** / **not yet** (naming the sub-issues and their PRs). On close: comment "All sub-issues are done." and `gh issue close N`.

Stop at any point: the parent stays in `stage:ready`; `agent` on it resumes from section 1 in a new thread.

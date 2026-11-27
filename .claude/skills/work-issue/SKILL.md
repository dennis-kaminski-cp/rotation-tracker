---
name: work-issue
description: Pick up a GitHub issue wherever it stands (refine, design, build, or orchestrate its sub-issues) and carry it forward in this thread. Use when told to work on an issue, or by the `agent` label webhook.
---

# Work issue

Input: an issue number N. The single entry point to the issue workflow: it reads where N stands and runs the matching stage skill; each stage skill then offers to continue with the next one here.

Read `docs/agent-workflow.md` first: its **Rules for agent runs** apply throughout.

```sh
gh issue view N --json number,title,state,author,labels,subIssues
```

Run the entry check, remove the `agent` label if present, and link the thread to the issue (see the rules); each stage skill sets the thread title.

Another thread on N: T3 `t3_thread_list` with `titleContains: "#N "` (trailing space) and `settled: false`. One besides this one: name it (its link) and ask **go on here** / **stop**.

Route by stage label:

| Stage | Run |
|---|---|
| `stage:inbox`, `stage:refining`, none | `refine-issue` |
| `stage:refined`, `stage:designing` | `design-issue` |
| `stage:ready`, issue has sub-issues | `orchestrate-issue` |
| `stage:ready`, no sub-issues | `build-issue` |
| `stage:building`, `stage:review` | `build-issue` (it resumes the existing branch or PR and works review comments) |

More than one stage label, or a stage that contradicts the issue (e.g. `stage:ready` with no spec): show what you see and ask which stage to run.

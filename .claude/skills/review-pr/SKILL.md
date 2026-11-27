---
name: review-pr
description: Review a pull request or branch against this repo: correctness, spec and design match, DDD fundamentals, codebase patterns, pattern drift and reuse. Read-only, returns findings by severity. Use when asked to review a PR, a branch or local changes in this repo, and by build-issue's reviewer.
---

# Review PR

Input: a PR number, or a branch (base `origin/main`). Read-only: no edits, no commits, no GitHub writes. The output is the findings list.

The build already enforces style, sealed classes, layering, slice isolation, `@code` in `.razor`, banned APIs and complexity limits (`.editorconfig`, `Directory.Build.props`, `BannedSymbols.txt`, `CodeMetricsConfig.txt`, `ArchitectureTests.cs`). Don't report what CI catches; spend the review on what needs judgement.

## 1. Read

- The diff: `gh pr diff <n>` or `git diff origin/main...HEAD`. Then the full files it touches, not just the hunks.
- The issue the PR closes (`Closes #N`): the spec (Scope, Scenarios, Decisions) and `## Design`. A sub-issue: the parent's Decisions too. No issue: review against `CLAUDE.md` only and say so.
- `CLAUDE.md` and `CONTEXT.md`.
- **The baseline**: for each kind of code the PR adds (aggregate, value object, handler, page, step file, migration, test), open the closest existing example on `origin/main`. The patterns are whatever the existing code does, not what this file says; this file only says where to look.

## 2. Correctness

Run the `code-review` skill at `high` on the PR (or the branch). Its important findings are at least HIGH, its nits LOW, pre-existing ones go under Noticed.

## 3. Spec and design

- Every scenario in the spec has a matching Gherkin scenario in `Features/<Area>/<UseCase>.feature`, in domain language.
- Every invariant in the design has a unit test in `tests/RotationTracker.Tests`, mirroring the `Domain/` folder.
- What the PR does beyond the spec, or differently from the design, is stated under Deviations in the PR body. Unstated: HIGH.

## 4. DDD fundamentals

- **Invariants live in the domain.** Aggregates validate in their factory (`Project.Create`) and their methods, so an object can't exist in or move into an invalid state. A rule checked in a handler or page instead: HIGH. The exception is a rule that needs the database (uniqueness): the handler checks it.
- **Encapsulation.** State changes through methods named for domain behaviour, not public setters. Private setters and a private constructor, as in `Project`. A public setter on domain state: HIGH.
- **Aggregate boundaries.** One aggregate per transaction where possible. Another aggregate is referenced by its Id, not by a navigation that lets one aggregate change another. Children change only through their root.
- **Value objects.** A concept with its own rules, or several fields that belong together (an interval, a location), is an immutable value object compared by value, not loose primitives repeated across entities. Repeated validation of the same primitive is a sign one is missing.
- **Ubiquitous language.** Type, method, property, page and scenario names use `CONTEXT.md` terms; its _Avoid_ words are a finding. A newly settled term missing from `CONTEXT.md`: MEDIUM.
- **Persistence ignorance.** No EF attributes or `DbContext` in `Domain/`; mapping goes in `Infrastructure/`. The architecture test covers the reference; you cover the leaks, e.g. a constructor or setter added only for EF.
- **No anemic model.** Logic about a domain object belongs on that object, not in a handler or a static helper.

## 5. Codebase patterns and drift

Compare each new piece with its baseline from step 1. A different way of doing the same thing is drift: MEDIUM, HIGH when it splits the codebase (two error styles, two ways to load data). Name the baseline file in the finding. Drift that's deliberate and stated in the design or Deviations is fine; if it should become the new pattern everywhere, say so under Noticed.

Check at least:

- **Slice shape**: one folder per area under `Features/<Area>/`, handler and page together, the handler a `sealed class` with a `Handle` method. No MediatR, and no repository or service layer the existing slices don't have.
- **Data access**: `IDbContextFactory<AppDbContext>`, one context per handler call, `CancellationToken` passed through.
- **Results and errors**: the way existing handlers return outcomes (today a result record with an `Error` message, the domain's `ArgumentException` caught in the handler). A new style (exceptions up to the page, a `Result<T>` type) is drift.
- **Pages**: logic in the `.razor.cs` code-behind, `@inject` in the `.razor`, forms and validation shown the way the existing pages do it.
- **Tests**: unit tests named `Method_Behaviour`. E2E steps drive the browser only (no handler or database calls), scenarios are independent, and steps live in `<Area>Steps.cs` next to the feature.
- **Time and config**: `TimeProvider` injected, settings through options, nothing hard-coded that differs per environment.
- **Migrations**: generated, named for the change, matching the model, and safe to apply on startup to a database with data (no dropped column holding data, defaults for new non-null columns).

## 6. Reuse

For each new type, method and helper, search the repo for something that already does it or most of it: a value object, a validation, a query, a component, a step definition. Report:

- **Duplicate** of existing code: MEDIUM, naming the file to reuse.
- **Two new things that are the same**, inside the PR: MEDIUM.
- **An extraction that pays now**: only when the same logic exists at least twice after this PR. One use is no reason for an abstraction (`CLAUDE.md`: no speculative abstractions). A new interface with one implementation, a generic base or a factory for one product is itself a finding: LOW, MEDIUM when other code is shaped around it.
- **Gherkin**: a new step that rewords an existing one: LOW, reuse the existing wording.

## 7. Hard rule and security

- No credential values anywhere: fields, logs, error messages, fixtures, seed data, test names. Placeholders are obviously fake. A violation: CRITICAL.
- User input validated at the boundary, no raw SQL built from input, no secrets in committed config.
- Security findings go only into your returned list, marked **security**, never into a GitHub comment. The caller tells the owner in the thread.

## Output

Return all findings, most severe first, each as:

```
[SEVERITY] <correctness|spec|ddd|pattern|drift|reuse|security> file:line
<the problem in a sentence> -> <the fix in a line> (baseline: <file>, for drift and reuse)
```

- CRITICAL: hard rule, security hole, data loss.
- HIGH: bug, missing scenario or invariant test, domain rule outside the domain, unstated deviation, drift that splits the codebase.
- MEDIUM: drift, duplicate, missing glossary term, a missing value object.
- LOW: naming, small reuse, nits.

Then **Noticed**: pre-existing problems and patterns worth changing repo-wide, outside this PR. Then one line: the verdict (no CRITICAL or HIGH: approve) and what you didn't check.

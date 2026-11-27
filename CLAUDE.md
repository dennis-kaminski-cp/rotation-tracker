# CLAUDE.md

## Purpose

Learning project for AI-assisted coding and AI automation. The vehicle is a small credential rotation tracker (see README.md).

## Hard rule: no secrets

The app tracks rotation **metadata** only: policy, schedule, owner, and the locations (code/config/vault/pipeline) where a credential must be updated. Never add fields, storage, logging, or features that hold actual credential values, even "temporarily" or for tests. Use obviously fake placeholders in fixtures.

## Domain language

`CONTEXT.md` is the glossary. Use its terms in code, feature files and conversation, and update it when a term is settled while building a feature.

## Stack

.NET 10 Blazor Server (`src/RotationTracker`), EF Core on Postgres. Docker Compose locally, Kubernetes later. Docker must be running for E2E tests and Compose.

```sh
dotnet build
dotnet test                                      # unit + E2E (E2E starts its own Postgres container)
dotnet test tests/RotationTracker.Tests          # unit only, no Docker needed
dotnet format                                    # fix formatting and style; CI runs it with --verify-no-changes
docker compose up -d --build                     # app on http://localhost:8080, health at /healthz
docker compose up -d db && dotnet run --project src/RotationTracker   # app on host, db in Docker
dotnet ef migrations add <Name> -p src/RotationTracker -o Infrastructure/Migrations
```

Migrations run on app startup.

Lint: `.editorconfig` holds the style rules, `Directory.Build.props` makes every warning a build error (analyzers at `latest-recommended`, code style enforced in the build). Fix the code, don't suppress: a rule that's wrong for this repo gets changed in `.editorconfig`, with the owner's OK.

## Structure: DDD + vertical slices

- `Domain/`: aggregates and value objects. Invariants live here (e.g. `Project.Create` validates), not in handlers.
- `Features/<Area>/`: one slice per use case: handler + Razor page together. Handlers coordinate; no MediatR.
- `Infrastructure/`: `AppDbContext`, EF mappings, migrations.

## Tests

- `tests/RotationTracker.Tests`: xUnit unit tests for `Domain/` invariants. Mirror the `Domain/` folder.
- `tests/RotationTracker.E2E`: Reqnroll (Gherkin) + Playwright, against the real app and a Testcontainers Postgres.
  - `Features/<Area>/<UseCase>.feature` plus a `<Area>Steps.cs` next to it.
  - Steps drive the browser only; never set up data by calling handlers or the database directly.
  - The database is truncated before each scenario, so scenarios must not depend on each other.

## Feature workflow (BDD outside, TDD inside)

1. Write or extend the `.feature` file with scenarios in domain language. Run it: it must fail (red).
2. Write failing unit tests for any new domain rule, then implement the domain code until they pass.
3. Build the slice (handler + page) until the scenarios pass (green).
4. Refactor, run `dotnet test`. Done means the whole suite passes.

## Git flow

`main` is protected: work on a branch, open a PR, and merge (squash) only when the `build-and-test` CI check (`.github/workflows/ci.yml`) is green.

Work items are GitHub issues; labels, stages and the agent triggers are in `docs/agent-workflow.md`.

## Worktrees and parallel runs

- E2E runs are fully isolated: each run starts its own Postgres container and hosts the app on a random port. Several worktrees can run `dotnet test` at the same time.
- Docker Compose: the project name is the folder name, so each worktree gets its own containers and volume. Set host ports per worktree in a git-ignored `.env` (`APP_PORT=8081`, `DB_PORT=5433`).
- `dotnet run` in a worktree: pass `--urls http://localhost:<port>` and point `ConnectionStrings__Default` at that worktree's `DB_PORT`.

## Working style

- Keep it small. Prefer the simplest thing that works; no speculative abstractions.
- Build features as thin vertical slices, one at a time.

## Ownership

You own this code; you're not a guest in it. Notice what's worse than it should be (bugs, misleading names, dead code, missing tests, stale docs or rules, awkward designs, a spec that looks wrong) and speak up, unasked: staying silent is the failure, a proposal the owner turns down costs nothing.

- **Inside the task**, fix it in the diff. **Outside it**, propose it (below): no drive-by fix, and don't drop it either. Lead with at most 3 by impact, nits in one line.
- **Disagree once**, with your pick and how sure you are. Once the owner decides, build it fully and don't re-argue.
- **The harness counts too**: if `CLAUDE.md`, a skill or the docs steered you wrong, propose the fix.
- End every task report (and a PR's `## Noticed`) with what you noticed but didn't touch.
- **Found mid-task**: search `gh issue list --search "<words>"` for a duplicate, then propose the issue (title, type label, body) in chat. Create it only after the owner approves, in `stage:inbox`.
- **Security-sensitive findings never go on GitHub** (public repo: no issue, PR body, review or comment). Tell the owner in the thread only.

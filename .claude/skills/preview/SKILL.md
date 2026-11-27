---
name: preview
description: Start the Rotation Tracker app from the current checkout in an isolated Docker Compose preview (own containers, free ports, empty database) so a human can click through an agent's feature-branch changes before merging to main. Use when the user says "preview", "start a preview", "let me try the branch", "spin up the app to check the changes", or wants to verify a feature branch or worktree in the browser. Also handles "stop the preview".
---

# Preview

Runs the checked-out code as the real container image, next to a fresh Postgres, under its own Compose project. It never touches the user's normal `docker compose` stack or its data, and several worktrees can preview at once.

Run every command from the repo root of the checkout being previewed (the current working directory). Use bash.

## Start

1. **Docker check.** `docker info >/dev/null 2>&1` must succeed. If it fails, tell the user to start Docker Desktop and stop.

2. **Project name.** One preview per checkout, separate from the dev stack:
   ```sh
   P="$(basename "$PWD" | tr '[:upper:]' '[:lower:]' | tr -c 'a-z0-9-\n' '-')-preview"
   ```

3. **Fresh start, free ports.** `down -v` wipes the previous preview database so the user sees exactly what this branch's migrations produce. Port `0` lets Docker pick free host ports, which avoids clashes with the dev stack and other worktrees (shell env overrides any `.env`).
   ```sh
   docker compose -p "$P" down -v --remove-orphans
   APP_PORT=0 DB_PORT=0 docker compose -p "$P" up -d --build --wait db
   APP_PORT=0 DB_PORT=0 docker compose -p "$P" up -d --build app
   ```
   If the build fails, show the relevant error lines; the branch is not previewable, which is itself a review finding.

4. **Wait until healthy.** Migrations run on startup, so `/healthz` answering means the app is ready.
   ```sh
   URL="http://localhost:$(docker compose -p "$P" port app 8080 | cut -d: -f2)"
   for i in $(seq 90); do curl -fs "$URL/healthz" >/dev/null && break; sleep 1; done
   curl -fs "$URL/healthz" >/dev/null || docker compose -p "$P" logs --tail 50 app
   ```
   If it never got healthy, show the log tail and stop; do not report a URL.

5. **What to look at.** Gather the review context:
   ```sh
   git branch --show-current; git log --oneline -1
   git diff --stat main...HEAD
   git diff main...HEAD -- '*.feature'
   ```
   Uncommitted changes are part of the build too; mention them if `git status --short` is non-empty.

## Report

Keep it short:

```
Preview of <branch> @ <short sha> is running: <URL>
Empty database (fresh on every start).

Changed since main: <n> files (<areas, e.g. Features/Projects, Domain>)
Try these scenarios:
- <scenario title from added/changed .feature lines, phrased as something to click>
- ...

Stop with: /preview stop
```

Derive the scenarios from the added Gherkin (`+  Scenario:` and its steps) so the user knows what to click. If no `.feature` files changed, suggest checks from the changed pages under `Features/` instead. If the branch is `main` with no diff, just give the URL.

## Stop

When asked to stop (`/preview stop`), compute `P` as in step 2 and run:
```sh
docker compose -p "$P" down -v --remove-orphans
```

## Later

Seed data is planned but not built: when it exists, load it after step 4 and mention it in the report.

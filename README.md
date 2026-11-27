# ai-playground

A sandbox for exploring AI-assisted coding and AI automation.

## The app: credential rotation tracker

A small management app where users track credential rotations for their other applications.

It stores **metadata only**, never secrets:

- **Rotation policy**: how often a credential must be rotated, when it was last rotated, when it's due next
- **Rotation locations**: where the credential lives and must be updated (code, config files, pipelines, key vaults, etc.)
- **Owner/app**: which application and who's responsible

## Running locally

Requires Docker.

```sh
docker compose up -d --build
```

Then open http://localhost:8080.

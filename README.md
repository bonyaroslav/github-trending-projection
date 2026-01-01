# GitHub Trending Sync Service

A production-minded .NET backend that keeps a **current-state projection** of GitHub Trending repositories in **PostgreSQL** and exposes a REST API to query the data and trigger/view sync runs.

Built intentionally in **tiny TDD microsteps** (CONTRACT → RED → GREEN → REFACTOR). Small diffs, lots of proof.

---

## What it does (v1)

- Seeds repo candidates from GitHub Trending (`https://github.com/trending`).
- Enriches repo data via GitHub GraphQL (`https://api.github.com/graphql`).
- Stores the latest projection in **PostgreSQL** (single DB).
- Runs background work using **Hangfire** (in-process with the API host in v1; designed to extract later).
- Exposes a REST API for:
  - querying stored repositories
  - triggering sync (manual)
  - viewing sync runs/status

### Explicitly deferred (for now)
- Snapshots/history (v1 = current state only).
- Local-only fields (v1 doesn’t have them).
- “Perfect” overwrite/merge policy (v1 keeps it simple; expect change later).

---

## Tech stack

- .NET 10 / C# 14
- ASP.NET Core (API host)
- PostgreSQL (single source of truth)
- Hangfire + PostgreSQL storage provider (same DB)
- Strawberry Shake (GitHub GraphQL client)
- Tests: `dotnet test` (xUnit or equivalent)

---

## Docs (source of truth)

- Plan & microsteps: `docs/plan.md` + `docs/plan.phase*.md`
- Requirements: `docs/requirements.md`
- Standards: `docs/standards.md`
- API contract: `docs/API.md` (or Swagger/OpenAPI later)
- Repo layout: `docs/repo-structure.md`
- Decisions/ADRs: kept lightweight (see plan decision log or `docs/adr/` if introduced)

---

## Prerequisites

- .NET SDK 10.x
- PowerShell 7+ (`pwsh`)
- Docker (recommended; required once integration/E2E tests need Postgres)

---

## Quickstart (local)

### Build + test
```bash
dotnet restore
dotnet build
dotnet test
````

If you’re using scripts:

```bash
pwsh ./scripts/test.unit.ps1
```

### Run dependencies (when enabled)

```bash
docker compose up -d
```

### Run the API

```bash
dotnet run --project src/Api/Api.csproj
```

---

## Configuration

Config is via environment variables (final names live in `docs/requirements.md`). Suggested names:

* `GITHUB_TOKEN`
* `SEED_TRENDING_URL` (default: `https://github.com/trending`)
* `SEED_CACHE_TTL`
* `GITHUB_MAX_CONCURRENCY`
* `GITHUB_PAGE_SIZE`
* `SYNC_CRON` or `SYNC_INTERVAL`
* `POSTGRES_CONNECTION_STRING`
* retry/backoff settings (attempts, base delay, max delay)

Tip: use `.env.example` as a starting point (if present).
Important: never log secrets (token/connection strings).

---

## Testing

```bash
dotnet test
```

Integration/E2E tests may require Docker/Postgres depending on the current phase.
Docker/Testcontainers are allowed *as soon as needed* (not “later by principle”). See `docs/plan.*`.

---

## Folder structure (high level)

```text
repo-root/
  README.md
  .editorconfig
  .gitignore
  .gitattributes
  .dockerignore
  .env.example
  docker-compose.yml

  docs/
    requirements.md
    standards.md
    plan.md
    plan.phase0.md
    plan.phase1.md
    plan.phase2.md
    plan.phase3.md
    plan.phase4.md
    repo-structure.md
    API.md

  scripts/
    dev.up.ps1
    dev.down.ps1
    test.unit.ps1
    test.int.ps1

  src/
    Api/
      Api.csproj
      Program.cs
      appsettings*.json
      Endpoints/
      Contracts/
      Background/          (worker-in-API for v1)
    Core/
      Core.csproj
      Domain/
      Application/
        Abstractions/
        Features/
        Common/
    Infrastructure/
      Infrastructure.csproj
      GitHub/
      Seed/
      Postgres/
      Config/

  tests/
    Unit/
    Integration/
    E2E/
```

---

## Workflow (how to work in this repo)

1. Take the next microstep from `docs/plan.phase*.md`
2. Implement exactly that microstep (small diff)
3. Run proof commands (`dotnet build` / `dotnet test`)
4. Stop + review before moving on

---

## License

TBD 

```

---

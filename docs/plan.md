# Plan — GitHub Trending Sync Service (Postgres-only)

## 0) Header
**Objective (v1):** Build a .NET backend that can expose a REST API over a stored view of GitHub Trending repositories, and later sync/enrich data via background jobs.

**Non-goals (explicitly postponed):**
- Any graph database / Neo4j.
- Snapshot/history storage (v1 uses “current state” only).
- Local-only fields in v1 (deferred).
- Final “perfect” merge policy for sync overwrite vs merge (v1 uses simplest approach; decision may evolve).
- Heavy performance/stress testing.

**Global constraints (always):**
- Phases → Milestones → Microsteps.
- Keep diffs small and reviewable (prefer 1–3 files per microstep when possible).
- Every microstep has proof commands and ends with a STOP + approval request.
- No “future phase leakage”: don’t introduce DB/Hangfire/sync concerns before the phase allows it.

> Note: instruction docs can hit size caps (32 KiB default). Keep this file short and push details into phase files.【see docs/plan.phase*.md】

## 1) Phases overview (keep short)
- **Phase 0 — Repo skeleton + test harness**
  - Outcome: solution/projects exist; tests can run; docs layout ready
  - Proof: `dotnet build`, `dotnet test`
  - Stop: after scaffolding is stable

- **Phase 1 — API contract + first endpoints (no persistence)**
  - Outcome: contract doc exists; API returns stubbed/in-memory data; tests enforce contract
  - Proof: `dotnet test`
  - Stop: contract locked for v1 (until deliberate change)

- **Phase 2 — Persistence + background job plumbing**
  - Outcome: Postgres + EF Core + Hangfire storage (same Postgres)
  - Proof: integration tests + local run
  - Stop: data layer stable

- **Phase 3 — Sync/enrichment**
  - Outcome: seed from Trending + enrich via GitHub GraphQL; store to Postgres
  - Proof: integration tests; controlled manual sync
  - Stop: end-to-end happy path

- **Phase 4 — Hardening**
  - Outcome: resilience, rate-limit behavior, idempotency, observability gates
  - Proof: tests + operational checks
  - Stop: “production-minded” baseline

(Details live in: `docs/plan.phase0.md`, `docs/plan.phase1.md`, ...)

## 2) Phase files (source of truth)
- `docs/plan.phase0.md`
- `docs/plan.phase1.md`
- `docs/plan.phase2.md` (placeholder)
- `docs/plan.phase3.md` (placeholder)
- `docs/plan.phase4.md` (placeholder)

## 3) Execution rules for Codex
- Plan-first: propose exactly **one** next microstep (no edits) → wait approval → implement.
- Follow TDD loop for new behavior: CONTRACT → RED → GREEN → (REFACTOR).
- Always run the microstep’s proof commands and report results.
- Stop after each microstep: summarize files changed + why; ask approval.

Microstep format and numbering is Phase.Milestone.Microstep (e.g., 1.2.3). See phase files for the template.

## 4) Decision log (ADR-lite)
> If this grows, move older entries to `docs/adr/` and keep only links here.

- **D-001 Single DB**
  - Options: (A) Postgres only, (B) Postgres + another DB
  - Choice: **Postgres only**
  - Why: keep v1 simple; one source of truth
  - Consequences: relational modeling, migrations, etc.

- **D-002 Background jobs location (v1)**
  - Options: (A) worker inside API host, (B) separate worker service
  - Choice: **Worker inside API host in v1**, keep extraction easy later
  - Why: KISS for v1
  - Consequences: ensure boundaries so it can be extracted later

- **D-003 Hangfire storage**
  - Options: (A) Postgres storage, (B) separate Hangfire storage
  - Choice: **Hangfire Postgres storage in same DB**
  - Why: avoid extra infrastructure

- **D-004 Data strategy (v1)**
  - Choice: **Current state only** (no snapshots)
  - Consequences: future snapshot support may change schema

- **D-005 Projection overwrite policy (v1)**
  - Choice: **Simplest approach** (latest projection wins)
  - Consequences: may overwrite API-modified data; revisit later

## 5) Risk & rollback
- Risks: scope creep, early infra leakage, over-large diffs, flaky tests, unclear contract changes.
- Rollback: revert last commit / revert last microstep changes; keep microsteps small so rollback is cheap.

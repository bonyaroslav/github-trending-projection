# Plan — GitHub Trending Snapshots Service (Postgres-only)

## Objective (v1)
Build a .NET backend that:
- Captures GitHub Trending repositories into **immutable snapshots** stored in Postgres
- Exposes a REST API for snapshot CRUD (metadata) + querying repos within a snapshot
- Supports background capture via Hangfire (manual trigger first, recurring later)

Source of truth: `requirements.md`.

## Non-goals (v1)
- Perfect snapshot diff/compare features
- Heavy load testing
- Multi-tenant auth/permissions model
- Recreating GitHub’s Trending algorithm

## Global constraints
- Work in **phases** (big layers / capabilities), not microsteps.
- Still keep diffs reviewable, but don’t force artificial step granularity.
- Strict TDD: tests before implementation for every behavior.

---

## Phase 0 — Repo + test harness baseline
Outcome:
- Solution/projects compile, tests run reliably (unit/integration/e2e placeholders wired)

Exit criteria:
- `dotnet test` is green and repeatable

---

## Phase 1 — API contract + HTTP surface (no DB required)
Outcome:
- API contract stabilized (routes, DTOs, validation, ProblemDetails)
- Endpoints exist with in-memory implementation (enough for e2e/contract tests)

Focus areas:
- Snapshot endpoints (create/list/get/update/delete metadata)
- Snapshot repo query endpoints (list/get within snapshot)
- Pagination + consistent errors

Exit criteria:
- `dotnet test` green
- Contract tests cover success + common failures

---

## Phase 2 — Postgres persistence + immutability rules
Outcome:
- Postgres schema + EF Core mapping
- Snapshot item immutability enforced (constraints + app rules)
- Integration tests prove transactions + uniqueness constraints

Exit criteria:
- `dotnet test` green (integration tests included)

---

## Phase 3 — Snapshot capture pipeline + Hangfire
Outcome:
- Trending seed provider behind interface
- GraphQL enrichment client behind interface
- Hangfire job(s): manual trigger + run tracking persisted
- Single-flight behavior (no overlapping capture runs)

Exit criteria:
- End-to-end test: capture → persist snapshot → query API returns expected results

---

## Phase 4 — Hardening
Outcome:
- Rate limit awareness, bounded retries, better observability, idempotency policy
- Operational endpoints refined (run status/history)

Exit criteria:
- `dotnet test` green
- Minimal “production-minded” baseline documented in README

---

## How Codex should choose work
At any time, pick the next highest-value slice inside the current phase and implement it via TDD:
- clarify if needed → RED → GREEN → prove → report

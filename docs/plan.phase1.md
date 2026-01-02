# Phase 1 — API contract + first endpoints (no persistence)

## Goal
Define a stable v1 REST API contract and implement the first endpoints with tests, using in-memory/stubbed data only.

## Scope
- Contract doc (endpoints, DTOs, validation rules, error format, pagination rule)
- API skeleton endpoints implemented via TDD
- “API DTOs never leak domain entities” enforced

## Out of scope (Phase 1)
- Postgres/EF Core/Hangfire
- Real GitHub sync logic

## Acceptance criteria (verifiable)
- Contract doc exists and is referenced from docs
- Tests enforce at least the main endpoints and error shape
- `dotnet test` passes

## Milestones
- 1.1 Contract doc (first milestone — required)
- 1.2 API host wiring + “health/version” endpoint
- 1.3 First query endpoint (list) with pagination rule
- 1.4 First query endpoint (details) with consistent errors

---

### Milestone 1.1 — Contract doc (must be first)

**Microstep 1.1.1 — Create `docs/api.md` (contract v1)**
- Goal: Write the initial API contract doc (routes/DTOs/validation/errors/pagination).
- Files expected to change: `docs/api.md`
- Commands to run: (doc-only) manual review
- Acceptance: contract includes the minimum required items; it’s ready for tests
- Stop condition: Stop and ask approval.

---

### Milestone 1.2 - API host wiring + health/version

**Microstep 1.2.0 (DOC) - Allow integration test hosting dependency**
- Goal: Document the required test host package for integration tests.
- Files expected to change: `docs/plan.phase1.md`
- Commands to run: (doc-only) manual review
- Acceptance: plan allows adding `Microsoft.AspNetCore.Mvc.Testing` + API project reference for tests
- Stop condition: Stop and ask approval.

**Microstep 1.2.1a (CONTRACT) - Expose Program for integration test hosting**
- Goal: Add a minimal `public partial class Program` seam for `WebApplicationFactory`.
- Files expected to change: `src/Api/Program.cs`
- Commands to run: `dotnet build`
- Acceptance: build succeeds
- Stop condition: Stop and ask approval.

**Microstep 1.2.1 (CONTRACT) - Add request/response DTOs + endpoint signatures**
- Goal: Add minimal DTOs and endpoint shape so tests can compile (no behavior).
- Files expected to change: `src/Api/Contracts/*` (or similar)
- Commands to run: `dotnet build`
- Acceptance: build succeeds
- Stop condition: Stop and ask approval.

**Microstep 1.2.2 (RED) — Add failing API test for health/version**
- Goal: Add a failing test that asserts contract behavior.
- Files expected to change: `tests/Integration/*` (or chosen test project)
- Commands to run: `dotnet test`
- Acceptance: tests fail for the right reason (missing behavior)
- Stop condition: Stop and ask approval.

**Microstep 1.2.3 (GREEN) — Implement minimal endpoint**
- Goal: Implement the minimal endpoint to satisfy the test.
- Files expected to change: `src/Api/*`
- Commands to run: `dotnet test`
- Acceptance: tests pass
- Stop condition: Stop and ask approval.

---

### Milestone 1.3 — List endpoint (in-memory)

Repeat CONTRACT → RED → GREEN microsteps for “list repositories”.
Keep it in-memory only. Do not introduce persistence.

---

### Milestone 1.4 — Details endpoint + consistent errors

Repeat CONTRACT → RED → GREEN microsteps for “get repository details”.
Add at least one negative case (e.g., not found) using consistent ProblemDetails-style errors.

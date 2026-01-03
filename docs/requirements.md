## 1) Requirements.md — edited version (Snapshot-first, “full crude API”)

### What changed (high-level)

* **Before:** each sync overwrote “current state” in Postgres (projection).
* **Now:** each sync **creates a new immutable snapshot** (append-only), and API queries snapshots + the repositories inside them. Metadata can be updated; snapshot contents cannot.

> You can paste this as a replacement for your Requirements.md, or treat it as “v2”.

---

# Requirements — GitHub Trending Snapshots Service (Postgres-only)

**GitHub Trending (seed) → GitHub GraphQL (enrich) → PostgreSQL (snapshots) → REST API**
Target stack: **.NET 10 + C# 14 + ASP.NET Core + EF Core + PostgreSQL + Hangfire + Strawberry Shake**
Primary goal: **senior-level system design + maintainable code + strict TDD (CONTRACT → RED → GREEN → REFACTOR).**

---

## 0) Executive summary

Implement a backend service that periodically collects **Trending repositories**, enriches them via **GitHub GraphQL**, and persists **time-frozen, immutable snapshots** into **PostgreSQL**. The service exposes a **REST API** to manage snapshots (CRUD metadata + delete) and to query repositories *within a snapshot*. Background sync is orchestrated via **Hangfire** (recurring and on-demand jobs).

Key emphasis:

* Clean Architecture boundaries (Domain / Application / Infrastructure / API)
* Contract-first and tests-first TDD discipline
* Reliability: idempotency, rate-limit awareness, bounded retries, single-flight sync
* Strong DX: README, consistent errors, reproducible tests (Docker/Testcontainers as needed)

---

## 1) Data sources and constraints

### 1.1 Trending is a UI page, not an official API

Treat Trending as a **seed-only** source behind an abstraction (`ITrendingSeedProvider`) and be resilient to HTML changes.

Trending URL seed source:

* `https://github.com/trending`

### 1.2 GitHub GraphQL endpoint and auth

* Endpoint: `https://api.github.com/graphql`
* Auth: `Authorization: Bearer <TOKEN>` via configuration

### 1.3 Pagination constraints (GraphQL)

Connections require `first` or `last` within **1–100**.

### 1.4 Rate limiting constraints (GraphQL)

Explicitly handle GitHub GraphQL rate/query limits.

---

## 2) Non-goals (for v1)

* Writing back to GitHub (mutations)
* Recreating GitHub’s internal Trending algorithm
* “Perfect” diffing/compare between snapshots (optional later)
* Heavy load testing (optional later)
* Advanced multi-versioning strategy beyond `/api/v1` (keep simple)

---

## 3) Architecture requirements (must follow)

### 3.1 Clean Architecture boundaries

* Domain: entities/value objects, invariants
* Application: use cases (capture snapshot, list snapshots, update metadata, query snapshot repos), ports/interfaces
* Infrastructure: Trending provider, GitHub GraphQL client, EF Core/Postgres, Hangfire wiring, retry/rate-limit logic
* API/Presentation: endpoints, validation, ProblemDetails mapping

Dependency direction:

* Domain does not depend on Infrastructure
* Application depends on abstractions; Infrastructure implements ports
* API depends on Application only

### 3.2 Ports/adapters (TDD-enabling)

External dependencies behind interfaces (illustrative):

* `ITrendingSeedProvider`
* `IGitHubGraphQlClient` (Strawberry Shake)
* `ISnapshotStore` / `ISnapshotReadModel`
* `ISyncCoordinator` (single-flight orchestration)
* `IJobScheduler` (Hangfire wrapper)
* `IClock`

---

## 4) Process requirement: Phases → Milestones → Microsteps

Project execution is maintained as **Phases → Milestones → Microsteps**, compatible with strict TDD.

---

## 5) Functional requirements (Snapshot-based)

### FR-0 API contract is a first-class artifact (TDD prerequisite)

The REST API contract must be defined early and used as the test target.

Minimum contract artifacts:

* routes + verbs
* request/response DTOs
* validation rules
* error format (ProblemDetails)
* pagination rules
* snapshot immutability rules

> Use `API.md` as the contract source of truth for v1.

### FR-1 Seed acquisition (Trending)

1. Fetch Trending seeds from `https://github.com/trending`.
2. Implement behind `ITrendingSeedProvider`.
3. Low-frequency + cached (configurable TTL).
4. Resilient to HTML changes; fail gracefully (do not crash API process).

### FR-2 Enrichment (GitHub GraphQL via Strawberry Shake)

Given a seed repo (`owner/name`), fetch repository details via GitHub GraphQL.

* Use Strawberry Shake client
* Respect pagination and rate limits
* Define policy for partial GraphQL responses (`data` + `errors`)

### FR-3 Persistence (Postgres snapshots, immutable contents)

Persist **snapshots** and **snapshot repository items** to Postgres.

**Snapshot entity (minimum fields):**

* `snapshotId` (server-generated, GUID or ULID; choose one)
* `source` (e.g. `github-trending`)
* `capturedAt` (UTC timestamp of capture)
* `name` (optional)
* `notes` (optional)
* `itemCount` (derived from items count)

**Snapshot repository item (time-frozen fields):**

* `snapshotId` (FK)
* `repoId` (stable: GitHub `node_id` preferred, else `owner/name`)
* `rank` (1..N unique within snapshot)
* `owner`, `name`, `fullName`
* `description`, `language`
* `stars`, `forks`, `url`
* `repoUpdatedAt` (as known at capture time; allow null if unknown)

**Immutability rules:**

* After snapshot creation, **snapshot items MUST NOT be updated**.
* Only snapshot metadata (`name`, `notes`) may be updated.

**DB constraints (must-have):**

* Unique `(snapshotId, repoId)`
* Unique `(snapshotId, rank)`
* Optional uniqueness to prevent duplicate snapshots (choose one and document):

  * `(source, capturedAt)` unique, OR
  * allow duplicates but expose as separate IDs

**Atomic write rule (must-have):**

* Snapshot + all items are inserted in a single transaction (all-or-nothing).

### FR-4 Snapshot capture policy (replaces “projection overwrite”)

* Each successful sync run creates **one new snapshot**.
* Snapshot contents represent the trending list at capture time.
* Subsequent runs do not mutate prior snapshots.

> Optional v1+ convenience (not required): maintain a derived “latest snapshot” pointer or view for faster access.

### FR-5 REST API (full crude API for snapshots)

The REST API must implement:

1. **Snapshot CRUD**

   * create snapshot from inline repository list (manual)
   * list snapshots (newest first)
   * get snapshot metadata
   * update snapshot metadata (PATCH only)
   * delete snapshot

2. **Snapshot repository queries**

   * list repos in snapshot (paged, rank-ordered)
   * get repo entry in snapshot

3. **Operational endpoints**

   * trigger snapshot capture (manual)
   * view capture run history/status (basic)

> Server-side capture and run tracking are exposed via `/snapshots:capture` and `/sync-runs`.

### FR-6 Background sync (Hangfire)

1. Use Hangfire for recurring schedule + manual trigger.
2. Single-flight semantics (no overlapping capture runs).
3. Cancellation aware; clean shutdown.
4. Hangfire uses Postgres storage (same DB).

---

## 6) Reliability and operational requirements (must-have)

### RR-1 Snapshot idempotency + dedupe policy

* A single run must not create duplicate items (constraints + transaction).
* Define what “duplicate snapshot” means and enforce it if needed (e.g., `(source, capturedAt)` unique).

### RR-2 Retry strategy (bounded)

* Bounded retries for transient failures
* Backoff + jitter
* Rate-limit aware (avoid retry storms)

### RR-3 Rate limiting compliance

* Detect and respect GraphQL rate/query limits
* Cap concurrency (configurable)
* Log rate-limit events

### RR-4 Run tracking and auditability

Persist sync metadata:

* run id, start/end, status
* counts (seeds processed, snapshot items inserted)
* error summary (non-sensitive)
* optional: snapshotId created by the run

---

## 7) Error handling & observability (must-have)

* Global exception handling
* Consistent ProblemDetails error responses
* Input validation
* Structured logging with correlation id
* Never log secrets

---

## 8) Testing requirements (TDD mandatory)

### TR-1 Tests first

Every behavior starts with failing tests after contract is defined.

### TR-2 Minimum suite

* Unit tests: seed parsing, mapping, snapshot invariants, validation rules
* Integration tests: Postgres constraints, transaction atomicity, “immutability cannot be broken”
* E2E: capture → persist snapshot → API queries return expected snapshot + items

### TR-3 Docker/Testcontainers as soon as needed

Introduce Docker/Testcontainers early for Postgres integration tests when it starts saving time.

---

## 9) Documentation requirements (must-have)

* README: overview, architecture, config, run locally, run tests, limitations
* Decision log in `docs/plan.md`
* API docs: API.md or OpenAPI (Swagger) (either is OK; API.md is enough for v1)

---

## 10) Configuration requirements (must-have)

* `GITHUB_TOKEN`
* `SEED_TRENDING_URL` (default `https://github.com/trending`)
* `SEED_CACHE_TTL`
* `GITHUB_MAX_CONCURRENCY`
* `GITHUB_PAGE_SIZE` (1–100)
* `SYNC_INTERVAL`
* `POSTGRES_CONNECTION_STRING`
* retry/backoff settings

---

## 11) Deliverables

* Source code
* Unit + integration + E2E tests
* README + API docs
* Working local run
* Optional docker-compose

---

## 2) API contract alignment decisions (v1)

### A) Manual snapshot creation only on POST /snapshots

* `POST /snapshots` stores an inline repositories list only.
* Server-side capture is exposed separately via `/snapshots:capture` and `/sync-runs`.

### B) Metadata updates are PATCH-only

* `PATCH /snapshots/{snapshotId}` is the only metadata update endpoint.
* Unknown fields yield `400` validation error.

### C) Pagination behavior is deterministic for tests

* `totalPages = ceil(totalItems / pageSize)`; when `totalItems = 0`, `totalPages = 0`.
* If `page` is greater than `totalPages`, return `200` with empty `items`.

### D) Snapshot detail returns notes

* `SnapshotDetail` includes `notes` and `name`; `SnapshotSummary` stays minimal.

### E) Resource identifiers are unambiguous

* `repoId` in paths must be URL-encoded; use `/repositories/by-full-name` for `owner/name`.

### F) Location header on create

* `201 Created` responses include `Location: /api/v1/snapshots/{snapshotId}`.

---
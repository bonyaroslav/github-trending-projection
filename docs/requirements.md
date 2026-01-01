# Requirements — GitHub Trending Sync Service (Postgres-only)

**GitHub Trending (seed) → GitHub GraphQL (enrich) → PostgreSQL (single source of truth) → REST API**
Target stack: **.NET 10 + C# 14 + ASP.NET Core (.NET 10 / C# 14) + EF Core (.NET 10 / C# 14) + PostgreSQL + Hangfire + Strawberry Shake (StrawberryShake)**
Primary goal: **demonstrate senior-level system design, maintainable code, and strict TDD discipline (CONTRACT → RED → GREEN → REFACTOR).**

---

## 0) Executive summary

Implement a backend service that periodically collects **Trending repositories** (from GitHub Trending UI), enriches them via the **GitHub GraphQL API**, persists the resulting dataset into **PostgreSQL**, and exposes a **REST API** to query and modify the stored data. Background sync is orchestrated via **Hangfire** (recurring and on-demand jobs). ([Hangfire][1])

Key emphasis:

* **Clean Architecture** boundaries (Domain / Application / Infrastructure / API)
* **TDD-first** workflow (contract-first + tests-first)
* Production-minded reliability: **idempotency**, **rate-limit awareness**, **bounded retries**, **single-flight sync**
* Strong DX: **README**, predictable configuration, consistent error handling, reproducible tests (Docker/testcontainers as needed)

---

## 1) Data sources and constraints

### 1.1 Trending is a UI page, not an official API

GitHub does **not** provide official REST API endpoints for `/trending` (or `/explore`). Treat Trending as a **seed-only** source and implement it behind an abstraction so it can be replaced later. ([GitHub][2])

Trending URL (seed source):

```text
https://github.com/trending
```

### 1.2 GitHub GraphQL endpoint and auth

GitHub GraphQL endpoint:

```text
https://api.github.com/graphql
```

Clients must send `Authorization: Bearer <TOKEN>` (token management is out of scope; it is provided via configuration). ([GitHub Docs][3])

### 1.3 Pagination constraints (GraphQL)

GraphQL connections require `first` or `last` and the value must be **1–100**. ([GitHub Docs][4])

### 1.4 Rate limiting constraints (GraphQL)

The implementation must explicitly handle GitHub GraphQL rate limits and query limits (primary limits and related behaviors). ([GitHub Docs][5])

---

## 2) Non-goals (for v1)

These are intentionally out of scope unless explicitly added later:

* Writing back to GitHub (mutations)
* Perfectly recreating GitHub’s internal Trending ranking algorithm
* Heavy performance/stress testing (optional late task)
* Finalizing an advanced API versioning strategy up front (e.g., `/v1`)
* Implementing a “perfect” merge policy for projection updates (see §5.4 for the explicit TBD decision point)

---

## 3) Architecture requirements (must follow)

### 3.1 Clean Architecture boundaries

Implement with at least these layers:

* **Domain**: entities/value objects, invariants, domain rules
* **Application**: use cases (sync orchestration, mapping, persistence, query services), ports/interfaces
* **Infrastructure**: Trending seed provider, GitHub GraphQL client (Strawberry Shake), EF Core/Postgres, Hangfire wiring, retry/rate-limit plumbing
* **API/Presentation**: REST endpoints, request/response models, validation, error mapping

**Dependency direction**

* Domain must not depend on Infrastructure.
* Application references only abstractions; Infrastructure implements ports.
* API depends on Application, not vice versa.

### 3.2 Ports/adapters (TDD-enabling)

All external dependencies must be behind interfaces (names are illustrative):

* `ITrendingSeedProvider`
* `IGitHubGraphQlClient` (implemented via Strawberry Shake) ([chillicream.com][6])
* `IRepositoryStore` / `IRepositoryReadModel` (Postgres persistence/query)
* `ISyncCoordinator` (single-flight + orchestration)
* `IJobScheduler` (Hangfire wrapper)
* `IClock` (time abstraction)

This is mandatory to support tests-first development.

---

## 4) Process requirement: Phases → Milestones → Microsteps

* Project execution must be maintained as: **Phases → Milestones → Microsteps**.
* Microsteps are expected to evolve over time as you learn and make decisions.
* The plan must remain compatible with strict TDD loop enforcement: every milestone should be decomposable into contract/tests/implementation/refactor microsteps.

---

## 5) Functional requirements

### FR-0 API Contract must be designed early (TDD prerequisite)

Because TDD requires a stable target to test against, the **REST API contract is a first-class artifact** and must be defined early (first milestone).

Minimum contract artifacts must include:

* endpoint list (routes + HTTP verbs)
* request/response DTOs
* validation rules (minimum: required fields, ranges)
* error format (ProblemDetails recommended, but not mandatory)
* pagination/sorting/filtering rules (at least one, documented)

> Note: GitHub GraphQL contract already exists; your internal API contract does not, so it must be explicitly designed up front.

---

### FR-1 Seed acquisition (Trending)

1. Fetch Trending seeds from:

   ```text
   https://github.com/trending
   ```

2. Must be implemented through `ITrendingSeedProvider`.

3. Must be **low-frequency + cached** (configurable TTL) to avoid abusive automation.

4. Must be resilient to HTML changes:

   * fail gracefully with clear error messages and logs
   * do not bring down the API host process

Implementation choice is open (HTML parsing/headless browser/etc.), but must be documented and testable.

---

### FR-2 Enrichment fetch (GitHub GraphQL via Strawberry Shake)

1. Given a seed repo (`owner/name`), fetch repository details via GitHub GraphQL. ([GitHub Docs][7])
2. Use **Strawberry Shake** as the GraphQL client library for the GitHub GraphQL API. ([chillicream.com][6])
3. Must support cursor pagination where needed and respect `first/last` limits (1–100). ([GitHub Docs][4])
4. Must handle partial GraphQL responses (GraphQL may return partial `data` with `errors`) by a documented policy (e.g., proceed with partial data vs fail run).

---

### FR-3 Persistence (Postgres only, via EF Core)

1. Persist enriched entities into **PostgreSQL** as the **single source of truth**.
2. Must use stable identities:

   * prefer GitHub node IDs when available, or
   * deterministic keys derived from `owner/name` (document the choice)
3. Must be **idempotent**:

   * re-ingesting the same repo must not create duplicates
   * repeated runs converge to the same state (subject to time-varying upstream data)
4. Idempotency must be enforced using:

   * unique constraints / indexes
   * deterministic keys
   * explicit upsert strategy (implementation-defined)

---

### FR-4 “Projection overwrite” policy 

FR-4 Projection policy (v1: current-state overwrite)

 - Sync persists current state only (no snapshots).
 - Sync overwrites mapped GitHub fields on every run.
 - Any API-driven changes to those same fields are allowed but not durable and may be overwritten by next sync.

---

### FR-5 REST API (query + ops)

The REST API must provide:

1. Query/list stored repositories

   * include at least one of: sorting, filtering, pagination (document behavior)

2. Operational endpoints:

   * trigger a sync run
   * view sync status/run history (basic)
3. API can modify data

	- allow delete/untrack of stored repo records with documented behavior that next sync may recreate 
	- them if they appear again in Trending.

API documentation is required (see §9).

---

### FR-6 Background sync (Hangfire)

1. Background processing must use **Hangfire**. ([Hangfire][1])
2. Must support:

   * recurring schedule (configurable)
   * manual trigger (via API endpoint)
3. Must implement **single-flight semantics**:

   * prevent overlapping sync runs (global or per “scope”, as defined)
   * define behavior when a run is requested during an active run (reject or enqueue one) and document it
4. Must honor cancellation tokens and shut down cleanly.
5. Hangfire must use PostgreSQL storage provider and reuse the same Postgres database (separate schema/tables are fine).

---

## 6) Reliability and operational requirements (must-have)

### RR-1 End-to-end idempotency

Multiple runs must converge without duplicates.

### RR-2 Retry strategy (bounded)

* Implement retries for transient failures (network, 5xx, transient DB connectivity)
* Must be bounded (max attempts) with backoff + jitter
* Must be rate-limit-aware (avoid retry storms) ([GitHub Docs][5])

### RR-3 Rate limiting compliance

* Must detect and respect GitHub GraphQL rate/query limits ([GitHub Docs][5])
* Must cap concurrency for outbound requests (configurable)
* Must log rate limiting events in a reviewable way

### RR-4 Run tracking and auditability

Persist sync metadata supporting at least:

* run id, start/end timestamps, status (success/fail/canceled)
* counts (seeds processed, entities upserted)
* error summary (non-sensitive)

---

## 7) Error handling & observability (must-have)

* Global exception handling for API requests
* Consistent error response format (ProblemDetails recommended)
* Input validation on API requests
* Structured logging:

  * correlation id / request id
  * sync lifecycle events
  * retry events (attempt count, delay)
  * rate limit events
* Never log secrets (GitHub token, connection strings)

---

## 8) Testing requirements (TDD is mandatory)

### TR-1 Process requirement: tests first

Every new behavior must start with a failing test (**RED**) after establishing the needed contract (**CONTRACT**), then minimal implementation (**GREEN**), then refactor (**REFACTOR**).

### TR-2 Minimum test suite

1. **Unit tests**

   * seed parsing → seed contract
   * mapping from GraphQL DTOs → domain models
   * idempotency logic (keying, upsert decisions)
   * rate-limit behavior policy unit tests (as feasible)
2. **Integration tests**

   * Postgres persistence behaviors (constraints, upserts)
   * migrations applied and validated
3. **End-to-end happy path**

   * seed → enrich → persist → API query returns expected data

### TR-3 Docker usage for tests (as early as needed)

Docker may be introduced **as soon as needed** to run integration/E2E tests efficiently and reproducibly (locally and in CI).
Using **Testcontainers for .NET** for Postgres is allowed/recommended (implementation choice), but not mandatory. ([Testcontainers for .NET][8])

---

## 9) Documentation requirements (must-have)

### DR-1 README.md (required)

Must include:

* what the system does (one-paragraph overview)
* high-level architecture description
* configuration (env vars)
* how to run locally
* how to run tests (including Docker/Testcontainers expectations)
* what’s implemented vs deferred
* known limitations and next steps
### DR-2 
	Accepts Decision log inside docs/plan.md

### DR-3 API documentation (required)

Either:

* OpenAPI/Swagger **or**
* `API.md` describing endpoints, example requests/responses, and error format

---

## 10) Configuration requirements (must-have)

Configuration via environment variables (names are suggestions; document final names):

* `GITHUB_TOKEN`
* `SEED_TRENDING_URL` (default `https://github.com/trending`)
* `SEED_CACHE_TTL`
* `GITHUB_MAX_CONCURRENCY`
* `GITHUB_PAGE_SIZE` (must be within 1–100 for GraphQL connections) ([GitHub Docs][4])
* `SYNC_INTERVAL` (Hangfire recurring schedule)
* `POSTGRES_CONNECTION_STRING`
* retry/backoff settings (attempts, base delay, max delay)

---

## 11) Deliverables

### Required

* Source code repository
* Unit + integration + E2E tests (TDD-compliant)
* README + API docs
* Working local run

### Docker

* Docker support is allowed **as soon as needed** (especially for tests and local setup).
* `docker-compose` is acceptable if you choose to provide it.

---

## 12) Evaluation criteria (for take-home and review)

A senior implementation will be assessed on:

* Clean Architecture boundaries (dependency direction)
* Test quality and TDD discipline (meaningful tests, not superficial)
* Correct handling of pagination, rate limiting, retries, idempotency ([GitHub Docs][4])
* Code readability, maintainability, and change friendliness
* Documentation quality (README + API docs)
* Operational clarity (run tracking, logs, predictable behavior)

---

## 13) Live coding interview follow-up (based on the implementation)

Pick one extension task during the interview (examples):

1. Add/adjust one stored field or mapping rule, tests first
2. Tighten idempotency guarantees (duplicate seeds, run replays)
3. Improve rate-limit behavior (throttling/backoff, concurrency caps) ([GitHub Docs][5])
4. Add a new query capability to the API (filter/sort/pagination), tests first
5. Modify the overwrite/merge policy with tests and an ADR

---

## 14) Reference constraints (for implementers)

* GitHub GraphQL API docs ([GitHub Docs][7])
* GitHub GraphQL pagination rules (first/last 1–100) ([GitHub Docs][4])
* GitHub GraphQL rate/query limits ([GitHub Docs][5])
* No official Trending API statement (GitHub Community discussion) ([GitHub][2])
* Strawberry Shake docs / project ([chillicream.com][6])
* Hangfire docs ([Hangfire][1])
* Testcontainers for .NET (Postgres module / guide) ([Testcontainers for .NET][8])

---


[1]: https://www.hangfire.io/?utm_source=chatgpt.com "Hangfire – Background jobs and workers for .NET and .NET ..."
[2]: https://github.com/orgs/community/discussions/161519?utm_source=chatgpt.com "REST API Endpoints for /explore and /trending #161519"
[3]: https://docs.github.com/en/graphql/guides/using-graphql-clients?utm_source=chatgpt.com "Using GraphQL Clients"
[4]: https://docs.github.com/en/graphql/guides/using-pagination-in-the-graphql-api?utm_source=chatgpt.com "Using pagination in the GraphQL API"
[5]: https://docs.github.com/en/graphql/overview/rate-limits-and-query-limits-for-the-graphql-api?utm_source=chatgpt.com "Rate limits and query limits for the GraphQL API"
[6]: https://chillicream.com/docs/strawberryshake/v14/get-started/?utm_source=chatgpt.com "Get started with Strawberry Shake and Blazor"
[7]: https://docs.github.com/en/graphql?utm_source=chatgpt.com "GitHub GraphQL API documentation"
[8]: https://dotnet.testcontainers.org/modules/postgres/?utm_source=chatgpt.com "PostgreSQL"

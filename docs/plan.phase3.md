# Phase 3 - Snapshot capture pipeline + Hangfire

## Outcome
- Trending seed provider feeds capture runs via abstraction.
- GitHub GraphQL enrichment runs behind a client port with rate-limit aware behavior.
- Capture runs persist snapshots + run records with single-flight orchestration.
- Manual capture endpoint schedules Hangfire job and exposes run status.
- End-to-end path: capture -> persist snapshot -> query API for items.

## Decisions checklist (phase 3)
- Use ITrendingSeedProvider and IGitHubGraphQlClient ports from Application layer.
- Keep capture orchestration in Application; Infrastructure wires providers + Hangfire.
- Track sync runs in Postgres with status, timestamps, counts, and snapshotId (optional).
- Enforce single-flight capture (no overlapping runs) via ISyncCoordinator.
- Conflict behavior: return 409 and log a clear, structured message with run id (if any).
- Snapshot creation uses existing transactional store from phase 2.
- GraphQL rate-limit info is captured and logged; retry policy stays bounded.
- Manual trigger uses Hangfire enqueue; recurring schedule added after manual path is stable.
- Errors are normalized to ProblemDetails for API endpoints.
- If GitHub data is needed for tests, capture it once and store as local fixtures; tests must run offline.

## Slices (TDD, integration tests unless noted)
1) Sync run tracking store (integration)
   - Add SyncRun entity + schema + store abstraction.
   - Persist start/end/status/counts; query latest runs.
   - Proof: dotnet test

2) Capture orchestrator (unit)
   - Add Application use case that fetches seeds, enriches, and persists snapshot.
   - Verify deterministic behavior with IClock and fakes.
   - Proof: dotnet test

3) Snapshot persistence flow (integration)
   - Verify single transaction for snapshot + items.
   - Validate sync run status transitions and counts.
   - Proof: dotnet test

4) Single-flight coordinator (unit)
   - Prevent overlapping capture runs; return 409 conflict result.
   - Log a clear structured message for debugging.
   - Proof: dotnet test

5) Rate-limit handling (unit)
   - Parse/propagate rate-limit info from GraphQL responses.
   - Ensure logging includes limit/remaining/reset fields.
   - Proof: dotnet test

6) Manual capture contract + Hangfire job (integration)
   - Add contract tests for /snapshots:capture response and error codes.
   - Wire Hangfire job to invoke capture use case; return run id.
   - Proof: dotnet test

7) End-to-end happy path (e2e)
   - Trigger capture -> snapshot persisted -> list repos in snapshot.
   - Use local fixtures or in-memory fakes; no network reliance.
   - Proof: dotnet test

## Proof gates
- Always: dotnet test
- If new infrastructure or migrations added: dotnet build

# Phase 2 - Persistence + immutability

## Outcome
- Postgres persistence is production-minded (schema owned, migrations, constraints).
- Snapshot item immutability is enforced at DB + app layers.
- API queries scale by filtering/paging in SQL, not in-memory.
- Integration tests prove constraints, transactions, and query behavior.

## Decisions checklist (phase 2)
- Use EF Core migrations for schema evolution; avoid EnsureCreated for production flows.
- Enforce unique snapshot identity on (source, captured_at_utc).
- Enforce snapshot item uniqueness on (snapshot_id, repo_id) and (snapshot_id, rank).
- Insert snapshot + items in a single transaction (all-or-nothing).
- Use async EF Core APIs with cancellation tokens end-to-end.
- Map DB unique-constraint failures to 409; other DbUpdateException cases surface as 500.
- Query repos within a snapshot via SQL (paging/filtering/sorting in DB).
- Add indexes aligned to query patterns (snapshot_id, rank, full_name, language, stars, forks).
- Add DB-aware health signal (readiness) separate from liveness.
- Move connection string/config into options with validation at startup.

## Slices (TDD, integration tests)
1) Migrations + schema ownership
   - Add initial migration for snapshots + snapshot_repositories + indexes.
   - Integration tests use Migrate() instead of EnsureCreated().
   - Proof: dotnet test

2) SQL-backed repository query
   - Add read-model query in PostgresSnapshotStore (or ISnapshotReadModel) for paged/filter/sort.
   - Update endpoints to call query; avoid loading full snapshot for listing.
   - Proof: dotnet test

3) Error mapping + resilience
   - Distinguish unique-constraint conflict vs other DbUpdateException errors.
   - Add global exception handler for consistent ProblemDetails.
   - Proof: dotnet test

4) Async data access + cancellation
   - Convert store + endpoint paths to async EF calls with CancellationToken.
   - Proof: dotnet test

5) Health + config validation
   - Add readiness endpoint that checks DB connectivity.
   - Introduce options + validation for POSTGRES_CONNECTION_STRING.
   - Proof: dotnet test

## Proof gates
- Always: dotnet test
- If migrations or new infra introduced: dotnet build
# Phase 0 — Repo skeleton + test harness

## Goal
Create a minimal, clean repo scaffold that compiles and runs tests locally.

## Scope
- Solution + projects + test projects
- Docs layout
- Minimal scripts for build/test (PowerShell)

## Out of scope (Phase 0)
- Postgres, EF Core, Hangfire, Docker compose usage
- GitHub calls / sync logic
- Real API endpoints (beyond “skeleton/health” if needed)

## Acceptance criteria (verifiable)
- `dotnet build` succeeds
- `dotnet test` succeeds (at least one smoke test)

## Milestones
- 0.1 Docs layout (plan + standards + requirements + structure doc)
- 0.2 Solution + projects scaffold
- 0.3 Tests scaffold + smoke tests
- 0.4 Scripts + editorconfig verification

---

### Milestone 0.1 — Docs layout

**Microstep 0.1.1 — Add repo structure doc under docs/**
- Goal (1 sentence): Put the folder structure into `docs/` as the authoritative reference.
- Files expected to change: `docs/repo-structure.md`
- Commands to run: (doc-only) manual review
- Acceptance: doc exists and matches intended structure
- Stop condition: Stop and ask approval for next microstep.

**Microstep 0.1.2 — Create phase placeholders (2–4)**
- Goal: Add empty placeholder phase files so the doc layout is stable.
- Files expected to change: `docs/plan.phase2.md`, `docs/plan.phase3.md`, `docs/plan.phase4.md`
- Commands to run: (doc-only) manual review
- Acceptance: placeholder files exist with headers
- Stop condition: Stop and ask approval for next microstep.

---

### Milestone 0.2 — Solution + projects scaffold

**Microstep 0.2.1 — Create solution file**
- Goal: Create the .sln and base folders.
- Files expected to change: `*.sln`, folders under `/src`, `/tests`
- Commands to run: `dotnet build`
- Acceptance: build succeeds
- Stop condition: Stop and ask approval.

**Microstep 0.2.2 — Create Core project**
- Goal: Add Core class library project.
- Files expected to change: `src/Core/*`
- Commands to run: `dotnet build`
- Acceptance: build succeeds
- Stop condition: Stop and ask approval.

**Microstep 0.2.3 — Create Infrastructure project**
- Goal: Add Infrastructure class library project.
- Files expected to change: `src/Infrastructure/*`
- Commands to run: `dotnet build`
- Acceptance: build succeeds
- Stop condition: Stop and ask approval.

**Microstep 0.2.4 — Create API project**
- Goal: Add ASP.NET Core API project.
- Files expected to change: `src/Api/*`
- Commands to run: `dotnet build`
- Acceptance: build succeeds
- Stop condition: Stop and ask approval.

**Microstep 0.2.5 — Wire project references (Clean Architecture direction)**
- Goal: Set correct project references (API → Application/Core; Infrastructure implements ports).
- Files expected to change: `*.csproj` refs only
- Commands to run: `dotnet build`
- Acceptance: build succeeds; dependency direction is sane
- Stop condition: Stop and ask approval.

---

### Milestone 0.3 — Tests scaffold + smoke tests

**Microstep 0.3.1 — Create Unit test project**
- Goal: Add Unit test project and run a trivial passing test.
- Files expected to change: `tests/Unit/*`
- Commands to run: `dotnet test`
- Acceptance: `dotnet test` passes
- Stop condition: Stop and ask approval.

**Microstep 0.3.2 — Create Integration test project (empty for now)**
- Goal: Create the project so it’s ready for later phases.
- Files expected to change: `tests/Integration/*`
- Commands to run: `dotnet test`
- Acceptance: test run still passes (no infra introduced)
- Stop condition: Stop and ask approval.

**Microstep 0.3.3 — Create E2E test project (empty for now)**
- Goal: Create the project so it’s ready for later phases.
- Files expected to change: `tests/E2E/*`
- Commands to run: `dotnet test`
- Acceptance: test run still passes
- Stop condition: Stop and ask approval.

---

### Milestone 0.4 — Scripts + editorconfig verification

**Microstep 0.4.1 — Add PowerShell test runner scripts**
- Goal: Add `scripts/test.unit.ps1` and keep it minimal.
- Files expected to change: `scripts/test.unit.ps1` (and maybe `scripts/test.all.ps1` if desired)
- Commands to run: `pwsh ./scripts/test.unit.ps1`
- Acceptance: script succeeds and runs `dotnet test`
- Stop condition: Stop and ask approval.

**Microstep 0.4.2 — Verify `.editorconfig` is present at repo root**
- Goal: Ensure formatting rules are in place early.
- Files expected to change: `.editorconfig` (only if missing)
- Commands to run: `dotnet build`
- Acceptance: build succeeds; editorconfig is present
- Stop condition: Stop and ask approval.

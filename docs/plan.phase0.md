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


**Microstep 0.3.1 — Scaffold Test Infrastructure**

* **Goal:** Create `Unit`, `Integration`, and `E2E` test projects, add them to the solution, and ensure the test runner detects them. Add a trivial passing test to the Unit project to verify the harness works.
* **Files expected to change:** `tests/Unit/*`, `tests/Integration/*`, `tests/E2E/*`, `*.sln`
* **Commands to run:** `dotnet test`
* **Acceptance:**
1. Directory structure exists for all three test types.
2. Projects are added to the `.sln` file.
3. `dotnet test` runs successfully and reports at least 1 passing test (from the Unit project).

* **Stop condition:** Stop and ask approval.

---

**Would you like me to look at the next few steps (0.4 series) to see if we can batch those into larger chunks as well?**

---

### Milestone 0.4 — Scripts + editorconfig verification

**Microstep 0.4.1 — Add PowerShell test runner scripts**
- Goal: Add `scripts/test.unit.ps1` and keep it minimal.
- Files expected to change: `scripts/test.unit.ps1` (and maybe `scripts/test.all.ps1` if desired)
- Commands to run: `pwsh ./scripts/test.unit.ps1`
- Acceptance: script succeeds and runs `dotnet test`
- Stop condition: Stop and ask approval.

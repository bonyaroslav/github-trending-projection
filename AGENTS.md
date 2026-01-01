# AGENTS.md — workflow contract for coding agents (Codex CLI)

This repo is built in **tiny, verifiable microsteps**. Your job is to stay on-task, keep diffs small, and prove each change.

---

## Read order (keep it lean)
1) `AGENTS.md` (this file)
2) `docs/plan.md` (phase index + rules + decision log)
3) Current phase file: `docs/plan.phase*.md`
4) `docs/standards.md`
5) `docs/requirements.md`

> Keep instruction loading small (tools may stop after a size limit). Prefer links to `docs/*` over duplicating content here.

### Rule priority (if anything conflicts)
`AGENTS.md` → `docs/plan.md` → `docs/plan.phase*.md` → `docs/standards.md` → `docs/requirements.md` → everything else.

---

## Non-negotiable loop (every run)

### 1) PLAN (no edits)
Propose **exactly one** next microstep from the plan (e.g., `0.2.3`).

Include:
- Goal (1 sentence)
- Expected files to change
- Proof commands to run
- Acceptance check (what “done” means)

Then **STOP** and ask for approval.

### 2) EXECUTE (one microstep only)
- Implement only the approved microstep.
- Keep the diff small and reviewable.
- If the microstep looks too big: **STOP**, propose how to split it in the plan, and wait.

### 3) PROVE
Run the microstep’s proof commands (at minimum `dotnet build` or `dotnet test`).

### 4) REPORT + STOP
Summarize:
- What changed (files + why)
- Commands run + pass/fail (short, relevant output)
- Any risks / follow-ups

Then ask approval for the next microstep.

---

## Default TDD rhythm (use unless plan says otherwise)

### CONTRACT
Create the minimal seam so tests compile:
- DTOs / interfaces / ports / endpoint signatures
- **No real behavior**

Run: `dotnet build`

### RED
Add tests for **one** behavior. Do not change production code.
Run: `dotnet test` (must fail for the right reason)

### GREEN
Minimal production code to pass.
Run: `dotnet test`

### REFACTOR (optional)
Refactor only after green; no behavior changes.
Run: `dotnet test`

---

## Guardrails (common failure modes)
- **No future-phase leakage:** do not introduce Postgres/Hangfire/GitHub sync concerns before the phase allows it.
- **Small diffs:** prefer one behavior per microstep; avoid “while I’m here” changes.
- **Tests assert behavior:** avoid tests coupled to internal implementation.
- **Mocking:** mock only external boundaries (HTTP/DB/clock). Avoid over-mocking.
- **Sanity check:** if a test passes unexpectedly, briefly break behavior to confirm it fails (then undo).
- **Architecture direction:** keep dependencies clean (API → Core; Infrastructure implements ports).
- **API contract boundary:** public API DTOs must never expose domain entities.

---

## Dependencies & new tools
- Prefer built-in .NET libraries.
- If you need a new package/tool:
  1) Propose it in PLAN step (why + minimal alternative)
  2) Wait for approval
  3) Add it in a dedicated microstep

---

## Proof command defaults
Use these unless the microstep specifies others:
- `dotnet build`
- `dotnet test`

Optional (only if introduced/configured in this repo):
- `dotnet format --verify-no-changes`
- PowerShell wrappers: `pwsh ./scripts/test.unit.ps1`, `pwsh ./scripts/test.int.ps1`

---

## Plan maintenance
- Do not “silently” change scope or requirements.
- If the plan needs adjustment: propose a doc-only microstep (update `docs/plan*`) and wait for approval before coding.

---

## Optional: folder-specific overrides
If `AGENTS.override.md` exists in a subfolder, follow the nearest override for that subtree. Keep overrides short.

# AGENTS.md — working agreement for Codex (TDD, no microsteps)

## Read order
1) `AGENTS.md` (this file)
2) `plan.md`
3) `requirements.md`

Rule priority: `AGENTS.md` → `plan.md` → `requirements.md` → everything else.

## Operating mode (important)
- We do NOT use “milestones/microsteps” or “STOP for approval after each step”.
- Instead, work in **TDD slices**:
  - A slice = one small user-visible behavior (or one small contract change) delivered via tests-first.

## Session loop (every run)
### 0) Clarify (only if needed)
- If the request is ambiguous, ask up to **3** targeted questions.
- Otherwise state assumptions in 1–3 bullets and proceed.

### 1) Plan (short, then execute)
Provide a short plan (3–7 bullets), including:
- The next **slice**
- Which test level you’ll use (unit vs integration vs e2e)
- Proof command(s)

Then start implementing immediately.

### 2) Enforce TDD (non-negotiable)
For each slice:
- **CONTRACT** (optional): minimal DTOs/interfaces so tests compile (no behavior)
- **RED**: add/adjust tests first; run `dotnet test` and ensure it fails for the right reason
- **GREEN**: minimal implementation to pass
- **REFACTOR**: only after green

### 3) Proof gates (must run)
- Always run: `dotnet test`
- If a change is “big-ish” (new project, infra, migrations): also run `dotnet build`

### 4) Report (end of run)
Summarize:
- What changed (files + why)
- Commands run (pass/fail)
- Follow-ups / next slice

## When to ask for explicit approval (only these cases)
Ask before:
- Adding new production dependencies
- DB schema migrations / destructive data changes
- Breaking API contract changes
- Running commands that touch outside the repo workspace / enable network unexpectedly

Everything else: proceed autonomously inside the repo + tests.

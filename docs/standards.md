# docs/standards.md — Engineering Standards (Codex + Humans)
_Last reviewed: 2025-12-31_

## Purpose
Stable, repo-wide conventions for building a modern .NET API with small, verifiable steps.
- `docs/plan.md` controls sequencing/phases.
- This file controls “how we implement” once a phase allows it.
- Codex note: Codex auto-loads `AGENTS.md`. `AGENTS.md` must instruct Codex to follow this file.

---

## Global workflow rules (always)
- Prefer **small diffs** over rewrites. (Smaller changes are easier to review and less risky.)
- Every change must include a **proof step**: `dotnet build` and/or `dotnet test`.
- If a change can’t be verified quickly, split it.

---

## Testing & TDD (default for new behavior)
**Rule**
- For new behavior, follow **CONTRACT → RED → GREEN → REFACTOR**:
  1) **CONTRACT** (optional): add minimal interface/DTO so tests compile (no logic).
  2) **RED**: write/extend tests first; run `dotnet test` and confirm they fail for the right reason.
  3) **GREEN**: implement the minimum to pass.
  4) **REFACTOR**: cleanup only after green; re-run tests.
- Keep one behavior per loop.

**Test quality guardrails**
- Assert **observable behavior**, not internal implementation details.
- Avoid over-mocking: mock only external boundaries (DB/clock/HTTP).
- Include at least one negative/boundary case for non-trivial logic.
- Sanity check: if a test passes unexpectedly, briefly break the implementation (or substitute an obviously wrong stub) and confirm the test fails.

**Optional periodic gate**
- Run mutation testing (Stryker.NET) before closing a milestone/phase.

---

## The 10 Core Standards

### 1) Automated formatting + consistent style
- `.editorconfig` is the source of truth.
- Use `dotnet format` (or CI) so formatting is consistent.  
**Proof**: `dotnet format --verify-no-changes`

### 2) Analyzers are enabled and enforced
- Enable .NET analyzers; keep warnings under control.
- Prefer enforcement via build/CI.

### 3) Nullable reference types are enabled
- Nullable is enabled for new code.
- Fix warnings rather than broad suppression.

### 4) Async-first (avoid sync-over-async)
- Don’t use `.Result`/`.Wait()` in request paths.
- Prefer async I/O end-to-end.

### 5) DI by default; lifetime-correct services
- Use constructor injection.
- Avoid service-locator patterns.
- Respect lifetimes (avoid scoped captured by singleton).
- Group registrations via `AddXyz()` extension methods.

### 6) Configuration via Options pattern (typed + validated)
- Use Options pattern for related settings.
- Validate at startup when possible (fail fast).

### 7) API errors are consistent (ProblemDetails)
- Standardize errors (ProblemDetails).
- Don’t leak internal exception details.
- Exceptions aren’t used as control flow.

### 8) Logging is structured; observability is phase-gated
- Use `ILogger<T>` and structured logging.
- Don’t log secrets/PII.
- If/when enabled: OpenTelemetry/OTLP (traces/metrics/logs).

### 9) EF Core data access avoids common pitfalls
- `DbContext` is short-lived (unit of work), not shared across threads.
- Use no-tracking queries **only** for read-only paths.
- Watch for N+1 patterns.
- Prefer integration tests for persistence boundaries.

### 10) Production readiness baseline (enable as phases allow)
- Health checks: liveness/readiness endpoints.
- Rate limiting for public endpoints.
- OpenAPI docs for discoverability and contract testing.
- Secrets/security: don’t store secrets in repo; use platform secret mechanisms.
- Outbound HTTP: use `IHttpClientFactory` and (when needed) resilience handlers.

### 11) “API DTOs never leak domain entities.”
- API project uses request/response models in Api/Contracts
- Domain entities live in Core, never returned directly
- Mapping happens in the API or application boundary (your choice), but the rule is: no domain types in public contracts
---

## Modern “production-ready” additions (recommended defaults)
These are frequently important in real APIs; keep them phase-gated in `docs/plan.md`:

- **Security posture**: authentication/authorization, least privilege; avoid secret leakage.
- **Input validation**: enforce request validation early; test negative cases.
- **Resilience/timeouts**: timeouts on outbound calls; retry only where safe and idempotent.
- **CI gates**: run format + build + tests on PR; keep changes reviewable.

---

## Sources (verify yourself)

**Codex / AI workflow**
- OpenAI Codex — `AGENTS.md` discovery + overrides:  
  https://developers.openai.com/codex/guides/agents-md/
- Google Cloud — AI coding best practices (Published **Oct 7, 2025**):  
  https://cloud.google.com/blog/topics/developers-practitioners/five-best-practices-for-using-ai-coding-assistants
- Microsoft .NET Blog — AI-generated code review (Published **Oct 7, 2025**):  
  https://devblogs.microsoft.com/dotnet/developer-and-ai-code-reviewer-reviewing-ai-generated-code-in-dotnet/
- Nimble Approach — TDD + AI (Published **Nov 28, 2025**):  
  https://nimbleapproach.com/blog/how-to-use-test-driven-development-for-better-ai-coding-outputs/
- TestDouble — mutation testing + coding agents (Published **Oct 21, 2025**):  
  https://testdouble.com/insights/keep-your-coding-agent-on-task-with-mutation-testing
- Google Engineering Practices — small CLs:  
  https://google.github.io/eng-practices/review/developer/small-cls.html

**Repo docs as “standards”/guidelines**
- GitHub Docs — “Setting guidelines for repository contributors” (why and where to put repo guidance docs):  
  https://docs.github.com/en/communities/setting-up-your-project-for-healthy-contributions/setting-guidelines-for-repository-contributors

**.NET / ASP.NET Core / EF Core**
- dotnet format: https://learn.microsoft.com/dotnet/core/tools/dotnet-format
- Code analysis overview: https://learn.microsoft.com/dotnet/fundamentals/code-analysis/overview
- Nullable reference types: https://learn.microsoft.com/dotnet/csharp/nullable-references
- ASP.NET Core best practices: https://learn.microsoft.com/aspnet/core/fundamentals/best-practices?view=aspnetcore-10.0
- DI guidelines: https://learn.microsoft.com/dotnet/core/extensions/dependency-injection-guidelines
- Options pattern: https://learn.microsoft.com/aspnet/core/fundamentals/configuration/options?view=aspnetcore-10.0
- Error handling (ProblemDetails): https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0
- Logging: https://learn.microsoft.com/dotnet/core/extensions/logging
- Observability with OTel: https://learn.microsoft.com/dotnet/core/diagnostics/observability-with-otel
- EF Core DbContext configuration: https://learn.microsoft.com/ef/core/dbcontext-configuration/
- EF Core performance: https://learn.microsoft.com/ef/core/performance/advanced-performance-topics
- Health checks: https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0
- Rate limiting: https://learn.microsoft.com/aspnet/core/performance/rate-limit?view=aspnetcore-10.0
- OpenAPI: https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0
- Secrets in dev: https://learn.microsoft.com/aspnet/core/security/app-secrets?view=aspnetcore-10.0
- HttpClientFactory: https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory
- HTTP resilience: https://learn.microsoft.com/dotnet/core/resilience/http-resilience
- Mutation testing (.NET): https://learn.microsoft.com/dotnet/core/testing/mutation-testing

# Repo structure (minimal)

> Notes:
> - `docker-compose.yml` may exist early, but should only be *used* when project requires it.
> - Keep `docs/` authoritative for planning and standards.

```text
repo-root/
  README.md
  .editorconfig
  .gitignore
  .gitattributes
  .dockerignore
  .env.example
  docker-compose.yml        (used starting Phase 2)

  /docs/
    requirements.md
    standards.md
    plan.md
    plan.phase0.md
    plan.phase1.md
    plan.phase2.md          (placeholder)
    plan.phase3.md          (placeholder)
    plan.phase4.md          (placeholder)
    repo-structure.md
    api.md                  (created in Phase 1)

  /scripts/
    dev.up.ps1              (Phase 2+)
    dev.down.ps1            (Phase 2+)
    test.unit.ps1
    test.int.ps1            (Phase 2+)

  /src/
    /Api/
      Api.csproj
      Program.cs
      appsettings.json
      appsettings.Development.json
      /Endpoints/
      /Contracts/
      /Background/          (worker-in-API for v1)

    /Core/
      Core.csproj
      /Domain/
      /Application/
        /Abstractions/
        /Features/
        /Common/

    /Infrastructure/
      Infrastructure.csproj
      /GitHub/
      /Seed/
      /Postgres/
      /Config/

  /tests/
    /Unit/
      UnitTests.csproj
    /Integration/
      IntegrationTests.csproj
    /E2E/
      E2ETests.csproj

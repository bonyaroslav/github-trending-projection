# Trending Snapshots API Contract (v1)

This REST API stores **time-frozen snapshots** of GitHub “Trending” repositories.
A snapshot is immutable in content (the repo list) once created; only snapshot metadata can be updated.

This document is the contract target for tests (TDD). In Phase 1, implementations may use in-memory storage.

---

## 1) Conventions

- Base URL: `/api/v1`
- Request/response format: `application/json; charset=utf-8`
- Timestamps: ISO 8601 UTC strings (e.g., `2026-01-02T18:45:00Z`)
- Correlation: server SHOULD echo `X-Correlation-Id` if provided by client

### Versioning
- Version is in the URL (`/api/v1`). Backward-incompatible changes require a new version.

---

## 2) Pagination (list endpoints)

Query parameters:
- `page` (optional, default `1`, 1-based, min `1`)
- `pageSize` (optional, default `20`, min `1`, max `100`)

All list responses:
```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 0
}
````

---

## 3) Errors (ProblemDetails, RFC 9457)

All non-2xx errors MUST use:

* `Content-Type: application/problem+json`

Shape (extensions allowed):

```json
{
  "type": "string",
  "title": "string",
  "status": 400,
  "detail": "string",
  "instance": "string",
  "errors": {
    "fieldName": ["error message"]
  },
  "traceId": "string"
}
```

Notes:

* `errors` is OPTIONAL and used for validation failures.
* `traceId` is OPTIONAL (recommended).
* `type` SHOULD be a stable identifier (URI or URI-like), e.g.:

  * `/problems/validation`
  * `/problems/not-found`
  * `/problems/conflict`
  * `/problems/dependency-failure`

---

## 4) Resource & ID Rules

### Snapshot identity

* `snapshotId` is server-generated (opaque). Format: `ulid` or `guid` (choose one for implementation).
* A snapshot represents a **single capture moment** (`capturedAt`) from a given `source`.

### Repository identity inside a snapshot

* `repoId` is stable across snapshots (choose one):

  * GitHub GraphQL `node_id` (recommended), OR
  * `fullName` in the form `owner/name`
* Within a snapshot, `repoId` MUST be unique.

### Snapshot immutability

* The repository list captured in a snapshot is immutable.
* Updating a snapshot is limited to metadata (e.g., `name`, `notes`).
* To change repo contents, create a new snapshot.

---

## 5) DTOs

### SnapshotSummary

* `id` (string, required) — snapshotId
* `capturedAt` (string, required, ISO 8601 UTC)
* `source` (string, required) — e.g. `"github-trending"`
* `name` (string, optional)
* `itemCount` (int, required)

### SnapshotDetail

Same fields as `SnapshotSummary` for v1.

### SnapshotCreateRequest

* `source` (string, optional, default `"github-trending"`)
* `name` (string, optional)
* `notes` (string, optional)
* `capturedAt` (string, optional) — if omitted, server sets to “now”
* `repositories` (array, optional) — if provided, snapshot is created from this inline list (no external calls)

Validation:

* If `repositories` is provided, it MUST be non-empty.

### SnapshotUpdateRequest

* `name` (string, optional)
* `notes` (string, optional)

Validation:

* Unknown fields -> 400 ProblemDetails (`/problems/validation`)

### RepositorySnapshot

A repository as captured in a snapshot:

* `id` (string, required) — repoId
* `rank` (int, required, min `1`) — position in trending list (1 is top)
* `name` (string, required)
* `owner` (string, required)
* `fullName` (string, required) — `owner/name`
* `description` (string, optional)
* `language` (string, optional)
* `stars` (int, required, min `0`)
* `forks` (int, required, min `0`)
* `url` (string, required)
* `repoUpdatedAt` (string, required, ISO 8601 UTC) — from GitHub

---

## 6) Endpoints

### GET /health

Liveness check.

Response 200:

```json
{ "status": "ok" }
```

### GET /version

Service version.

Response 200:

```json
{ "version": "string" }
```

---

## 7) Snapshots (CRUD)

### POST /snapshots

Create a new snapshot.

Request:

```json
{
  "source": "github-trending",
  "name": "My snapshot",
  "notes": "optional",
  "capturedAt": "2026-01-02T18:45:00Z",
  "repositories": [
    {
      "id": "string",
      "rank": 1,
      "name": "repo",
      "owner": "org",
      "fullName": "org/repo",
      "description": "optional",
      "language": "optional",
      "stars": 123,
      "forks": 45,
      "url": "https://github.com/org/repo",
      "repoUpdatedAt": "2025-12-30T10:00:00Z"
    }
  ]
}
```

Responses:

* 201 Created + `Location: /api/v1/snapshots/{snapshotId}`

  * Body: SnapshotDetail
* 400 ProblemDetails (`/problems/validation`)
* 409 ProblemDetails (`/problems/conflict`) — e.g., duplicate snapshot uniqueness rule (implementation-defined)
* 503 ProblemDetails (`/problems/dependency-failure`) — only if server fetch mode is implemented and upstream fails

Response 201 body:

```json
{
  "id": "string",
  "capturedAt": "2026-01-02T18:45:00Z",
  "source": "github-trending",
  "name": "My snapshot",
  "itemCount": 50
}
```

### GET /snapshots

List snapshots (newest capturedAt first).

Responses:

* 200 list of SnapshotSummary
* 400 ProblemDetails (invalid pagination)

Response 200:

```json
{
  "items": [ { /* SnapshotSummary */ } ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 0
}
```

### GET /snapshots/{snapshotId}

Get snapshot details.

Responses:

* 200 SnapshotDetail
* 404 ProblemDetails (`/problems/not-found`)

### PUT /snapshots/{snapshotId}

Replace snapshot metadata (does NOT change repo list).

Request:

```json
{
  "name": "New name",
  "notes": "New notes"
}
```

Responses:

* 200 SnapshotDetail
* 400 ProblemDetails (`/problems/validation`)
* 404 ProblemDetails (`/problems/not-found`)

### PATCH /snapshots/{snapshotId}

Partial update snapshot metadata (does NOT change repo list).

Request:

```json
{ "name": "New name" }
```

Responses:

* 200 SnapshotDetail
* 400 ProblemDetails (`/problems/validation`)
* 404 ProblemDetails (`/problems/not-found`)

### DELETE /snapshots/{snapshotId}

Delete a snapshot.

Responses:

* 204 No Content
* 404 ProblemDetails (`/problems/not-found`)

---

## 8) Snapshot Repositories

### GET /snapshots/{snapshotId}/repositories

List repositories in a snapshot.

Default ordering:

* `rank` ascending (1..N)

Responses:

* 200 list of RepositorySnapshot
* 400 ProblemDetails (invalid pagination)
* 404 ProblemDetails (unknown snapshot)

Response 200:

```json
{
  "items": [ { /* RepositorySnapshot */ } ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 0
}
```

### GET /snapshots/{snapshotId}/repositories/{repoId}

Get a single repo entry within a snapshot.

Responses:

* 200 RepositorySnapshot
* 404 ProblemDetails (`/problems/not-found`) — unknown snapshot or repoId

---

<!--
References (for future readers)

- RFC 9457 (Problem Details for HTTP APIs; obsoletes RFC 7807):
  https://www.rfc-editor.org/rfc/rfc9457.html
- IANA media type registration for application/problem+json:
  https://www.iana.org/assignments/media-types/application/problem%2Bjson
- HTTP 201 Created (resource created; typically used with POST):
  https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/201
- Location header semantics (meaningful with 201 and redirects):
  https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Location
- Nested collections guidance (naming & hierarchy patterns):
  https://google.aip.dev/122
- ASP.NET Core API error handling + ProblemDetails (AddProblemDetails):
  https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0
-->


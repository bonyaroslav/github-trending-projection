````markdown
# Trending Snapshots API Contract (v1)

This REST API stores **time-frozen snapshots** of GitHub “Trending” repositories.
A snapshot is **immutable in content** (the list of repositories and their captured fields) once created; only snapshot **metadata** can be updated.

This document is the **contract target for tests (TDD)**. In Phase 1, implementations may use in-memory storage.

---

## 1) Conventions

- Base URL: `/api/v1`
- Request/response format: `application/json; charset=utf-8`
- Error format: `application/problem+json` (ProblemDetails, RFC 9457)
- Timestamps: ISO 8601 UTC strings (e.g., `2026-01-02T18:45:00Z`)
- Correlation:
  - Client MAY send `X-Correlation-Id`
  - Server SHOULD echo `X-Correlation-Id` if provided
- IDs:
  - `snapshotId`: server-generated opaque string (GUID/ULID acceptable)
  - `repoId`: GitHub GraphQL repository `id` (opaque string). Clients MUST URL-encode it when used in path segments.

### Versioning policy
- Backward-compatible changes (new optional fields/endpoints) are allowed within `v1`.
- Breaking changes require a new major version (`/api/v2`).

---

## 2) Pagination (list endpoints)

Query parameters:
- `page` (optional, default `1`, 1-based, min `1`)
- `pageSize` (optional, default `20`, min `1`, max `100`)

All list responses return:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 0,
  "totalPages": 0
}
````

Notes:

* `totalItems` / `totalPages` MUST be provided for v1 simplicity.
* If calculating totals becomes expensive later, v2 may switch to cursor pagination.

---

## 3) Errors (ProblemDetails, RFC 9457)

All non-2xx errors MUST return `application/problem+json`.

Shape (extensions allowed):

```json
{
  "type": "https://example.com/problems/validation-error",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/v1/snapshots",
  "errors": {
    "fieldName": ["error message"]
  },
  "traceId": "00-...-..."
}
```

Guidelines:

* `errors` is used for validation problems (400).
* `traceId` is recommended (maps well to ASP.NET Core).
* Server MUST NOT leak secrets (tokens, headers, raw upstream payloads).

Common status codes:

* `400` validation / malformed request
* `401` missing/invalid auth (if enabled)
* `404` not found
* `409` conflict (see duplicate snapshot rule below)
* `429` rate limited (server-side capture operations only)
* `503` external dependency unavailable (server-side capture operations only)
* `500` unexpected

---

## 4) Invariants

### Snapshot immutability

* After a snapshot is created, its **repository items MUST NOT be updated**.
* Only snapshot metadata (`name`, `notes`) is mutable.

### Duplicate snapshot conflict rule (v1)

To keep behavior deterministic for tests, the server enforces:

* Unique key: `(source, capturedAt)`
  If a client attempts to create a snapshot with the same `source` and the same `capturedAt` value as an existing snapshot, the server MUST return `409 Conflict`.

Notes:

* For server-side capture endpoints, `capturedAt` is set by the server. Conflicts are unlikely but still defined.

---

## 5) DTOs

### 5.1 SnapshotSummary

* `id` (string, required) — snapshotId
* `capturedAt` (string, required, ISO 8601 UTC)
* `source` (string, required) — e.g. `"github-trending"`, `"manual"`
* `name` (string, optional)
* `itemCount` (int, required)

Example:

```json
{
  "id": "01JH7ZV1V2Q4J6R3P0Q9H0M9FW",
  "capturedAt": "2026-01-02T18:45:00Z",
  "source": "github-trending",
  "name": "Evening capture",
  "itemCount": 25
}
```

### 5.2 SnapshotDetail

Same as SnapshotSummary PLUS:

* `notes` (string, optional)

Example:

```json
{
  "id": "01JH7ZV1V2Q4J6R3P0Q9H0M9FW",
  "capturedAt": "2026-01-02T18:45:00Z",
  "source": "github-trending",
  "name": "Evening capture",
  "notes": "Captured after work",
  "itemCount": 25
}
```

### 5.3 SnapshotCreateRequest (manual snapshot; no external fetch)

Creates a snapshot from an inline list of repositories provided by the client.

* `source` (string, optional, default `"manual"`)
* `capturedAt` (string, optional, ISO 8601 UTC; default = server time)
* `name` (string, optional)
* `notes` (string, optional)
* `repositories` (array, required, min 1)

```json
{
  "source": "manual",
  "capturedAt": "2026-01-02T18:45:00Z",
  "name": "My snapshot",
  "notes": "Optional notes",
  "repositories": [
    {
      "repoId": "MDEwOlJlcG9zaXRvcnkxMjM0NTY3OA==",
      "rank": 1,
      "owner": "octocat",
      "name": "hello-world",
      "fullName": "octocat/hello-world",
      "description": "Example repo",
      "language": "C#",
      "stars": 1234,
      "forks": 56,
      "url": "https://github.com/octocat/hello-world",
      "repoUpdatedAt": "2025-12-31T12:00:00Z"
    }
  ]
}
```

Validation:

* `repositories[].rank` must be unique within snapshot and start at 1..N (server MAY allow gaps, but SHOULD reject for v1).
* `repositories[].repoId` must be unique within snapshot.
* `stars`/`forks` must be `>= 0`.
* If `capturedAt` is provided, it must be valid UTC ISO 8601.

### 5.4 SnapshotMetadataUpdateRequest (PATCH)

Updates snapshot metadata only.

* `name` (string, optional, nullable)
* `notes` (string, optional, nullable)

Semantics:

* Fields omitted are unchanged.
* Fields present with `null` clear the value.

```json
{
  "name": "Renamed snapshot",
  "notes": null
}
```

### 5.5 RepositoryInSnapshot

* `repoId` (string, required)
* `rank` (int, required)
* `owner` (string, required)
* `name` (string, required)
* `fullName` (string, required)
* `description` (string, optional)
* `language` (string, optional)
* `stars` (int, required)
* `forks` (int, required)
* `url` (string, required)
* `repoUpdatedAt` (string, optional, ISO 8601 UTC)

Example:

```json
{
  "repoId": "MDEwOlJlcG9zaXRvcnkxMjM0NTY3OA==",
  "rank": 1,
  "owner": "octocat",
  "name": "hello-world",
  "fullName": "octocat/hello-world",
  "description": "Example repo",
  "language": "C#",
  "stars": 1234,
  "forks": 56,
  "url": "https://github.com/octocat/hello-world",
  "repoUpdatedAt": "2025-12-31T12:00:00Z"
}
```

### 5.6 CaptureSnapshotRequest (server-side capture; external fetch)

Used by the server to capture Trending + enrich with GitHub GraphQL, then create a new snapshot.

* `source` (string, optional, default `"github-trending"`)
* `name` (string, optional)
* `notes` (string, optional)
* `limit` (int, optional, default `25`, min `1`, max `100`)
* `language` (string, optional) — filter at seed level if supported
* `since` (string, optional, enum: `"daily" | "weekly" | "monthly"`, default `"daily"`)

```json
{
  "source": "github-trending",
  "name": "Morning capture",
  "notes": "Auto captured",
  "limit": 25,
  "language": "csharp",
  "since": "daily"
}
```

---

## 6) Endpoints

### 6.1 Health & Version

#### GET /health

Liveness check.

Response `200`:

```json
{ "status": "ok" }
```

#### GET /version

Service version.

Response `200`:

```json
{
  "service": "trending-snapshots",
  "version": "1.0.0",
  "commit": "optional"
}
```

---

## 6.2 Snapshots (CRUD)

#### GET /snapshots

List snapshots (newest first).

Query: pagination (`page`, `pageSize`)

Response `200`:

```json
{
  "items": [
    {
      "id": "01JH7ZV1V2Q4J6R3P0Q9H0M9FW",
      "capturedAt": "2026-01-02T18:45:00Z",
      "source": "github-trending",
      "name": "Evening capture",
      "itemCount": 25
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 1,
  "totalPages": 1
}
```

#### POST /snapshots

Create a **manual snapshot** from an inline repositories list (no external fetch).

Request body: `SnapshotCreateRequest`

Responses:

* `201 Created` with `SnapshotDetail`
* `400` validation
* `409` if `(source, capturedAt)` already exists

Response `201`:

```json
{
  "id": "01JH7ZV1V2Q4J6R3P0Q9H0M9FW",
  "capturedAt": "2026-01-02T18:45:00Z",
  "source": "manual",
  "name": "My snapshot",
  "notes": "Optional notes",
  "itemCount": 1
}
```

#### GET /snapshots/{snapshotId}

Get snapshot metadata.

Response `200`: `SnapshotDetail`
Response `404` if not found.

#### PATCH /snapshots/{snapshotId}

Update snapshot metadata only.

Request: `SnapshotMetadataUpdateRequest`

Responses:

* `200` with updated `SnapshotDetail`
* `400` validation
* `404` not found

#### DELETE /snapshots/{snapshotId}

Delete snapshot and all its repository items.

Responses:

* `204 No Content`
* `404` not found

Notes:

* Delete is allowed in v1 for simplicity (even though snapshots are immutable, they can be removed).

---

## 6.3 Snapshot repositories (read-only)

#### GET /snapshots/{snapshotId}/repositories

List repositories captured in a snapshot.

Query parameters:

* `page`, `pageSize` (pagination)
* `q` (optional) — substring match against `fullName` or `description` (implementation-defined but stable)
* `language` (optional) — exact or normalized match (implementation-defined but stable)
* `sort` (optional, enum: `"rank" | "stars" | "forks"`, default `"rank"`)
* `order` (optional, enum: `"asc" | "desc"`, default depends on sort: `rank=asc`, others `desc`)

Response `200`:

```json
{
  "items": [
    {
      "repoId": "MDEwOlJlcG9zaXRvcnkxMjM0NTY3OA==",
      "rank": 1,
      "owner": "octocat",
      "name": "hello-world",
      "fullName": "octocat/hello-world",
      "description": "Example repo",
      "language": "C#",
      "stars": 1234,
      "forks": 56,
      "url": "https://github.com/octocat/hello-world",
      "repoUpdatedAt": "2025-12-31T12:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 1,
  "totalPages": 1
}
```

Responses:

* `404` if snapshot not found

#### GET /snapshots/{snapshotId}/repositories/{repoId}

Get a single repository item in a snapshot by `repoId`.

Notes:

* `repoId` MUST be URL-encoded by clients when used in the path.

Response `200`: `RepositoryInSnapshot`
Response `404` if not found (snapshot or repo item).

#### GET /snapshots/{snapshotId}/repositories/by-full-name?fullName={owner}/{name}

Convenience lookup when clients only have `owner/name`.

* `fullName` (required), example: `octocat/hello-world` (URL-encoded)

Response `200`: `RepositoryInSnapshot`
Response `404` if not found.

---

## 6.4 Server-side capture (creates a snapshot)

These endpoints may call external systems (GitHub Trending + GitHub GraphQL). Therefore they MAY return:

* `429` if rate-limited (server-side)
* `503` if upstream dependency is unavailable
* `502` if upstream returns invalid/unexpected response

#### POST /snapshots:capture

Capture Trending + enrich + persist a new snapshot **synchronously**.

Request body: `CaptureSnapshotRequest` (optional; defaults apply)

Responses:

* `201 Created` with `SnapshotDetail`
* `429` rate limit
* `503` dependency unavailable
* `409` if `(source, capturedAt)` conflicts (rare)

Example request:

```json
{
  "name": "Morning capture",
  "notes": "Auto captured",
  "limit": 25,
  "language": "csharp",
  "since": "daily"
}
```

Example response `201`:

```json
{
  "id": "01JH80B4N2T9E1JX7H5G6Q9Z2M",
  "capturedAt": "2026-01-03T09:00:00Z",
  "source": "github-trending",
  "name": "Morning capture",
  "notes": "Auto captured",
  "itemCount": 25
}
```

---

## 6.5 Async capture runs (Hangfire-friendly)

### SyncRun DTOs

#### SyncRunSummary

* `id` (string, required)
* `status` (string, required, enum: `queued | running | succeeded | failed`)
* `requestedAt` (string, required)
* `startedAt` (string, optional)
* `finishedAt` (string, optional)
* `snapshotId` (string, optional) — present when succeeded
* `error` (string, optional) — sanitized summary when failed

#### SyncRunCreateRequest

Same shape as `CaptureSnapshotRequest`.

---

#### POST /sync-runs

Enqueue an async capture run.

Request body: `SyncRunCreateRequest` (optional; defaults apply)

Response:

* `202 Accepted` with `SyncRunSummary`

Example `202`:

```json
{
  "id": "run_01JH80D7N9X2Q3K4M5P6R7S8T9",
  "status": "queued",
  "requestedAt": "2026-01-03T09:05:00Z",
  "startedAt": null,
  "finishedAt": null,
  "snapshotId": null,
  "error": null
}
```

#### GET /sync-runs

List async capture runs (newest first).

Pagination supported.

Response `200`:

```json
{
  "items": [
    {
      "id": "run_01JH80D7N9X2Q3K4M5P6R7S8T9",
      "status": "succeeded",
      "requestedAt": "2026-01-03T09:05:00Z",
      "startedAt": "2026-01-03T09:05:02Z",
      "finishedAt": "2026-01-03T09:05:40Z",
      "snapshotId": "01JH80B4N2T9E1JX7H5G6Q9Z2M",
      "error": null
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 1,
  "totalPages": 1
}
```

#### GET /sync-runs/{runId}

Get run status.

Response `200`: `SyncRunSummary`
Response `404` if not found.

---

## 7) Notes for implementation & tests

* Tests SHOULD enforce snapshot immutability (no endpoint modifies repo items after creation).
* Manual snapshot creation (`POST /snapshots`) is recommended for deterministic tests.
* Capture endpoints (`/snapshots:capture`, `/sync-runs`) can be covered with integration tests using fakes/mocks for:

  * Trending seed provider
  * GitHub GraphQL client
* If auth is added later, v1 MAY introduce `401/403` without changing routes.

---

## 8) References


<!--
References (for future readers)

* Problem Details for HTTP APIs (RFC 9457): [https://www.rfc-editor.org/rfc/rfc9457](https://www.rfc-editor.org/rfc/rfc9457)
* ASP.NET Core ProblemDetails: [https://learn.microsoft.com/aspnet/core/web-api/handle-errors?view=aspnetcore-10.0](https://learn.microsoft.com/aspnet/core/web-api/handle-errors?view=aspnetcore-10.0)

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


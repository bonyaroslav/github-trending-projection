# API Contract (v1)

This document defines the initial REST API contract. It is the test target for Phase 1 (no persistence).

## Conventions

- Base URL: `/`
- Content type: `application/json`
- All timestamps are ISO 8601 UTC strings.
- Error format uses ProblemDetails (see below).

## Pagination (list endpoints)

Query parameters:
- `page` (optional, default `1`, 1-based)
- `pageSize` (optional, default `20`, min `1`, max `100`)

## Errors (ProblemDetails)

All errors use this shape:

```json
{
  "type": "string",
  "title": "string",
  "status": 400,
  "detail": "string",
  "instance": "string",
  "errors": {
    "fieldName": [
      "error message"
    ]
  }
}
```

- `errors` is optional and used for validation failures.
- 404 uses ProblemDetails with `status` 404 and a short `title`.

## DTOs

### RepositorySummary

- `id` (string, required, opaque)
- `name` (string, required)
- `owner` (string, required)
- `fullName` (string, required)
- `description` (string, optional)
- `language` (string, optional)
- `stars` (int, required)
- `forks` (int, required)
- `url` (string, required)
- `updatedAt` (string, required, ISO 8601)

### RepositoryDetail

Same fields as `RepositorySummary` for Phase 1.

## Endpoints

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

### GET /repositories

List repositories (in-memory in Phase 1).

Query parameters:
- `page` (optional)
- `pageSize` (optional)

Response 200:

```json
{
  "items": [ { /* RepositorySummary */ } ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 0
}
```

### GET /repositories/{id}

Get repository details by id.

Response 200:

```json
{ /* RepositoryDetail */ }
```

Response 404:

ProblemDetails with `status` 404.

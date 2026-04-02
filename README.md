# Document Processing API
[![CI](https://github.com/juhagh/document-processing-api/actions/workflows/ci.yml/badge.svg)](https://github.com/juhagh/document-processing-api/actions/workflows/ci.yml)

An event-driven ASP.NET Core backend that accepts document-analysis jobs through a Web API, stores job state in PostgreSQL, and processes jobs asynchronously using RabbitMQ and a background worker.

## Overview

This portfolio project is built to demonstrate employable .NET backend skills beyond basic CRUD APIs, including:

- asynchronous job processing
- worker-based background execution
- message-driven architecture
- explicit job lifecycle management
- PostgreSQL persistence with EF Core
- Dockerized local infrastructure
- layered architecture
- automated testing
- GitHub Actions CI with PostgreSQL and RabbitMQ services

In the current version, the API accepts **text input** rather than real file uploads. A client submits a document-processing job, the API stores the job, marks it as queued, publishes a RabbitMQ message, and a worker processes the job asynchronously. The client can then query job status and results later.

## What This Project Demonstrates

This project is designed to show practical backend skills that map to real systems:

- ASP.NET Core Web API design
- EF Core + PostgreSQL persistence
- RabbitMQ message publishing and consumption
- Background worker processing
- explicit domain state transitions
- polling-based async job tracking
- clean separation of concerns across layers
- Docker Compose local development setup
- integration and domain testing
- GitHub Actions CI with PostgreSQL and RabbitMQ services

## Current Tech Stack

- .NET 10
- ASP.NET Core Web API
- Worker Service
- EF Core 10
- PostgreSQL
- RabbitMQ
- Docker / Docker Compose
- xUnit
- GitHub Actions

## Solution Structure

```text
src/
  DocumentProcessing.Api
  DocumentProcessing.Application
  DocumentProcessing.Domain
  DocumentProcessing.Infrastructure
  DocumentProcessing.Worker

tests/
  DocumentProcessing.Api.Tests
  DocumentProcessing.Domain.Tests
  DocumentProcessing.Worker.Tests
```

### Layer Responsibilities

- **DocumentProcessing.Api**
  HTTP endpoints, request/response contracts, JSON configuration, and API wiring.

- **DocumentProcessing.Application**
  Use cases, DTOs, service abstractions, repository abstractions, and messaging abstractions.

- **DocumentProcessing.Domain**
  Core business model, job lifecycle rules, and domain invariants.

- **DocumentProcessing.Infrastructure**
  EF Core persistence, repository implementations, RabbitMQ publisher, and infrastructure registration.

- **DocumentProcessing.Worker**
  Background consumer that reads RabbitMQ messages, loads jobs from the database, performs analysis, and updates job state.

## Current Job Lifecycle

The core aggregate is `DocumentJob`.

A job moves through these states:
- Pending
- Queued
- Processing
- Completed
- Failed

Current transition rules:
- Pending -> Queued
- Queued -> Processing
- Processing -> Completed
- Processing -> Failed

For v1, Failed is treated as a terminal state.

## Current API Endpoints

### Create a job

`POST /api/jobs`

Accepts text input and returns a queued job response.

### Get a job by id

`GET /api/jobs/{id}`

Returns the current status and any available analysis results.

### List jobs

`GET /api/jobs`

Returns jobs ordered by SubmittedAtUtc descending.

### Example Request

```json
{
  "inputText": "This is a test document.\nIt has multiple lines.\n"
}
```

### Example Response After Submission

```json
{
  "id": 144,
  "status": "Queued",
  "inputText": "This is a test document.\nIt has multiple lines.\n",
  "submittedAtUtc": "2026-04-02T04:17:00.688481Z",
  "updatedAtUtc": "2026-04-02T04:17:01.133146Z",
  "completedAtUtc": null,
  "errorMessage": null,
  "wordCount": null,
  "characterCount": null,
  "lineCount": null,
  "keywordHits": null,
  "category": null,
  "summary": null
}
```

### Example Response After Processing

```json
{
  "id": 144,
  "status": "Completed",
  "inputText": "This is a test document.\nIt has multiple lines.\n",
  "submittedAtUtc": "2026-04-02T04:17:00.688481Z",
  "updatedAtUtc": "2026-04-02T04:17:35.764846Z",
  "completedAtUtc": "2026-04-02T04:17:35.764846Z",
  "errorMessage": null,
  "wordCount": 9,
  "characterCount": 48,
  "lineCount": 2,
  "keywordHits": 0,
  "category": "General",
  "summary": "This is a test document.\nIt has multiple lines.\n"
}
```

### Current Processing Flow
1. Client submits a document job to `POST /api/jobs`, job initial state is `Pending`
2. API creates the job in PostgreSQL
3. API marks the job as `Queued`
4. API publishes a `ProcessDocumentJobMessage` to RabbitMQ
5. Worker consumes the message
6. Worker loads the job from PostgreSQL
7. Worker marks the job as `Processing`
8. Worker performs simple text analysis
9. Worker marks the job as `Completed` or `Failed`
10. Client checks job status using `GET /api/jobs/{id}` or `GET /api/jobs`

### Current Analysis Output

**The worker currently produces:**
- word count
- character count
- line count
- keyword hit count
- default category
- truncated summary

**Notes**
- line counting ignores trailing newline characters
- keywordHits is currently a placeholder implementation
- category is currently a simple default value

## Running Locally

**1. Start infrastructure**

```bash
docker compose up -d
```

Current Docker services:
- PostgreSQL
- RabbitMQ with management UI

**2. Apply database migrations**

```bash
dotnet ef database update \
  --project src/DocumentProcessing.Infrastructure \
  --startup-project src/DocumentProcessing.Api
```

**3. Run the API**

```bash
dotnet run --project src/DocumentProcessing.Api
```

**4. Run the worker**

```bash
dotnet run --project src/DocumentProcessing.Worker
```

**5. Submit and query jobs**

Use the .http file, curl, Postman, or Swagger/OpenAPI as appropriate.

## RabbitMQ Management UI

RabbitMQ management UI is available locally at:
- `http://localhost:15672`

Use the credentials configured in `docker-compose.yml` / appsettings.

## Testing

Current automated tests include:

### Domain tests

- job creation
- valid transitions
- invalid transitions
- guard clauses
- completion result mapping

### API integration tests

- create job returns accepted response
- get job by id returns persisted job
- get job by id returns 404 when missing
- list jobs returns jobs ordered by submission time

### Worker tests

- document analysis returns expected counts for single-line input
- document analysis returns expected counts for multiline input
- trailing newline does not create an extra counted line
- long input truncates summary correctly

## Continuous Integration

GitHub Actions CI is configured for this repository.

The workflow currently:

- restores dependencies
- builds the solution
- provisions PostgreSQL and RabbitMQ service containers
- applies EF Core migrations
- runs the test suite

This helps validate that the API, worker, and supporting infrastructure work together in an automated environment.

## Important Design Notes

### Why jobs are marked queued before publish

In the current v1 implementation, a job is marked Queued and persisted before the RabbitMQ message is published.

This avoids a race where the worker could consume the message before the queued state is stored in the database.

### Known limitation

This still leaves a consistency gap:
- if database save succeeds
- but message publish fails

then the job may remain Queued without a corresponding broker message.

In a production system, this would typically be addressed with an **outbox pattern**.

## Current Limitations

This version intentionally keeps scope tight:
- text input only
- no real file upload yet
- no retry endpoint yet
- no dead-letter queue / retry policy
- no outbox pattern yet
- no authentication/authorization yet
- no pagination/filtering for job listing yet
- no advanced text analytics yet

## Future Improvements

Possible next steps:
- JWT authentication / authorization
- retry support for failed jobs
- real file upload
- outbox pattern
- dead-letter queue / retry handling
- richer keyword analysis and categorisation
- pagination and filtering for job queries
- additional integration tests around async processing
- containerised API and worker execution

## Why This Project Exists

I’m transitioning into .NET backend development from a senior integration / telecom engineering background. This project is intended to demonstrate backend skills that map to real production systems: asynchronous workflows, messaging, persistence, background processing, lifecycle management, and operational thinking.

## License

MIT

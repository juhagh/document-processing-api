# Document Processing API
[![CI](https://github.com/juhagh/document-processing-api/actions/workflows/ci.yml/badge.svg)](https://github.com/juhagh/document-processing-api/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-3-FF6600?logo=rabbitmq&logoColor=white)](https://www.rabbitmq.com)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)](https://www.docker.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

An event-driven ASP.NET Core backend that accepts document-analysis jobs through a Web API, stores job state in PostgreSQL, and processes jobs asynchronously using RabbitMQ and a background worker.

## Overview

This portfolio project is built to demonstrate employable .NET backend skills beyond basic CRUD APIs, including:

- asynchronous job processing
- worker-based background execution
- message-driven architecture
- explicit job lifecycle management
- outbox pattern
- PostgreSQL persistence with EF Core
- fully containerised application and infrastructure
- layered architecture
- automated testing
- GitHub Actions CI

In the current version, the API accepts **text input** rather than real file uploads. A client submits a document-processing job, the API stores the job and outbox message, and marks the job as queued.  Publisher periodically polls outbox for unpublished messages, processes them in batches and publishes to RabbitMQ. A worker then processes the job asynchronously. The client can then query job status and results later.

## What This Project Demonstrates

This project is designed to show practical backend skills that map to real systems:

- ASP.NET Core Web API design
- EF Core + PostgreSQL persistence
- RabbitMQ message publishing and consumption
- Background worker processing
- explicit domain state transitions
- polling-based async job tracking
- outbox pattern
- clean separation of concerns across layers
- fully containerised local development with Docker Compose
- integration, domain, and worker unit testing
- GitHub Actions CI
- dead-letter queue handling
- consumer-side retry tracking

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

## Related Repositories

- [Document Processing UI](https://github.com/juhagh/document-processing-ui) — React frontend

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
  DocumentProcessing.E2E.Tests
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
  "id": "c6147881-a3a7-41fb-97f7-f96d12e62e58",
  "status": "Queued",
  "inputText": "This is a test document.\nIt has multiple lines.\n",
  "submittedAtUtc": "2026-04-15T05:30:40.209483Z",
  "updatedAtUtc": "2026-04-15T05:30:40.283789Z",
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
  "id": "c6147881-a3a7-41fb-97f7-f96d12e62e58",
  "status": "Completed",
  "inputText": "This is a test document.\nIt has multiple lines.\n",
  "submittedAtUtc": "2026-04-15T05:30:40.209483Z",
  "updatedAtUtc": "2026-04-15T05:30:40.796823Z",
  "completedAtUtc": "2026-04-15T05:30:40.796823Z",
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
2. Application marks the job as `Queued`
3. Application persists the job and the outbox message atomically in a single transaction.
4. Background outbox publisher periodically polls for unpublished outbox messages, publishes them to RabbitMQ, and marks them as published.
5. Worker consumes the message from RabbitMQ.
6. Worker loads the job from PostgreSQL
7. Worker marks the job as `Processing` (Note: In current version, this state change is not visible to client in case of retry scenario)
8. Worker performs simple text analysis
9. Worker marks the job as `Completed` or `Failed`
10. Client retrieves job status using `GET /api/jobs/{id}` or `GET /api/jobs`

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

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Start the full stack

```bash
docker compose up -d
```

This starts all services:
- PostgreSQL
- RabbitMQ
- API (available at `http://localhost:8080`)
- Worker

Database migrations are applied automatically on API startup.

### Submit a job

```bash
curl -X POST http://localhost:8080/api/jobs \
  -H "Content-Type: application/json" \
  -d '{"inputText": "Hello from the fully containerised stack!"}'
```

### Check job status

```bash
curl http://localhost:8080/api/jobs/{id}
```

### RabbitMQ Management UI

Available at `http://localhost:15672` using the credentials configured in `docker-compose.yml`.

### Running locally without Docker (development)

If you prefer to run the API and worker directly for faster iteration:

**1. Start infrastructure only**

```bash
docker compose up -d postgres rabbitmq
```

**2. Apply database migrations**

```bash
dotnet ef database update \
  --project src/DocumentProcessing.Infrastructure \
  --startup-project src/DocumentProcessing.Api
```

**3. Run the API and worker**

```bash
dotnet run --project src/DocumentProcessing.Api
dotnet run --project src/DocumentProcessing.Worker
```

## Testing

### Domain tests
- `DocumentJob` creation with valid input
- valid transitions
- invalid transitions
- guard clauses
- completion result mapping
- `OutboxMessage` creation and validation rules
- outbox publication/error rules

### API integration tests
- create job returns accepted response
- get job by id returns persisted job
- get job by id returns 404 when missing
- list jobs returns jobs ordered by submission time

### Worker unit tests
- document analysis returns expected counts for single-line input
- document analysis returns expected counts for multiline input
- trailing newline does not create an extra counted line
- long input truncates summary correctly

### E2E tests
- full job lifecycle from submission to completion
- full job lifecycle from submission to failure *(skipped — see Known Limitation: Consumer Retry Queue Pattern)*

> E2E tests require the full stack to be running via `docker compose up`.

### Running the test suite

```bash
# Unit and integration tests
dotnet test --filter "Category!=E2E"

# E2E tests (requires docker compose up)
dotnet test --filter "Category=E2E"
```

## Continuous Integration

GitHub Actions CI is configured for this repository and runs on every push.

The workflow:
- restores dependencies
- builds the solution
- provisions PostgreSQL and RabbitMQ service containers
- applies EF Core migrations
- runs unit and integration tests

E2E tests are excluded from CI and are intended to be run against a locally running stack.

## Important Design Notes

### Outbox Pattern

Without the outbox pattern, a failure between saving the job and publishing the message could leave a job stranded in Queued state indefinitely.

With outbox pattern implemented with unit of work, the job is persisted in the database along with a message in the outbox in one atomic transaction.
The Outbox Publisher then periodically polls the outbox for unpublished messages and processes them in batches.

The outbox pattern guarantees at-least-once delivery, however there could be multiple deliveries in case of crash between publishing and marking the message as published.
The background worker guards against multiple delivery by checking message status before processing, however a more robust solution would be to implement idempotency on the consumer side. This is planned as a future improvement. 
A partial index on outbox_messages covering only unpublished messages ensures the publisher query stays fast as the table grows. 

### Dead-Letter Queue

A Dead Letter Exchange (DLX) and Dead Letter Queue (DLQ) are implemented for handling messages that cannot be processed safely.

The main queue, `document-processing.jobs`, is configured with a dead-letter exchange:

- DLX: `document-processing.dlx`
- DLQ: `document-processing.jobs.dlq`
- Dead-letter routing key: `document-processing-key`

When the worker determines that a message should not be retried, it negatively acknowledges the message with `requeue: false`. RabbitMQ then dead-letters the message to `document-processing.dlx`, which routes it to `document-processing.jobs.dlq` using the configured binding.
The DLX, DLQ, and bindings are declared in `rabbitmq/definitions.json`.

#### Messages are dead-lettered for cases such as:

- Invalid or empty `ProcessDocumentJobMessage`
- Malformed `x-death` header
- Non-existent `DocumentJob`
- `DocumentJob` is not in the expected `Queued` state
- Maximum retry count exceeded, once broker-level retry/dead-letter tracking is fully implemented

```text
Producer / API
    │
    │ publish job message
    ▼
document-processing.jobs
    │
    │ consumed by Worker
    ▼
DocumentJobConsumer
    │
    ├── success
    │   └── message is acknowledged and removed from queue
    │       BasicAckAsync
    │
    ├── transient failure
    │   └── message is negatively acknowledged and requeued
    │       BasicNackAsync(requeue: true)
    │
    └── non-retryable failure / retry limit exceeded
        │
        │ message is negatively acknowledged without requeue
        │ BasicNackAsync(requeue: false)
        │
        │ RabbitMQ dead-letters the message using:
        │   exchange:    document-processing.dlx
        │   routing key: document-processing-key
        ▼
document-processing.dlx
    │
    │ direct exchange binding:
    │   routing key: document-processing-key
    ▼
document-processing.jobs.dlq
```

### Known Limitation: Consumer Retry Queue Pattern

Currently, transient processing failures are handled with `BasicNackAsync(requeue: true)`, which returns the message immediately to the main queue.
This is simple, but it has two drawbacks:

- a repeatedly failing message may be retried immediately and consume worker capacity
- because the message is requeued rather than dead-lettered, RabbitMQ’s x-death header is not incremented for those retry attempts.

The worker already contains early support for reading RabbitMQ’s x-death header, but the current retry behaviour does not yet make full use of it because failed messages are requeued directly.

A more robust solution would use a dedicated retry exchange and retry queue with a TTL. Failed messages would be dead-lettered into the retry queue, wait for the TTL to expire, and then be routed back to the main queue. This would provide delayed retries and better broker-level retry tracking.
This is planned as a future improvement.

### Auto-migration on startup

The API automatically applies pending EF Core migrations on startup. This ensures the database schema is always up to date when running via Docker Compose without any manual steps.

## Current Limitations

This version intentionally keeps scope tight:
- text input only
- no real file upload yet
- no retry endpoint yet
- no authentication/authorization yet
- no pagination/filtering for job listing yet
- no advanced text analytics yet

## Future Improvements

Possible next steps:
- JWT authentication / authorization
- retry support for failed jobs
- real file upload
- richer keyword analysis and categorisation
- pagination and filtering for job queries
- React frontend for job submission and status tracking
- dedicated retry exchange and queue with a TTL

## Why This Project Exists

I'm transitioning into .NET backend development from a senior integration / telecom engineering background. This project is intended to demonstrate backend skills that map to real production systems: asynchronous workflows, messaging, persistence, background processing, lifecycle management, and operational thinking.

## License

MIT

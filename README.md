# Document Processing API

An event-driven ASP.NET Core backend that accepts document-analysis jobs through a Web API, stores job state in PostgreSQL, and processes jobs asynchronously using a background worker and RabbitMQ.

## Overview

This project is being built as a portfolio backend project to demonstrate employable .NET backend skills beyond basic CRUD APIs, including:

- asynchronous job processing
- worker-based background execution
- message-driven architecture
- job lifecycle management
- PostgreSQL persistence
- Dockerized local infrastructure
- layered / clean architecture structure
- automated testing

In v1, the API accepts **text input** rather than real file uploads. A client submits a document-processing job, the API stores the job and publishes a message, and a worker processes the job asynchronously. The client can later query job status and results.

## Goals

This project is designed to demonstrate:

- ASP.NET Core Web API design
- Worker Service background processing
- RabbitMQ-based asynchronous messaging
- EF Core with PostgreSQL
- clear separation of concerns across layers
- domain-driven lifecycle/state modeling
- testing of domain rules and API behavior
- Docker Compose local development setup
- GitHub Actions CI

## Planned Tech Stack

- ASP.NET Core Web API
- .NET 10
- Worker Service
- RabbitMQ
- PostgreSQL
- EF Core 10
- Docker / Docker Compose
- xUnit
- GitHub Actions

## Planned Architecture

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
```

### Layer responsibilities

* **DocumentProcessing.Api**
  HTTP endpoints, request/response contracts, and API configuration

* **DocumentProcessing.Application**
  Use cases, orchestration, and application abstractions

* **DocumentProcessing.Domain**
  Core business model, job lifecycle rules, and invariants

* **DocumentProcessing.Infrastructure**
  EF Core persistence, messaging integration, and external service implementations

* **DocumentProcessing.Worker**
  Background message consumption and asynchronous job processing

## Current Scope (v1)

The initial version keeps scope intentionally tight.

### Input

* text content only
* no real file upload yet

### Job lifecycle

* Pending
* Queued
* Processing
* Completed
* Failed

### Planned analysis

* word count
* character count
* line count
* keyword hit count
* simple category
* simple summary

### Planned endpoints

* `POST /api/jobs`
* `GET /api/jobs/{id}`
* `GET /api/jobs`

## Domain Model

The core aggregate is `DocumentJob`.

A job is created in the `Pending` state and moves through an explicit lifecycle:

* `Pending -> Queued`
* `Queued -> Processing`
* `Processing -> Completed`
* `Processing -> Failed`

For v1, `Failed` is treated as a terminal state. Retrying a failed job can be added later as a future enhancement.

## Development Status

### Completed so far

* solution and project structure created
* project references configured
* .NET 10 / EF Core 10 setup
* initial domain model created
* `DocumentJob` lifecycle rules implemented
* domain tests added for:

  * creation
  * valid transitions
  * invalid transitions
  * guard clauses
  * completion result mapping

### Next steps

* add `DbContext`
* configure EF Core mappings
* create first migration
* add PostgreSQL connection
* implement initial API endpoints
* add RabbitMQ publisher
* add worker consumer
* add Docker Compose
* add CI workflow

## Running the project

Instructions will be added as the API, database, and messaging setup are completed.

## Future Enhancements

Possible later improvements:

* JWT authentication / authorization
* retry support for failed jobs
* real file upload
* dead-letter queue / retry policies
* outbox pattern
* richer document analysis
* pagination / filtering for job queries
* observability improvements
* deployment pipeline

## Why this project exists

I’m transitioning into .NET backend development from a senior integration / telecom engineering background. This project is intended to demonstrate backend skills that map to real production systems: asynchronous workflows, decoupled processing, messaging, persistence, and lifecycle management.

## License

This project is for portfolio and learning purposes.

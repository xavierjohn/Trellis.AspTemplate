# Trellis ASP.NET Service Template

A `dotnet new` template for creating ASP.NET services using the [Trellis](https://github.com/xavierjohn/Trellis) framework with Railway-Oriented Programming (ROP) and Domain-Driven Design (DDD).

## Getting Started

### Install the template

```powershell
dotnet new install Trellis.AspTemplate
```

### Create a new service

```powershell
dotnet new trellis-asp -n MyService --authorName "Your Name"
```

This creates a `MyService/` directory with the full solution structure ready to build and run.

### Build and test

```powershell
cd MyService
dotnet build
dotnet test
```

### Run the API

```powershell
dotnet run --project Api/src
```

## What's Included

### Architecture

The template provides a layered architecture following DDD and CQRS principles:

| Layer | Project | Purpose |
|-------|---------|---------|
| **Domain** | `Domain/` | Aggregates, entities, value objects — pure business logic with zero external dependencies |
| **Application** | `Application/` | Commands, queries, handlers — orchestrates domain logic via CQRS (Mediator) |
| **Anti-Corruption Layer** | `Acl/` | Repository implementations, external service adapters, EF Core — shields the domain from infrastructure |
| **API** | `Api/` | Controllers, DTOs, middleware, composition root — thin HTTP layer |

Each layer has `src/` and `tests/` projects.

### Key Features

- **Railway-Oriented Programming** — Errors are values (`Result<T>`), not exceptions. Business logic flows through composable pipelines using `Bind`, `Map`, `Ensure`, and `Tap`.
- **Domain-Driven Design** — Value objects with `TryCreate` validation, aggregates with domain events, specifications for composable queries.
- **CQRS** — Commands and queries separated via [Mediator](https://github.com/martinothamar/Mediator) source generator.
- **Service Level Indicators** — All API methods emit duration and status code metrics via [ServiceLevelIndicators](https://github.com/xavierjohn/ServiceLevelIndicators).
- **OpenTelemetry** — Traces and metrics configured out of the box. Use the included [Aspire Dashboard](DockerOpenTelemetry/README.md) for local observability.
- **API Versioning** — Date-based versioning with `Asp.Versioning`.
- **Copilot Instructions** — `template/.github/copilot-instructions.md` and `template/.github/trellis-api-reference.md` ship with the template to guide AI assistants toward Trellis patterns.

### Template Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `-n` / `--name` | Service name (used for solution file, namespaces, assembly names) | `MyService` |
| `--authorName` | Author name in `Directory.Build.props` | `Your Name` |

## Convention over Configuration for Resource Names

When deploying across multiple regions and environments, set `RegionShortName` and `Environment` to derive resource names automatically via `EnvironmentOptions`:

```json
{
  "EnvironmentOptions": {
    "Environment": "test",
    "Region": "westus2",
    "RegionShortName": "usw2"
  }
}
```

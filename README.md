# Trellis ASP.NET Service Template

Create a production-ready ASP.NET service with Trellis, layered architecture, testing, and observability already wired in.

## Quick Start

```powershell
dotnet new install Trellis.AspTemplate
dotnet new trellis-asp -n MyService --authorName "Your Name"
cd MyService; dotnet run --project Api\src
```

The template restores packages after creation. Once the app is running, use the API endpoint or OpenAPI UI from the `Api` project output.

## What's Included

Trellis organizes your service into clear layers so business logic stays isolated from HTTP and infrastructure concerns.

```text
Api -> Application -> Domain
  \-> Acl -----------^
```

| Folder | Purpose |
|--------|---------|
| `Api\src` | ASP.NET entry point, middleware, configuration, HTTP endpoints |
| `Api\tests` | API and middleware tests |
| `Application\src` | Commands, queries, handlers, and application orchestration |
| `Application\tests` | Application-layer tests |
| `Domain\src` | Aggregates, value objects, specifications, and core business rules |
| `Domain\tests` | Domain tests |
| `Acl\src` | EF Core, repositories, environment/resource naming, external adapters |
| `Acl\tests` | Infrastructure tests |
| `.devcontainer` | Codespaces / Dev Container setup for a ready-to-code environment |
| `DockerOpenTelemetry` | Local observability helper for viewing traces, metrics, and logs |
| `build` | Build and automation scripts |

## Features

- Trellis-based service structure for clean, layered ASP.NET applications
- Railway-Oriented Programming patterns for explicit success and failure flows
- Domain-Driven Design friendly layout with domain, application, API, and ACL projects
- CQRS-style application layer with handlers and request separation
- Test projects for every layer
- EF Core-ready infrastructure setup
- OpenTelemetry support for local observability
- API starter files, HTTP samples, and local app settings
- Dev Container support for Codespaces and VS Code
- AI guidance files included in `.github` for Trellis-friendly code generation

## Template Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `-n`, `--name` | Service name. Also drives solution, directory, and namespace naming. | `MyService` |
| `--authorName` | Author value written into `Directory.Build.props`. | `Your Name` |

## Documentation

- Trellis docs: https://xavierjohn.github.io/Trellis/
- Dev Container guide: https://github.com/xavierjohn/Trellis/blob/main/TrellisAspTemplate/template/.devcontainer/README.md
- OpenTelemetry guide: https://github.com/xavierjohn/Trellis/blob/main/TrellisAspTemplate/template/DockerOpenTelemetry/README.md

## Requirements

- .NET 10 SDK

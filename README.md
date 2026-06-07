# Trellis.AspTemplate

[![Build](https://github.com/xavierjohn/Trellis.AspTemplate/actions/workflows/build.yml/badge.svg)](https://github.com/xavierjohn/Trellis.AspTemplate/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Trellis.AspTemplate.svg)](https://www.nuget.org/packages/Trellis.AspTemplate)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Trellis.AspTemplate.svg)](https://www.nuget.org/packages/Trellis.AspTemplate)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/download)
[![C#](https://img.shields.io/badge/C%23-14.0-blue.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![GitHub Stars](https://img.shields.io/github/stars/xavierjohn/Trellis.AspTemplate?style=social)](https://github.com/xavierjohn/Trellis.AspTemplate/stargazers)

> `dotnet new trellis-asp` template scaffolding a production-ready single-service ASP.NET application on the [Trellis framework](https://github.com/xavierjohn/Trellis) with Clean Architecture layout (API + Application + Domain + ACL), API versioning, EF Core, OpenAPI, and test infrastructure already wired in.

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

- Trellis docs: <https://xavierjohn.github.io/Trellis/>
- Dev Container guide: [`template/.devcontainer/README.md`](https://github.com/xavierjohn/Trellis.AspTemplate/blob/main/template/.devcontainer/README.md)
- OpenTelemetry guide: [`template/DockerOpenTelemetry/README.md`](https://github.com/xavierjohn/Trellis.AspTemplate/blob/main/template/DockerOpenTelemetry/README.md)

## Related repositories

- [`xavierjohn/Trellis`](https://github.com/xavierjohn/Trellis) — the framework: `Result<T>`, `Maybe<T>`, value objects, DDD primitives, ASP.NET / EF Core / Mediator integration.
- [`xavierjohn/Trellis.Microservices`](https://github.com/xavierjohn/Trellis.Microservices) — microservice trust-boundary packages: YARP gateway integration + consumer-side actor provider for multi-tenant ABAC.
- [`xavierjohn/Trellis.Microservices.Template`](https://github.com/xavierjohn/Trellis.Microservices.Template) — `dotnet new trellis-microservices` Project Tracker starter (multi-service topology with Aspire).
- [`xavierjohn/Trellis.ServiceLevelIndicators`](https://github.com/xavierjohn/Trellis.ServiceLevelIndicators) — latency SLI metrics library for emitting operation-duration histograms via System.Diagnostics.Metrics + OpenTelemetry.

## Requirements

- .NET 10 SDK

## License

[MIT](LICENSE).
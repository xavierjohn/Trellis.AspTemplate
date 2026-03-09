# Copilot Instructions — Building with Trellis

This project uses the **Trellis** framework (.NET 10). Trellis combines Railway-Oriented Programming (ROP) with Domain-Driven Design (DDD). Follow these patterns exactly.

**API Reference:** See `.github/trellis-api-reference.md` for all Trellis types, method signatures, and usage patterns. Use it as the authoritative source for Trellis API surface.

## Core Principles

1. **Errors are values, not exceptions.** Use `Result<T>` for expected failures. Never throw for business logic. Never use try/catch in Domain or Application layers.
2. **Make illegal states unrepresentable.** Every domain concept is a value object with `TryCreate`. If it exists, it's valid.
3. **No primitive obsession.** No raw `Guid`, `string`, `int`, or `decimal` in domain properties or method signatures. Every property on an Aggregate or Entity must be a typed value object. If the same concept appears in two contexts (e.g., line item quantity vs. stock quantity), create separate types for each.
4. **Use built-in `Trellis.Primitives` before creating custom value objects.** See the concrete value objects in the API reference (§3). Only create custom value objects for domain concepts not covered by these. Use `[StringLength]` on `RequiredString<T>` subclasses to add length validation without writing custom `TryCreate`.
5. **Optional values use `Maybe<T>`, never null.** `Maybe<PhoneNumber>`, not `PhoneNumber?`. Declare `Maybe<T>` properties as `partial` — the source generator handles the backing field.

## Architecture

```
Api → Application → Domain
Api → Acl → Application → Domain
```

| Layer | Depends On | Contains |
|-------|-----------|----------|
| **Domain** | Trellis packages only (Results, Primitives, DDD, Stateless, Authorization) | Aggregates, entities, value objects, domain events, specifications, permission constants |
| **Application** | Domain, Mediator, Trellis.Mediator | Commands, queries, handlers, repository interfaces |
| **Acl** | Application, Trellis.EntityFrameworkCore, EF Core provider | DbContext, entity configurations, repository implementations, migrations |
| **Api** | Application, Acl, Trellis.Asp | Endpoints, DTOs, Program.cs (composition root), IActorProvider implementation |

> **Why "Acl"?** ACL stands for Anti-Corruption Layer. This avoids confusion with actual infrastructure (servers, databases, cloud services). The Acl layer adapts external systems (SQL Server, message queues, etc.) to the domain model through repository implementations and EF Core.

**Rules:**
- Domain has ZERO external dependencies (no EF Core, no ASP.NET, no Mediator).
- Repository interfaces live in Application, implementations in Acl.
- `Mediator.SourceGenerator` is installed in the **Application** project (where commands and queries are defined).
- Each layer has one `DependencyInjection.cs` with an `Add{Layer}()` extension method.
- Register `IActorProvider` as **singleton** in the Api layer. This is safe because `IHttpContextAccessor.HttpContext` uses `AsyncLocal` internally. Trellis pipeline behaviors are registered as singletons, so a scoped `IActorProvider` will cause a runtime exception.

## Project Layout

The template provides the complete project structure. Do NOT modify or recreate build system files (`Directory.Build.props`, `Directory.Packages.props`, `global.json`, `build/test.props`). They are pre-configured.

```
{ServiceName}/
├── {ServiceName}.slnx
├── Directory.Build.props          ← DO NOT MODIFY
├── Directory.Packages.props       ← ADD new packages here (versions only)
├── global.json                    ← DO NOT MODIFY
├── build/
│   └── test.props                 ← DO NOT MODIFY
├── .github/
│   ├── copilot-instructions.md    ← this file
│   └── trellis-api-reference.md   ← Trellis API surface
├── Domain/
│   ├── src/
│   │   └── Domain.csproj
│   └── tests/
│       └── Domain.Tests.csproj
├── Application/
│   ├── src/
│   │   └── Application.csproj
│   └── tests/
│       └── Application.Tests.csproj
├── Acl/
│   ├── src/
│   │   └── AntiCorruptionLayer.csproj
│   └── tests/
│       └── AntiCorruptionLayer.Tests.csproj
└── Api/
    ├── src/
    │   └── Api.csproj
    └── tests/
        └── Api.Tests.csproj
```

**Adding NuGet packages:** Add `<PackageVersion>` entries to `Directory.Packages.props`, then `<PackageReference>` (without version) in the relevant `.csproj`. Never specify versions in `.csproj` files.

**HTTP file:** The template includes `Api/src/api.http` with sample requests. After implementing the spec, **replace its contents** with requests covering every endpoint in the API — happy-path examples, error cases, and the full resource lifecycle. Use `@variables` for host, api-version, and response-chained IDs (e.g., `{{createCustomer.response.body.id}}`). This file is the living documentation for manual testing and onboarding.

**Environment file:** Complex JSON variables (actors, auth tokens, reusable objects) do NOT work inline in `.http` files. Put them in `Api/src/http-client.env.json` instead:
```json
{
  "dev": {
    "host": "https://localhost:5001",
    "apiVersion": "2026-11-12",
    "adminActor": "{\"Id\":\"admin-1\",\"Permissions\":[\"customers:create\",\"products:create\"]}",
    "userActor": "{\"Id\":\"user-1\",\"Permissions\":[\"orders:create\",\"orders:read\"]}"
  }
}
```
The `.http` file then references them as `{{adminActor}}`, `{{host}}`, etc. Only simple scalar `@variables` (strings, numbers, response-chained IDs) belong in the `.http` file itself.

## Key Conventions

### Commands and Queries

- Commands receive **value object types** (e.g., `CustomerId`, not `Guid`). Scalar value binding validates at the API layer — handlers never call `TryCreate` on command properties.
- Use `IValidate` **only** for cross-field or collection validation (e.g., "at least one line item"). Single-field validation is handled by value objects.
- Use `IAuthorize` for permission-based authorization. Use `IAuthorizeResource<TResource>` for resource-based authorization (e.g., "only the owner can cancel"). The handler receives the entity already authorized — no auth logic in handlers. Register resource loaders as scoped in the Acl layer. See API reference §4 and §8 for interfaces and pipeline.
- **Resource authorization registration:** Use `services.AddResourceAuthorization(assembly)` in the Acl layer's `DependencyInjection.cs`.
- **`Unit` type disambiguation:** Both `Trellis` and `Mediator` define a `Unit` type. Always use `Trellis.Unit`. See "Known Issues & Workarounds" in the API reference.

### Handler ROP Pattern

**Use `Bind`/`BindAsync` chains in handlers — not imperative `if`/`return`.** Handlers should compose Result operations using the ROP pipeline. See "Usage Patterns & Recipes" in the API reference for examples.

**Task vs ValueTask overload ambiguity (CS0121):** Trellis provides `TapAsync`, `BindAsync`, `MapAsync` overloads for both `Task` and `ValueTask`. When using async lambdas in ROP chains, the compiler cannot resolve between them. Fix by casting the lambda to an explicit `Func<>` with `Task`:

```csharp
// CS0121 — compiler can't choose between Task and ValueTask overloads
.BindAsync(async order => await _repo.SaveAsync(order, ct))  // ❌ ambiguous

// Fix — cast to explicit Func with Task return type
.BindAsync((Func<Order, Task<Result<Order>>>)(async order =>
{
    var saveResult = await _repo.SaveAsync(order, ct);
    return saveResult.Map(_ => order);
}))
```

### Parallel Async Operations

When a handler needs multiple independent async results, use `Result.ParallelAsync` + `.WhenAllAsync()` instead of sequential `await`. See `ParallelAsync` and `WhenAllAsync` in the API reference (§1).

### State Machines (Trellis.Stateless)

Use `Trellis.Stateless` for aggregate state transitions. See API reference §11 for `FireResult()`.

**Lazy initialization required for EF Core.** The third-party `StateMachine<TState, TTrigger>` constructor eagerly invokes its `stateAccessor` function. When EF Core materializes an aggregate via its parameterless constructor, state properties are not yet populated — causing a `NullReferenceException`. Use lazy initialization:

```csharp
public class Order : Aggregate<OrderId>
{
    public OrderStatus Status { get; private set; }

    // ✅ Lazy — defers construction until first use (after EF Core populates properties)
    private StateMachine<string, string>? _machine;
    private StateMachine<string, string> Machine => _machine ??= ConfigureStateMachine();

    private StateMachine<string, string> ConfigureStateMachine()
    {
        var machine = new StateMachine<string, string>(() => Status.Name, s => Status = OrderStatus.FromName(s));
        machine.Configure("Draft").Permit("Submit", "Submitted");
        // ... more transitions
        return machine;
    }

    public Result<Order> Submit() =>
        Machine.FireResult("Submit")
            .Tap(_ => DomainEvents.Add(new OrderSubmittedEvent(Id)))
            .Map(_ => this);

    // ❌ Wrong — eager construction crashes when EF Core calls parameterless constructor
    // private readonly StateMachine<string, string> _machine = new(...);
}
```

### EF Core

- **NEVER write `HasConversion()`.** Call `ApplyTrellisConventions` in `ConfigureConventions` — it handles all scalar Trellis value objects and `Money` properties automatically.
- **Custom composite `ValueObject` types** (e.g., `ShippingAddress` with multiple fields) are NOT auto-mapped. Map them with `OwnsOne` in the entity configuration and configure each property explicitly.
- **`Maybe<T>` properties** — use C# 13 `partial` properties. The source generator emits the backing field and getter/setter automatically. `ApplyTrellisConventions` auto-maps them as nullable columns:

```csharp
// ✅ Correct — partial property (source generator handles the rest)
public partial class Order : Aggregate<OrderId>
{
    public partial Maybe<DateTime> SubmittedAt { get; set; }
    public partial Maybe<DateTime> ShippedAt { get; set; }
}
// No EF Core configuration needed for Maybe<T> properties
```

- **Entity configurations:** Use `IEntityTypeConfiguration<T>` per entity in the Acl layer — one file per aggregate/entity. Register them with `ApplyConfigurationsFromAssembly` in `OnModelCreating`. Do NOT inline configuration in `DbContext.OnModelCreating`.
- **Migrations:** After implementing all entities and configurations, run `dotnet ef migrations add InitialCreate -p Acl/src -s Api/src` to generate the initial migration. Do not rely on `EnsureCreated()` for anything beyond a quick prototype.
- See API reference §12 for all EF Core extensions (`SaveChangesResultUnitAsync`, queryable extensions, `Maybe<T>` LINQ queries, exception classification, etc.).

### MVC Controllers

Controllers inherit `ControllerBase` with `[ApiController]`. Actions are thin — send command via Mediator, chain the result to an action result. See API reference §5 for `ToActionResult`, `ToCreatedAtActionResult`, and async variants.

**Every controller must have:**
- `[ApiController]` attribute and inherit `ControllerBase`
- `[ServiceLevelIndicator]` at class level
- `[Route("api/[controller]")]` at class level
- `[Consumes("application/json")]` and `[Produces("application/json")]` at class level
- Error responses as RFC 9457 Problem Details (handled automatically)

**Do NOT add `[ApiVersion]` attributes.** Version is derived automatically from the controller namespace via `VersionByNamespaceConvention` (see API Versioning below).

### Automatic Scalar Value Binding

**Use value object types — not primitives — in controller action parameters.** Trellis automatically converts route parameters, query parameters, and JSON body properties via model binding and JSON converters. Never call `.Create()` or `.TryCreate()` manually in controllers. See API reference §5 for registration (`AddScalarValueValidation`, `UseScalarValueValidation`).

**Request/Response DTOs** live in `Api/src/Contracts/`. Never expose domain types directly. Request DTOs can use scalar value object types as properties — they will be validated automatically via the JSON converter.

### API Versioning

Versioning is **namespace-driven** — no `[ApiVersion]` attribute needed. Register the convention in `Api/src/DependencyInjection.cs`:
```csharp
services.AddApiVersioning()
        .AddMvc(options => options.Conventions.Add(new VersionByNamespaceConvention()))
        .AddApiExplorer()
        .AddOpenApi(options => options.Document.AddScalarTransformers());
```

**Folder & namespace convention:** Place controllers in `Api/src/{date}/Controllers/` with a matching namespace. The date in the namespace (with underscores) maps to the API version (with hyphens):
- Folder: `Api/src/2026-11-12/Controllers/`
- Namespace: `{ServiceName}.Api.v2026_11_12.Controllers`
- Resolved version: `2026-11-12`

### OpenAPI & Scalar

The template uses **Scalar** (not Swagger/Swashbuckle) for interactive API documentation, backed by the built-in ASP.NET Core OpenAPI support.

**Packages** — `Api.csproj` must reference:
```xml
<PackageReference Include="Scalar.AspNetCore" />
<PackageReference Include="Scalar.AspNetCore.Microsoft" />
```

**Program.cs** — map OpenAPI and Scalar endpoints (development only):
```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().WithDocumentPerVersion();
    app.MapScalarApiReference(
        options =>
        {
            var descriptions = app.DescribeApiVersions();

            for (var i = 0; i < descriptions.Count; i++)
            {
                var description = descriptions[i];
                var isDefault = i == descriptions.Count - 1;
                options.AddDocument(description.GroupName, description.GroupName, isDefault: isDefault);
            }
        });
}
```

The Scalar UI is available at `/scalar/{version}` (e.g., `/scalar/2026-11-12`).

## Testing Strategy

**Domain tests:** Pure unit tests, no external dependencies. Test value object TryCreate, aggregate rules, state machine transitions, specifications.

**Application tests:** Mock repository interfaces. Test handler logic, authorization checks, error mapping. Use `Xunit.DependencyInjection` for test DI with a `Startup.cs` that registers Mediator and mock services.

**API integration tests:** Use `WebApplicationFactory<Program>` with SQLite in-memory. Test HTTP round-trips, status codes, Problem Details, authorization enforcement. Use `MartinCostello.Logging.XUnit.v3` for test logging.

**Do NOT** create `GlobalUsings.cs` files in test projects. Global usings come from `build/test.props`.

## Trellis Feedback

While building with Trellis, **actively track friction points, workarounds, and missing capabilities.** At the end of the project (or at any significant milestone), generate a `TRELLIS_FEEDBACK.md` file in the repository root.

This feedback helps the Trellis team identify gaps in the framework and prioritize future improvements. **Generate this file proactively** — do not wait to be asked.

### When to Record Feedback

- You had to write boilerplate that Trellis should have handled
- You worked around a missing pattern or building block
- A Trellis API was confusing or required reading source code to understand
- You wished a base class, interface, or extension method existed but it didn't
- The copilot instructions were ambiguous or missing guidance for a scenario you encountered
- An error message from Trellis was unhelpful or misleading
- You had to make an architectural decision that Trellis should have constrained
- A common .NET pattern (middleware, DI, configuration) wasn't covered by Trellis conventions

### Feedback File Format

Generate `TRELLIS_FEEDBACK.md` with this structure:

```markdown
# Trellis Feedback — {ServiceName}

> Generated by AI while building {ServiceName} on {date}.
> Trellis version: {version from Directory.Packages.props}
> AI model: {model name}

## Summary

{1-2 sentence overall assessment of the development experience with Trellis}

## Friction Points

### FP-1: {Short title}
- **Category:** Missing Building Block | Workaround Required | Ambiguous API | Missing Documentation | Error Message | Architectural Gap
- **Severity:** High (blocked progress) | Medium (slowed progress) | Low (minor inconvenience)
- **Context:** {What were you trying to do?}
- **What happened:** {What went wrong or was harder than expected?}
- **Workaround used:** {What you did instead, if anything}
- **Suggested improvement:** {What Trellis could add or change}

### FP-2: ...

## What Worked Well

{List of Trellis features that were particularly effective or easy to use. This helps the team know what NOT to change.}

## Suggested New Features

### SF-1: {Feature name}
- **Use case:** {When would this be useful?}
- **Proposed API:** {Sketch of what the API could look like}

### SF-2: ...

## Copilot Instructions Feedback

{Any sections of the copilot instructions that were unclear, missing, or led to incorrect code generation. Be specific about which section and what was confusing.}
```

### Rules

- **Be specific.** Include the exact code you wrote as a workaround. Vague feedback like "EF Core was hard" is not actionable.
- **One friction point per entry.** Don't combine unrelated issues.
- **Include severity.** This helps the Trellis team prioritize.
- **Credit what works.** The "What Worked Well" section is equally important — it prevents regressions.
- **If nothing went wrong, say so.** A feedback file with zero friction points and a strong "What Worked Well" section is valuable data.

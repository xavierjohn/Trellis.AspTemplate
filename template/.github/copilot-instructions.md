# Copilot Instructions — Building with Trellis

This project uses the **Trellis** framework (.NET 10). Trellis combines Railway-Oriented Programming (ROP) with Domain-Driven Design (DDD). Follow these patterns exactly.

**API Reference:** See `.github/trellis-api-reference.md` for all Trellis types, method signatures, and usage patterns. Use it as the authoritative source for Trellis API surface.

## Core Principles

1. **Errors are values, not exceptions.** Use `Result<T>` for expected failures. Never throw for business logic. Never use try/catch in Domain or Application layers.
2. **Make illegal states unrepresentable.** Every domain concept is a value object with `TryCreate`. If it exists, it's valid.
3. **No primitive obsession.** No raw `Guid`, `string`, `int`, or `decimal` in domain properties or method signatures. Every property on an Aggregate or Entity must be a typed value object. If the same concept appears in two contexts (e.g., line item quantity vs. stock quantity), create separate types for each.
4. **Use built-in `Trellis.Primitives` before creating custom value objects.** `EmailAddress`, `PhoneNumber`, `Url`, `Hostname`, `IpAddress`, `Slug`, `CountryCode`, `CurrencyCode`, `LanguageCode`, `Age`, `Percentage`, and `Money` are already provided with full validation, JSON converters, and EF Core support. Only create custom value objects for domain concepts not covered by these. Use `[StringLength]` on `RequiredString<T>` subclasses to add length validation without writing custom `TryCreate`.
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
- Use `IAuthorize` for permission-based authorization. Use `IAuthorizeResource<TResource>` for resource-based authorization (e.g., "only the owner can cancel").
- **`IAuthorizeResource<TResource>`:** The pipeline loads the resource via an `IResourceLoader<TMessage, TResource>` before calling `Authorize(Actor, TResource)`. The handler receives the entity already authorized — no auth logic in handlers. Register the resource loader as scoped in the Acl layer. Use `ResourceLoaderById<TMessage, TResource, TId>` as a convenience base class for ID-based lookups.
- **Registration:** Use `services.AddResourceAuthorization(assembly)` in the Acl layer's `DependencyInjection.cs` to scan-register all `IAuthorizeResource<T>` commands and their `IResourceLoader` implementations. Alternatively, register explicitly with `services.AddResourceAuthorization<TMessage, TResource, TResponse>()`.
- **`Unit` type disambiguation:** Both `Trellis` and `Mediator` define a `Unit` type. In handler return types and ROP chains, always use `Trellis.Unit` (or `default(Trellis.Unit)`). The global `using Trellis;` directive makes the unqualified `Unit` resolve to `Trellis.Unit`, but when both namespaces are imported, qualify explicitly.

### Handler ROP Pattern

**Use `Bind`/`BindAsync` chains in handlers — not imperative `if`/`return`.** Handlers should compose Result operations using the ROP pipeline, not unwrap results manually.

**Task vs ValueTask overload ambiguity (CS0121):** Trellis provides `TapAsync`, `BindAsync`, `MapAsync` overloads for both `Task` and `ValueTask`. When using async lambdas in ROP chains, the compiler cannot resolve between them. Fix by casting the lambda to an explicit `Func<>` with `Task`:

```csharp
// CS0121 — compiler can't choose between Task and ValueTask overloads
.BindAsync(async order => await _repo.SaveAsync(order, cancellationToken))  // ❌ ambiguous

// Fix — cast to explicit Func with Task return type
.BindAsync((Func<Order, Task<Result<Order>>>)(async order =>
{
    var saveResult = await _repo.SaveAsync(order, cancellationToken);
    return saveResult.Map(_ => order);
}))
```

```csharp
// ✅ Correct — ROP chain with Bind/BindAsync
public async ValueTask<Result<OrderDto>> Handle(SubmitOrderCommand command, CancellationToken cancellationToken) =>
    await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken)
        .BindAsync(order => order.Submit())
        .TapAsync(order => _orderRepository.SaveAsync(order, cancellationToken))
        .MapAsync(OrderDto.From);

// ❌ Wrong — imperative unwrapping
public async ValueTask<Result<OrderDto>> Handle(SubmitOrderCommand command, CancellationToken cancellationToken)
{
    var orderResult = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
    if (!orderResult.TryGetValue(out var order))
    {
        _ = orderResult.TryGetError(out var error);
        return error;
    }
    var submitResult = order.Submit();
    if (!submitResult.TryGetValue(out var submitted))
    {
        _ = submitResult.TryGetError(out var error);
        return error;
    }
    await _orderRepository.SaveAsync(submitted, cancellationToken);
    return OrderDto.From(submitted);
}
```

### Parallel Async Operations

When a handler needs multiple independent async results (e.g., fetching a customer AND products), use `Result.ParallelAsync` + `.WhenAllAsync()` instead of sequential `await`:

```csharp
// ✅ Correct — parallel fetches with ParallelAsync
public async ValueTask<Result<Order>> Handle(CreateDraftOrderCommand command, CancellationToken cancellationToken)
{
    var productIds = command.LineItems.Select(li => li.ProductId).ToList();

    return await Result.ParallelAsync(
        () => _customerRepository.GetByIdAsync(command.CustomerId, cancellationToken),
        () => _productRepository.GetByIdsAsync(productIds, cancellationToken))
        .WhenAllAsync()
        .BindAsync((Customer customer, List<Product> products) =>
            Order.TryCreate(customer, products, command.LineItems));
}

// ❌ Wrong — sequential fetches
var customer = await _customerRepository.GetByIdAsync(command.CustomerId, cancellationToken);
var products = await _productRepository.GetByIdsAsync(command.ProductIds, cancellationToken);
```

### State Machines (Trellis.Stateless)

Use `Trellis.Stateless` for aggregate state transitions. The `FireResult()` extension returns `Result<TState>` instead of throwing on invalid transitions.

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

- **NEVER write `HasConversion()`.** Call `ApplyTrellisConventions` in `ConfigureConventions` — it handles all scalar Trellis value objects automatically.
- **`Money` properties** are auto-mapped by `ApplyTrellisConventions` — no `OwnsOne` needed. See §12 in `trellis-api-reference.md` for column naming.
- **Custom composite `ValueObject` types** (e.g., `ShippingAddress` with multiple fields) are NOT auto-mapped. Map them with `OwnsOne` in the entity configuration and configure each property explicitly.
- Use `SaveChangesResultUnitAsync` in repositories (returns `Result<Unit>`). Never use bare `SaveChangesAsync`.
- Use `FirstOrDefaultMaybeAsync` for optional lookups, `FirstOrDefaultResultAsync` for required lookups.
- Use `.Where(specification)` for specification queries. Specifications support `.And()`, `.Or()`, `.Not()` composition.
- **`Maybe<T>` properties** — declare as `partial`. The source generator and `MaybeConvention` handle everything automatically — no manual backing fields or EF configuration needed. See §12 in `trellis-api-reference.md`. **After adding `partial Maybe<T>` properties, run `dotnet build` before writing code that references the backing field (e.g., entity configurations, LINQ queries).** The source generator must run first to emit the `_camelCase` backing field; until it does, the field does not exist and the compiler will report errors.
- **`Maybe<T>` in indexes** — `HasIndex` with `Maybe<T>` properties requires string-based backing field references because `MaybeConvention` ignores the CLR property. Use the backing field name (underscore + camelCase): `builder.HasIndex("Status", "_submittedAt")`. Do NOT use lambda expressions like `o => new { o.Status, o.SubmittedAt }` — they will silently fail.
- **`Maybe<T>` LINQ queries** — use `WhereNone`, `WhereHasValue`, `WhereEquals` extension methods. See §12 in `trellis-api-reference.md`.
- **Entity configurations:** Use `IEntityTypeConfiguration<T>` per entity in the Acl layer — one file per aggregate/entity (e.g., `OrderConfiguration.cs`, `CustomerConfiguration.cs`). Register them with `ApplyConfigurationsFromAssembly` in `OnModelCreating`. Do NOT inline configuration in `DbContext.OnModelCreating`.
- **Migrations:** After implementing all entities and configurations, run `dotnet ef migrations add InitialCreate -p Acl/src -s Api/src` to generate the initial migration. Do not rely on `EnsureCreated()` for anything beyond a quick prototype.

### MVC Controllers

Controllers inherit `ControllerBase` with `[ApiController]`. Actions are thin — send command via Mediator, chain `.ToActionResult(this)` or `.ToActionResultAsync(this)`.

**Every controller must have:**
- `[ApiController]` attribute and inherit `ControllerBase`
- `[Route("api/[controller]")]` at class level
- `[Consumes("application/json")]` and `[Produces("application/json")]` at class level
- Error responses as RFC 9457 Problem Details (handled by `ToActionResult`)

**Do NOT add `[ApiVersion]` attributes.** Version is derived automatically from the controller namespace via `VersionByNamespaceConvention` (see API Versioning below).

**Use `ToCreatedAtActionResult`** for POST endpoints that create resources — returns `201 Created` with `Location` header.

### Automatic Scalar Value Binding

**Use value object types — not primitives — in controller action parameters.** Trellis automatically converts route parameters, query parameters, and JSON body properties via model binding and JSON converters. Never call `.Create()` or `.TryCreate()` manually in controllers.

**Registration** — add scalar value validation to the MVC pipeline in `Api/src/DependencyInjection.cs`:
```csharp
services.AddControllers().AddScalarValueValidation();
```
And activate the middleware in `Program.cs`:
```csharp
app.UseScalarValueValidation();
```

**Request/Response DTOs** live in `Api/src/{version}/Models/` (e.g., `Api/src/2026-11-12/Models/`). Never expose domain types directly. Request DTOs can use scalar value object types as properties — they will be validated automatically via the JSON converter.

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

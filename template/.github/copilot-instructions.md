# Copilot Instructions — Building with Trellis

This project uses the **Trellis** framework (.NET 10). Trellis combines Railway-Oriented Programming (ROP) with Domain-Driven Design (DDD). Follow these patterns exactly.

**API Reference:** See `.github/trellis-api-reference.md` for all Trellis types, method signatures, and usage patterns. Use it as the authoritative source for Trellis API surface. See `.github/trellis-api-testing-reference.md` for testing APIs (FluentAssertions extensions, FakeRepository, TestActorProvider, test patterns).

## Core Principles

1. 🔴 **Errors are values, not exceptions.** Use `Result<T>` for expected failures. Never throw for business logic. Never use try/catch in Domain or Application layers.
2. 🔴 **Make illegal states unrepresentable.** Every domain concept is a value object with `TryCreate`. If it exists, it's valid.
3. 🔴 **No primitive obsession on domain surfaces.** No raw `Guid`, `string`, `int`, or `decimal` in aggregate/entity properties or public domain method signatures. Every property on an Aggregate or Entity must be a typed value object. If the same concept appears in two contexts (e.g., line item quantity vs. stock quantity), create separate types for each. 🟢 Private helpers and internal implementation details may use primitives when the value object adds no safety benefit.
4. 🟡 **Use built-in `Trellis.Primitives` before creating custom value objects.** `EmailAddress`, `PhoneNumber`, `Url`, `Hostname`, `IpAddress`, `Slug`, `CountryCode`, `CurrencyCode`, `LanguageCode`, `Age`, `Percentage`, and `Money` are already provided with full validation, JSON converters, and EF Core support. Only create custom value objects for domain concepts not covered by these. Use `[StringLength]` on `RequiredString<T>` subclasses to add length validation, and `[Range]` on `RequiredInt<T>` subclasses to add min/max validation, without writing custom `TryCreate`.
5. 🔴 **Optional values use `Maybe<T>`, never null.** `Maybe<PhoneNumber>`, not `PhoneNumber?`. When using EF Core, declare `Maybe<T>` properties as `partial` so the source generator can emit the backing field for persistence.

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
│   ├── trellis-api-reference.md   ← Trellis API surface
│   └── trellis-api-testing-reference.md ← Trellis.Testing API surface
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

**Environment file:** Actor headers and other complex values must be stored as **escaped JSON strings** in `Api/src/http-client.env.json`. The `.http` file only supports scalar variable substitution — complex objects with nested properties do NOT work.
```json
{
  "dev": {
    "host": "https://localhost:7011",
    "apiVersion": "2026-11-12",
    "adminActor": "{\"Id\":\"admin-1\",\"Permissions\":[\"customers:create\",\"products:create\"]}",
    "userActor": "{\"Id\":\"user-1\",\"Permissions\":[\"orders:create\",\"orders:read\"]}"
  }
}
```
The `.http` file then references them as `{{adminActor}}`, `{{host}}`, etc. The `host` must match the port in `Properties/launchSettings.json`.

## Implementation Order and Build Checkpoints

🔴 **Implement layer by layer and build between layers.** Trellis uses source generators (for `partial Maybe<T>` properties and Mediator) that emit code during compilation. Later layers (Acl, Api) reference generated code from earlier layers. If you create all files at once without building, the generated code won't exist and the compiler will report errors.

**Required sequence:**
1. **Domain/src** — Implement all value objects, aggregates, entities, events, specifications, permissions. Then run `dotnet build`.
2. **Application/src** — Implement repository interfaces, commands, queries, handlers. Then run `dotnet build`.
3. **Acl/src** — Implement DbContext, entity configurations, repositories, resource loaders. Then run `dotnet build`.
4. **Api/src** — Implement controllers, DTOs, Program.cs, IActorProvider. Then run `dotnet build`.
5. **Tests** — Implement Domain.Tests, Application.Tests, Api.Tests. Then run `dotnet test`.

The build after Domain is especially critical — it triggers the `MaybePartialPropertyGenerator` which emits the `_camelCase` backing fields that Acl entity configurations reference (e.g., `HasIndex("Status", "_submittedAt")`).

## Key Conventions

### Commands and Queries

- 🔴 Commands receive **value object types** (e.g., `CustomerId`, not `Guid`). Scalar value binding validates at the API layer — handlers never call `TryCreate` on command properties.
- 🟡 Use `IValidate` **only** for cross-field or collection validation (e.g., "at least one line item"). Single-field validation is handled by value objects. `Validate()` returns `IResult` — use `Result.Success()` for valid, `Result.Failure(ValidationError.For(...))` for invalid:
```csharp
public IResult Validate() =>
    LineItems.Count > 0
        ? Result.Success()
        : Result.Failure(ValidationError.For("lineItems", "At least one line item is required."));
```
- 🟡 Use `IAuthorize` for permission-based authorization. Use `IAuthorizeResource<TResource>` for resource-based authorization (e.g., "only the owner can cancel"). `Authorize()` returns `IResult`:
```csharp
public IResult Authorize(Actor actor, Order order) =>
    actor.HasPermission(Permissions.OrdersReadAll) || actor.IsOwner(order.CreatedByActorId.Value)
        ? Result.Success()
        : Result.Failure(Error.Forbidden("Only the order creator or an admin can cancel."));
```
- **`IAuthorizeResource<TResource>`:** The pipeline loads the resource via an `IResourceLoader<TMessage, TResource>` before calling `Authorize(Actor, TResource)`. The handler receives the entity already authorized — no auth logic in handlers. Register the resource loader as scoped in the Acl layer. Use `ResourceLoaderById<TMessage, TResource, TId>` as a convenience base class for ID-based lookups.
- **Registration:** `AddResourceAuthorization(params Assembly[])` scans assemblies for both `IAuthorizeResource<T>` commands and `IResourceLoader<,>` implementations. Pass both the Application assembly (commands) and the Acl assembly (loaders):
```csharp
// In Acl/src/DependencyInjection.cs
services.AddResourceAuthorization(
    typeof(CancelOrderCommand).Assembly,        // Application — finds IAuthorizeResource commands
    typeof(CancelOrderResourceLoader).Assembly); // Acl — finds IResourceLoader implementations
```
- 🔴 **`Unit` type disambiguation:** Both `Trellis` and `Mediator` define a `Unit` type. In handler return types and ROP chains, always use `Trellis.Unit` (or `default(Trellis.Unit)`). The global `using Trellis;` directive makes the unqualified `Unit` resolve to `Trellis.Unit`, but when both namespaces are imported, qualify explicitly.

### Handler ROP Pattern

**Use `Bind`/`BindAsync` chains in handlers — not imperative `if`/`return`.** Handlers should compose Result operations using the ROP pipeline, not unwrap results manually.

🟡 **Prefer `Bind`/`BindAsync`/`Tap`/`Map` chains** for straightforward flows. ROP chains make the success path obvious and ensure errors propagate automatically.

🟢 **Explicit branching is acceptable** when a handler has complex conditional logic (e.g., multiple independent branches, intermediate calculations) and a chain would reduce readability. Keep branching handlers short and ensure every path still returns `Result<T>`.

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
// ✅ Preferred — ROP chain with Bind/BindAsync
public async ValueTask<Result<OrderDto>> Handle(SubmitOrderCommand command, CancellationToken cancellationToken) =>
    await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken)
        .BindAsync(order => order.Submit())
        .TapAsync(order => _orderRepository.SaveAsync(order, cancellationToken))
        .MapAsync(OrderDto.From);

// 🟢 Acceptable — imperative style when it improves readability for complex branching
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
// ✅ Preferred — parallel fetches with ParallelAsync
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

// 🟡 Avoid — sequential fetches lose parallelism
var customer = await _customerRepository.GetByIdAsync(command.CustomerId, cancellationToken);
var products = await _productRepository.GetByIdsAsync(command.ProductIds, cancellationToken);
```

### State Machines (Trellis.Stateless)

Use `Trellis.Stateless` for aggregate state transitions. The `FireResult()` extension returns `Result<TState>` instead of throwing on invalid transitions.

🔴 **Lazy initialization required for EF Core.** The third-party `StateMachine<TState, TTrigger>` constructor eagerly invokes its `stateAccessor` function. When EF Core materializes an aggregate via its parameterless constructor, state properties are not yet populated — causing a `NullReferenceException`. Use lazy initialization:

```csharp
// ✅ Lazy — defers construction until first use (after EF Core populates properties)
private StateMachine<string, string>? _machine;
private StateMachine<string, string> Machine => _machine ??= ConfigureStateMachine();

// ❌ Wrong — eager construction crashes when EF Core calls parameterless constructor
// private readonly StateMachine<string, string> _machine = new(...);
```

### EF Core

- 🔴 **Always call `ApplyTrellisConventions`** in `ConfigureConventions` — it handles all scalar Trellis value objects automatically. 🟡 Do not write `HasConversion()` for types handled by Trellis conventions. 🟢 If a custom type is not handled by `ApplyTrellisConventions`, use explicit EF mapping (`HasConversion`, `OwnsOne`, etc.) for that type only.
- 🟡 **`Money` properties** are auto-mapped by `ApplyTrellisConventions` — no `OwnsOne` needed. See §12 in `trellis-api-reference.md` for column naming.
- 🟢 **Custom composite `ValueObject` types** (e.g., `ShippingAddress` with multiple fields) are NOT auto-mapped. Map them with `OwnsOne` in the entity configuration and configure each property explicitly.
- 🟡 **Owned collection property types** — use `IReadOnlyList<T>` (not `ReadOnlyCollection<T>`) for `OwnsMany` navigation properties. EF Core cannot populate `ReadOnlyCollection<T>` from a backing `List<T>` field during materialization.
- 🔴 Use `SaveChangesResultUnitAsync` in repositories (returns `Result<Unit>`). Never use bare `SaveChangesAsync`.
- 🟡 Use `FirstOrDefaultMaybeAsync` for optional lookups, `FirstOrDefaultResultAsync` for required lookups.
- 🟡 Use `.Where(specification)` for specification queries. Specifications support `.And()`, `.Or()`, `.Not()` composition.
- 🔴 **`Maybe<T>` properties** — declare as `partial`. The source generator and `MaybeConvention` handle everything automatically — no manual backing fields or EF configuration needed. See §12 in `trellis-api-reference.md`. The `_camelCase` backing field is emitted by the source generator during `dotnet build` (see **Implementation Order and Build Checkpoints** above). 🔴 **If using EF Core**, add `Trellis.EntityFrameworkCore.Generator` to the **Domain project** (as an Analyzer, with `ReferenceOutputAssembly="false"`). The generator must be in the project that declares entities with `partial Maybe<T>` properties — without it, backing fields will not be emitted and EF Core persistence will silently fail.
- 🔴 **`Maybe<T>` in indexes** — `HasIndex` with `Maybe<T>` properties requires string-based backing field references because `MaybeConvention` ignores the CLR property. Use the backing field name (underscore + camelCase): `builder.HasIndex("Status", "_submittedAt")`. Do NOT use lambda expressions like `o => new { o.Status, o.SubmittedAt }` — they will silently fail.
- 🟡 **`Maybe<T>` LINQ queries** — use `WhereNone`, `WhereHasValue`, `WhereEquals` extension methods. For comparisons on `Maybe<T>` properties (e.g., date thresholds), use `WhereLessThan`, `WhereLessThanOrEqual`, `WhereGreaterThan`, `WhereGreaterThanOrEqual`. These rewrite the expression tree to target the backing storage field. See §12 in `trellis-api-reference.md`.
```csharp
// ✅ Overdue orders: SubmittedAt < 7 days ago
var cutoff = DateTime.UtcNow.AddDays(-7);
context.Orders
    .Where(o => o.Status == OrderStatus.Submitted)
    .WhereLessThan(o => o.SubmittedAt, cutoff);
```
- 🟡 **Entity configurations:** Use `IEntityTypeConfiguration<T>` per entity in the Acl layer — one file per aggregate/entity (e.g., `OrderConfiguration.cs`, `CustomerConfiguration.cs`). Register them with `ApplyConfigurationsFromAssembly` in `OnModelCreating`. Do NOT inline configuration in `DbContext.OnModelCreating`.
- 🟡 **`MaybeQueryInterceptor`** — enables natural LINQ with `Maybe<T>` properties. Register via `optionsBuilder.AddTrellisInterceptors()` (uses a singleton internally). See §12 in `trellis-api-reference.md`.
- 🟡 **Migrations:** After implementing all entities and configurations, run `dotnet ef migrations add InitialCreate -p Acl/src -s Api/src` to generate the initial migration. Do not rely on `EnsureCreated()` for anything beyond a quick prototype.

### MVC Controllers

Controllers inherit `ControllerBase` with `[ApiController]`. Actions are thin — send command via Mediator, chain `.ToActionResult(this)` or `.ToActionResultAsync(this)`.

**Mapping domain types to DTOs in controllers:** Use the mapping overload to transform results inline:
```csharp
// GET endpoint — map domain to DTO
var result = await _sender.Send(new GetOrderByIdQuery(id), ct);
return result.ToActionResult(this, OrderDto.From);

// Or fully async chained:
return await _sender.Send(new GetOrderByIdQuery(id), ct)
    .ToActionResultAsync(this, OrderDto.From);
```

**Every controller must have:**
- `[ApiController]` attribute and inherit `ControllerBase`
- `[Route("api/[controller]")]` at class level
- `[Consumes("application/json")]` and `[Produces("application/json")]` at class level
- Error responses as RFC 9457 Problem Details (handled by `ToActionResult`)

**Do NOT add `[ApiVersion]` attributes.** Version is derived automatically from the controller namespace via `VersionByNamespaceConvention` (see API Versioning below).

**Use `ToCreatedAtActionResult`** for POST endpoints that create resources — returns `201 Created` with `Location` header. The `actionName` parameter is the GET action method name (e.g., `nameof(GetOrder)`). When the GET action is on the **same controller**, the controller name is inferred automatically. When it's on a different controller, pass the controller name (class name minus `Controller` suffix):
```csharp
// Same controller — controller name inferred
result.ToCreatedAtActionResult(this, nameof(GetOrder), o => new { id = o.Id.Value }, OrderDto.From);

// Different controller — specify controller name explicitly
result.ToCreatedAtActionResult(this, nameof(CustomersController.GetCustomer), o => new { id = o.Id.Value }, CustomerDto.From, "Customers");
```

### Automatic Scalar Value Binding

🔴 **Use value object types — not primitives — in controller action parameters.** Trellis automatically converts route parameters, query parameters, and JSON body properties via model binding and JSON converters. Never call `.Create()` or `.TryCreate()` manually in controllers.

**Registration** — add scalar value validation to the MVC pipeline in `Api/src/DependencyInjection.cs`:
```csharp
services.AddControllers().AddScalarValueValidation();
```
And activate the middleware in `Program.cs`:
```csharp
app.UseScalarValueValidation();
```

🟡 **Request/Response DTOs** live in `Api/src/{version}/Models/` (e.g., `Api/src/2026-11-12/Models/`). 🔴 Never expose domain types directly. Request DTOs can use scalar value object types as properties — they will be validated automatically via the JSON converter.

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

**Testing API Reference:** See `.github/trellis-api-testing-reference.md` for all Trellis.Testing types, assertion methods, FakeRepository, TestActorProvider, and testing patterns including TRLS003 workarounds.

**Domain tests:** Pure unit tests, no external dependencies. Test value object TryCreate, aggregate rules, state machine transitions, specifications.

**Application tests:** Mock repository interfaces. Test handler logic, authorization checks, error mapping. Use `Xunit.DependencyInjection` for test DI with a `Startup.cs` that registers Mediator and mock services.

**API integration tests:** Use `WebApplicationFactory<Program>` with SQLite in-memory. Test HTTP round-trips, status codes, Problem Details, authorization enforcement. Use `MartinCostello.Logging.XUnit.v3` for test logging.

**Do NOT** create `GlobalUsings.cs` files in test projects. Global usings come from `build/test.props`.

🔴 **`Maybe<T>` assertions** — use `.Should().HaveValue()` and `.Should().BeNone()` (from `Trellis.Testing`) to assert on `Maybe<T>` values. Do NOT use `.HasValue.Should().BeTrue()` or `.HasNoValue.Should().BeTrue()` — these bypass Trellis.Testing's assertion messages. Also available: `.Should().HaveValueEqualTo(expected)` and `.Should().HaveValueMatching(predicate)`.
```csharp
// ✅ Correct — Trellis.Testing assertions
customer.PhoneNumber.Should().HaveValue();
customer.PhoneNumber.Should().BeNone();
order.SubmittedAt.Should().HaveValue();
order.SubmittedAt.Should().HaveValueMatching(d => d > DateTime.UtcNow.AddMinutes(-1));

// ❌ Wrong — bypasses Trellis.Testing, poor error messages
customer.PhoneNumber.HasValue.Should().BeTrue();
customer.PhoneNumber.HasNoValue.Should().BeTrue();
```

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

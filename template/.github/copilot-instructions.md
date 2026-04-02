# Copilot Instructions — Building with Trellis

This project uses the **Trellis** framework (.NET 10). Trellis combines Railway-Oriented Programming (ROP) with Domain-Driven Design (DDD). Follow these patterns exactly.

**API Reference:** See the `.github/trellis-api-*.md` files for all Trellis types, method signatures, and usage patterns. Key files: `trellis-api-results.md` (Result, Maybe, Error types), `trellis-api-asp.md` (response mappers, ETag helpers), `trellis-api-efcore.md` (EF Core conventions), `trellis-api-ddd.md` (aggregates, entities, value objects), `trellis-api-primitives.md` (built-in value objects). See `trellis-api-testing-reference.md` for testing APIs (FluentAssertions extensions, FakeRepository, TestActorProvider, test patterns).

## Todo Sample — Study Before Replacing

🔴 **Before implementing your service, read all source files in the Todo sample.** The template ships with a working Todo application that demonstrates every key Trellis pattern. Study how it's built — then replace the Todo files with your domain.

Key patterns demonstrated in the sample:
- **Value objects** with `RequiredGuid`, `RequiredString`, `RequiredDateTime`, `ValidateAdditional` — see `Domain/src/ValueObjects/`
- **`RequiredEnum` smart enum** replacing C# `enum` — see `Domain/src/TodoStatus.cs`
- **Aggregate** with `LazyStateMachine` and `Maybe<T>` partial properties — see `Domain/src/Aggregates/TodoItem.cs`
- **Specification** with `.And()` composition — see `Domain/src/Specifications/OverdueTodoSpecification.cs`
- **Always-valid commands** with private constructor + `TryCreate` — see `Application/src/Todos/UpdateTodoCommand.cs`
- **`Result.Ensure`** for authorization checks — see `Application/src/Todos/CompleteTodoCommand.cs`
- **`IAuthorizeResource<T>`** with `ResourceLoaderById` — see `CompleteTodoCommand` + `Acl/src/CompleteTodoResourceLoader.cs`
- **Repository returning `Maybe<T>`** for lookups — see `Application/src/Todos/ITodoRepository.cs`
- **Handlers returning domain types**, DTO mapping at the controller — see handlers vs `Api/src/2026-03-26/Models/TodoResponse.cs`
- **`TimeProvider`** for testable time-dependent validation — see `UpdateTodoCommand.TryCreate`
- **Controller `TryCreate` → `BindAsync` → `Send`** pattern — see `Api/src/2026-03-26/Controllers/TodosController.cs`
- **Domain, Application, and API tests** — see `*/tests/` directories

## Core Principles

1. 🔴 **Errors are values, not exceptions.** Use `Result<T>` for expected failures. Never throw for business logic. Never use try/catch in Domain or Application layers.
2. 🔴 **Make illegal states unrepresentable.** Every domain concept is a value object with `TryCreate`. If it exists, it's valid.
3. 🔴 **No primitive obsession on domain surfaces.** No raw `Guid`, `string`, `int`, or `decimal` in aggregate/entity properties or public domain method signatures. Every property on an Aggregate or Entity must be a typed value object. If the same concept appears in two contexts (e.g., line item quantity vs. stock quantity), create separate types for each. 🟢 Private helpers and internal implementation details may use primitives when the value object adds no safety benefit.
4. 🟡 **Use built-in `Trellis.Primitives` before creating custom value objects.** `EmailAddress`, `PhoneNumber`, `Url`, `Hostname`, `IpAddress`, `Slug`, `CountryCode`, `CurrencyCode`, `LanguageCode`, `Age`, `Percentage`, and `Money` are already provided with full validation, JSON converters, and EF Core support. Only create custom value objects for domain concepts not covered by these. Use `[StringLength]` on `RequiredString<T>`, `[Range]` on `RequiredInt<T>`, and `ValidateAdditional` for custom validation (regex, format checks) — see `trellis-api-primitives.md`. Do NOT hand-roll `ScalarValueObject<T, string>` when `RequiredString<T>` + `ValidateAdditional` can express the same validation.
   > ⚠️ **`[StringLength]` and `[Range]` are Trellis attributes** (`Trellis.StringLengthAttribute`, `Trellis.RangeAttribute`), not `System.ComponentModel.DataAnnotations`. The global `using Trellis;` directive resolves them automatically. Do NOT add `using System.ComponentModel.DataAnnotations;` — the wrong attribute will silently compile but the source generator won't recognize it.
5. 🔴 **When the spec says "enum", use `RequiredEnum<T>` — not C# `enum`.** In the domain assembly, all enum-like concepts must be modeled as `RequiredEnum<T>` (smart enums). C# `enum` types are forbidden in domain aggregates, entities, and value objects. `RequiredEnum<T>` provides type-safe validation, string-based serialization, auto-wired JSON converters, auto-wired EF Core conventions (via `ApplyTrellisConventions` — no `HasConversion` needed), and the ability to attach behavior (properties, methods, state transitions). Declare them as `partial class` so the source generator emits `TryCreate`, `Create`, `Parse`, and `TryParse`:
```csharp
// ✅ Correct — RequiredEnum smart enum
public partial class OrderStatus : RequiredEnum<OrderStatus>
{
    public static readonly OrderStatus Draft = new();
    public static readonly OrderStatus Confirmed = new();
    public static readonly OrderStatus Shipped = new();
    public static readonly OrderStatus Cancelled = new();
}

// ✅ With custom wire/storage names (when external name differs from field name)
public partial class PaymentMethod : RequiredEnum<PaymentMethod>
{
    [EnumValue("credit-card")]
    public static readonly PaymentMethod CreditCard = new();
    [EnumValue("bank-transfer")]
    public static readonly PaymentMethod BankTransfer = new();
    public static readonly PaymentMethod Cash = new();   // wire name = "Cash"
}

// ✅ With behavior — attach domain logic to enum values
public partial class OrderStatus : RequiredEnum<OrderStatus>
{
    public static readonly OrderStatus Draft = new(canModify: true, isTerminal: false);
    public static readonly OrderStatus Confirmed = new(canModify: false, isTerminal: false);
    public static readonly OrderStatus Cancelled = new(canModify: false, isTerminal: true);

    public bool CanModify { get; }
    public bool IsTerminal { get; }

    private OrderStatus(bool canModify, bool isTerminal)
    {
        CanModify = canModify;
        IsTerminal = isTerminal;
    }
}

// ❌ Never do this in domain — no validation, no type safety, no auto-converters
public enum OrderStatus { Draft, Confirmed, Shipped, Cancelled }
```
   > **Usage:** `RequiredEnum<T>` values are compared with `==`, used in EF Core LINQ queries (`Where(o => o.Status == OrderStatus.Draft)`), serialized as strings in JSON, and work with `LazyStateMachine<TState, TTrigger>` as the state type. Access the string value via `.Value`. Use `GetAll()` to enumerate all members, `TryFromName("Draft")` for string-to-enum lookup.
   > **EF Core:** `RequiredEnum<T>` is stored as a `string` column. `ApplyTrellisConventions` auto-registers the converter — do NOT add `HasConversion<string>()`. Properties on aggregates should use `= null!` initializer (reference type) and `private set`.
6. 🔴 **Optional values use `Maybe<T>`, never null.** `Maybe<PhoneNumber>`, not `PhoneNumber?`. When using EF Core, declare `Maybe<T>` properties as `partial` so the source generator can emit the backing field for persistence.
7. 🟡 **Composite value objects** (e.g., `ShippingAddress`) — use `Result.Combine` to validate all fields and return all errors at once, not just the first:
```csharp
public static Result<ShippingAddress> TryCreate(
    string? street, string? city, string? state, string? postalCode, string? country) =>
    Result.Combine(
        Street.TryCreate(street),
        City.TryCreate(city),
        State.TryCreate(state),
        PostalCode.TryCreate(postalCode),
        Country.TryCreate(country))
    .Map((s, c, st, p, co) => new ShippingAddress { Street = s, City = c, State = st, PostalCode = p, Country = co });
```
This returns a `ValidationError` containing all field errors when multiple fields are invalid — not just the first one. See `trellis-api-results.md` §Combine and §ValidationError.

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
- 🔴 **Repository lookups return `Maybe<T>`**, not `Result<T>`. Absence is not a failure — it's data. The handler decides whether "not found" is an error by calling `.ToResult(Error.NotFound(...))`. This keeps repositories as pure data access and puts domain interpretation in the handler.
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
│   ├── trellis-api-results.md     ← Result, Maybe, Error types
│   ├── trellis-api-asp.md         ← Response mappers, ETag helpers
│   ├── trellis-api-ddd.md         ← Aggregates, entities, value objects
│   ├── trellis-api-primitives.md  ← Built-in value objects
│   ├── trellis-api-efcore.md      ← EF Core conventions
│   ├── trellis-api-mediator.md    ← Pipeline behaviors
│   ├── trellis-api-authorization.md ← Actor, permissions
│   ├── trellis-api-http.md        ← HttpClient → Result extensions
│   ├── trellis-api-stateless.md   ← State machine integration
│   ├── trellis-api-fluentvalidation.md ← FluentValidation bridge
│   ├── trellis-api-analyzers.md   ← TRLS/TRLSGEN diagnostics
│   ├── trellis-api-patterns.md    ← Usage patterns, workarounds
│   └── trellis-api-testing-reference.md ← Testing API surface
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

The build after Domain is especially critical — it triggers the `MaybePartialPropertyGenerator` which emits the `_camelCase` backing fields that Acl entity configurations reference (e.g., `HasTrellisIndex(o => new { o.Status, o.SubmittedAt })`).

## Key Conventions

### Commands and Queries

- 🔴 Commands receive **value object types** (e.g., `TodoId`, not `Guid`). Scalar value binding validates at the API layer — handlers never call `TryCreate` on command properties.
- 🔴 **Commands should be always-valid.** Use a private constructor + `TryCreate` factory method for commands with cross-field validation. This ensures invalid commands cannot exist — no reliance on pipeline behaviors for validation:
```csharp
public sealed record UpdateTodoCommand : ICommand<Result<TodoItem>>, IAuthorize
{
    public TodoId TodoId { get; }
    public Title Title { get; }
    public DueDate DueDate { get; }

    private UpdateTodoCommand(TodoId todoId, Title title, DueDate dueDate) { ... }

    public static Result<UpdateTodoCommand> TryCreate(TodoId todoId, Title title, DueDate dueDate, TimeProvider? timeProvider = null) =>
        Result.Ensure(dueDate > (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime,
                Error.Validation("Due date must be in the future.", "dueDate"))
            .Map(_ => new UpdateTodoCommand(todoId, title, dueDate));
}
```
- 🟢 Use `TimeProvider` as an optional parameter (defaulting to `TimeProvider.System`) in `TryCreate` methods that validate against the current time. This makes time-dependent validation testable with `FakeTimeProvider`.
- 🔴 **No `DateTime.UtcNow` or `DateTimeOffset.UtcNow` anywhere.** All timestamps must come from `TimeProvider` — in domain methods, domain events, handlers, and repositories. Pass `TimeProvider` as a parameter to domain methods that set timestamps or raise time-stamped events, and inject it into repositories that compute time-based queries:
```csharp
// ✅ Domain method — receives TimeProvider, uses it for event timestamp
public Result<Order> Approve(TimeProvider timeProvider) =>
    _machine.FireResult(Triggers.Approve)
        .Tap(_ => DomainEvents.Add(new OrderApprovedEvent(Id, OccurredAt: timeProvider.GetUtcNow().UtcDateTime)))
        .Map(_ => this);

// ✅ Repository — injects TimeProvider for time-based queries
public async Task<IReadOnlyList<Order>> GetOverdueAsync(CancellationToken ct)
{
    var cutoff = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-7);
    return await _context.Orders
        .Where(o => o.Status == OrderStatus.Submitted)
        .WhereLessThan(o => o.SubmittedAt, cutoff)
        .ToListAsync(ct);
}

// ❌ Never do this — untestable
public Result<Order> Approve() =>
    _machine.FireResult(Triggers.Approve)
        .Tap(_ => DomainEvents.Add(new OrderApprovedEvent(Id, OccurredAt: DateTime.UtcNow)))  // ❌
        .Map(_ => this);
```
- 🟡 **Handlers return domain types** (e.g., `Result<TodoItem>`, not `Result<TodoDto>`). DTO mapping is a presentation concern — do it in the controller, not the handler.
- 🟡 Use `IValidate` **only** when `TryCreate` is not feasible (e.g., collection validation where the command must already exist). `Validate()` returns `IResult`:
```csharp
public IResult Validate() =>
    LineItems.Count > 0
        ? Result.Success()
        : Result.Failure(ValidationError.For("lineItems", "At least one line item is required."));
```
  > 🟢 **Validate early, enforce always.** It is acceptable for `IValidate` and the domain aggregate's `TryCreate` to check the same invariant (e.g., "at least one line item"). `IValidate` runs in the pipeline before the handler — failing fast before loading aggregates. The domain enforces its own invariants regardless of how it's called. This duplication is intentional, not a bug.
- 🟡 Use `IAuthorize` for permission-based authorization. Use `IAuthorizeResource<TResource>` for resource-based authorization (e.g., "only the owner can complete"). Use `Result.Ensure` for clean authorization checks:
```csharp
public IResult Authorize(Actor actor, TodoItem resource) =>
    Result.Ensure(actor.IsOwner(resource.CreatedByActorId),
        Error.Forbidden("Only the creator can complete this todo."));
```
- **`IAuthorizeResource<TResource>`:** The pipeline loads the resource via an `IResourceLoader<TMessage, TResource>` before calling `Authorize(Actor, TResource)`. The handler receives the entity already authorized — no auth logic in handlers. Register the resource loader as scoped in the Acl layer. Use `ResourceLoaderById<TMessage, TResource, TId>` as a convenience base class for ID-based lookups.
- **Registration:** `AddResourceAuthorization(params Assembly[])` scans assemblies for both `IAuthorizeResource<T>` commands and `IResourceLoader<,>` implementations. Pass both the Application assembly (commands) and the Acl assembly (loaders):
```csharp
// In Acl/src/DependencyInjection.cs
services.AddResourceAuthorization(
    typeof(CompleteTodoCommand).Assembly,        // Application — finds IAuthorizeResource commands
    typeof(CompleteTodoResourceLoader).Assembly); // Acl — finds IResourceLoader implementations
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
// 🔴 Use BindAsync (not TapAsync) for SaveAsync — it returns Result<Unit> which may contain
// a ConflictError (duplicate key) or other DB failure. TapAsync silently discards the Result.
public async ValueTask<Result<Order>> Handle(SubmitOrderCommand command, CancellationToken cancellationToken) =>
    await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken)
        .BindAsync(order => order.Submit())
        .BindAsync(order => _orderRepository.SaveAsync(order, cancellationToken).MapAsync(_ => order));

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

### Multi-Aggregate Save with TraverseAsync

When a handler modifies multiple aggregates (e.g., reserving stock on products then saving the order), use `TraverseAsync` to stay in the ROP pipeline. `TraverseAsync` applies a Result-returning function to each element, short-circuiting on the first failure.

🔴 **All aggregates share the same `DbContext`.** EF Core tracks all changes in a single unit of work — calling `SaveChangesResultUnitAsync` once at the end commits everything atomically. Do NOT call `SaveAsync` per aggregate (which commits each individually); instead, stage all changes and save once:

```csharp
// ✅ Preferred — stage all changes, single SaveChanges at the end
public async ValueTask<Result<Order>> Handle(SubmitOrderCommand command, CancellationToken cancellationToken) =>
    await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken)
        .BindAsync(order => order.Submit(products, _timeProvider))
        .CheckAsync(_ => _orderRepository.SaveAsync(order, cancellationToken));
        // Products are tracked by the same DbContext — saved atomically with the order

// ✅ When repositories use different DbContexts or you need explicit multi-save,
// use TraverseAsync to keep the ROP chain:
await products.TraverseAsync(
        (p, ct) => _productRepository.SaveAsync(p, ct), cancellationToken)
    .CheckAsync(_ => _orderRepository.SaveAsync(order, cancellationToken));

// ❌ Avoid — imperative foreach breaks the ROP chain
foreach (var product in products)
{
    var saveResult = await _productRepository.SaveAsync(product, cancellationToken);
    if (saveResult.IsFailure) return saveResult.Error;
}
```

`TraverseAsync` supports `CancellationToken`, `Task`, and `ValueTask` overloads. See `trellis-api-results.md` §Traverse for the full API.

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

🔴 **Lazy initialization required for EF Core.** The third-party `StateMachine<TState, TTrigger>` constructor eagerly invokes its `stateAccessor` function. When EF Core materializes an aggregate via its parameterless constructor, state properties are not yet populated — causing a `NullReferenceException`. Use `LazyStateMachine<TState, TTrigger>` which defers construction until first access:

```csharp
// ✅ LazyStateMachine — defers construction until first use (after EF Core populates properties)
private readonly LazyStateMachine<OrderStatus, string> _machine;

private Order() // EF Core constructor
{
    _machine = new LazyStateMachine<OrderStatus, string>(
        () => Status,
        s => Status = s,
        ConfigureStateMachine);
}

private static void ConfigureStateMachine(StateMachine<OrderStatus, string> machine)
{
    // Configure transitions here
}

// Usage in domain methods:
public Result<OrderStatus> Submit() => _machine.FireResult("Submit");
```

### EF Core

- 🔴 **EF Core materialization boilerplate** — every `Aggregate<T>` and `Entity<T>` persisted by EF Core needs:
  1. A **private parameterless constructor** (EF Core cannot materialize without it)
  2. **`private set`** on all properties (EF Core sets properties via reflection)
  3. **`= null!`** on reference-type properties (suppresses nullable warning for the private constructor path)
  4. **`base(default!)`** in the parameterless constructor (satisfies the base class's required ID parameter)

```csharp
public class Customer : Aggregate<CustomerId>
{
    public FirstName FirstName { get; private set; } = null!;
    public LastName LastName { get; private set; } = null!;
    public EmailAddress Email { get; private set; } = null!;
    public ShippingAddress ShippingAddress { get; private set; } = null!;
    public partial Maybe<PhoneNumber> Phone { get; set; }

    private Customer() : base(default!) { }  // EF Core materialization

    // Public factory — the only way to create instances
    public static Result<Customer> TryCreate(...) => ...;
}
```

- 🔴 **Always call `ApplyTrellisConventions`** in `ConfigureConventions` — it handles all scalar Trellis value objects automatically, including `RequiredString<T>`, `RequiredGuid<T>`, `RequiredInt<T>`, `RequiredDecimal<T>`, `RequiredDateTime<T>`, `RequiredBool<T>`, `RequiredLong<T>`, `RequiredEnum<T>`, and all built-in primitives (`EmailAddress`, `PhoneNumber`, etc.). 🔴 Do NOT write `HasConversion()` for types handled by Trellis conventions — it silently overrides the convention and causes runtime failures. 🟢 If a custom type is not handled by `ApplyTrellisConventions`, use explicit EF mapping (`HasConversion`, `OwnsOne`, etc.) for that type only.
- 🟡 **`Money` properties** are auto-mapped by `ApplyTrellisConventions` — no `OwnsOne` needed. See `trellis-api-efcore.md` for column naming.
- 🟢 **Custom composite `ValueObject` types** (e.g., `ShippingAddress` with multiple fields) are NOT auto-mapped. Map them using standard EF Core owned entities (`OwnsOne`, `OwnsMany`) — refer to [EF Core Owned Entity Types documentation](https://learn.microsoft.com/en-us/ef/core/modeling/owned-entities).
- 🟡 **Owned collection property types** — use `IReadOnlyList<T>` (not `ReadOnlyCollection<T>`) for `OwnsMany` navigation properties. EF Core cannot populate `ReadOnlyCollection<T>` from a backing `List<T>` field during materialization.
- 🔴 Use `SaveChangesResultUnitAsync` in repositories (returns `Result<Unit>`). Never use bare `SaveChangesAsync`.
- 🟡 Use `FirstOrDefaultMaybeAsync` for optional lookups, `FirstOrDefaultResultAsync` for required lookups.
- 🟡 Use `.Where(specification)` for specification queries. Specifications support `.And()`, `.Or()`, `.Not()` composition.
- 🟡 **`Maybe<T>` in `Specification<T>` expressions** — accessing `Maybe<T>.Value` inside `ToExpression()` triggers TRLS006, even with a `HasValue` guard in the same `&&` chain. Suppress with `#pragma warning disable TRLS006` around the expression:
```csharp
public override Expression<Func<Order, bool>> ToExpression() =>
#pragma warning disable TRLS006
    order => order.Status == OrderStatus.Submitted
          && order.SubmittedAt.HasValue
          && order.SubmittedAt.Value < _cutoff;
#pragma warning restore TRLS006
```
- 🔴 **`Maybe<T>` properties** — declare as `partial`. The source generator and `MaybeConvention` handle everything automatically — no manual backing fields or EF configuration needed. See `trellis-api-efcore.md`. The `_camelCase` backing field is emitted by the source generator during `dotnet build` (see **Implementation Order and Build Checkpoints** above). 🔴 **If using EF Core**, add `Trellis.EntityFrameworkCore.Generator` to the **Domain project** (as an Analyzer, with `ReferenceOutputAssembly="false"`). The generator must be in the project that declares entities with `partial Maybe<T>` properties — without it, backing fields will not be emitted and EF Core persistence will silently fail.
- 🔴 **`Maybe<T>` in indexes** — Use `HasTrellisIndex` (not `HasIndex`) for indexes that include `Maybe<T>` properties. `HasTrellisIndex` accepts lambda expressions and automatically resolves `Maybe<T>` properties to their backing field names: `builder.HasTrellisIndex(o => new { o.Status, o.SubmittedAt })` resolves to `HasIndex("Status", "_submittedAt")`. Plain `HasIndex` with lambdas will silently fail for `Maybe<T>` properties. See `trellis-api-efcore.md`.
- 🟡 **`Maybe<T>` LINQ queries** — use `WhereNone`, `WhereHasValue`, `WhereEquals` extension methods. For comparisons on `Maybe<T>` properties (e.g., date thresholds), use `WhereLessThan`, `WhereLessThanOrEqual`, `WhereGreaterThan`, `WhereGreaterThanOrEqual`. These rewrite the expression tree to target the backing storage field. See `trellis-api-efcore.md`.
```csharp
// ✅ Overdue orders: SubmittedAt < 7 days ago
var cutoff = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-7);
context.Orders
    .Where(o => o.Status == OrderStatus.Submitted)
    .WhereLessThan(o => o.SubmittedAt, cutoff);
```
- 🟡 **Entity configurations:** Use `IEntityTypeConfiguration<T>` per entity in the Acl layer — one file per aggregate/entity (e.g., `OrderConfiguration.cs`, `CustomerConfiguration.cs`). Register them with `ApplyConfigurationsFromAssembly` in `OnModelCreating`. Do NOT inline configuration in `DbContext.OnModelCreating`.
- 🟡 **`MaybeQueryInterceptor`** — enables natural LINQ with `Maybe<T>` properties. Register via `optionsBuilder.AddTrellisInterceptors()` (uses a singleton internally). See `trellis-api-efcore.md`.
- 🟡 **Migrations:** After implementing all entities and configurations, run `dotnet ef migrations add InitialCreate -p Acl/src -s Api/src` to generate the initial migration. Do not rely on `EnsureCreated()` for anything beyond a quick prototype.
- 🟢 **Optimistic concurrency (RFC 9110)** is automatic. `Aggregate<TId>.ETag` is configured as a concurrency token by `ApplyTrellisConventions` and auto-generated on save by `AddTrellisInterceptors()`. Use RFC 9110 conditional requests:
  - **GET** responses: use `ToETagActionResultAsync(this, etagSelector, map)` to set the `ETag` response header
  - **PUT/PATCH/DELETE** requests: read `If-Match` header via `ETagHelper.ParseIfMatch(Request)`, pass to command as `EntityTagValue[]? IfMatchETags`. Handler calls `.OptionalETag(command.IfMatchETags)` (skips if absent) or `.RequireETag(command.IfMatchETags)` (428 if absent) → returns 412 Precondition Failed if stale
  - **GET** with `If-None-Match`: automatically returns 304 Not Modified when ETag matches (handled by `ToETagActionResult`)

### MVC Controllers

Controllers inherit `ControllerBase` with `[ApiController]`. Actions are thin — send command via Mediator, chain `.ToActionResult(this)` or `.ToActionResultAsync(this)`.

**Mapping domain types to DTOs in controllers:** Handlers return domain types. Map to response DTOs in the controller using the mapping overload:
```csharp
// GET endpoint — map domain aggregate to response DTO
return await _sender.Send(new GetTodoByIdQuery(id), cancellationToken)
    .ToActionResultAsync(this, TodoResponse.From);

// POST endpoint — 201 Created with Location header and mapping
return await _sender.Send(
    new CreateTodoCommand(request.Title, request.DueDate, request.Tag),
    cancellationToken)
    .ToCreatedAtActionResultAsync(this, nameof(GetById), r => new { id = r.Id }, TodoResponse.From);
```

**Commands with `TryCreate` validation:** When a command uses `TryCreate`, chain it into `Send` via `BindAsync` in the controller:
```csharp
return await UpdateTodoCommand.TryCreate(id, request.Title, request.DueDate, request.Tag)
    .BindAsync(command => _sender.Send(command, cancellationToken))
    .ToActionResultAsync(this, TodoResponse.From);
```

**Every controller must have:**
- `[ApiController]` attribute and inherit `ControllerBase`
- `[Route("api/[controller]")]` at class level
- `[Consumes("application/json")]` and `[Produces("application/json")]` at class level
- Error responses as RFC 9457 Problem Details (handled by `ToActionResult`)

**Do NOT add `[ApiVersion]` attributes.** Version is derived automatically from the controller namespace via `VersionByNamespaceConvention` (see API Versioning below).

**Use `ToCreatedAtActionResult`** for POST endpoints that create resources — returns `201 Created` with `Location` header. When the GET action is on a **different controller**, pass the controller name (class name minus `Controller` suffix):
```csharp
// Different controller — specify controller name explicitly
result.ToCreatedAtActionResult(this, nameof(CustomersController.GetCustomer), o => new { id = o.Id.Value }, CustomerDto.From, "Customers");
```

### Automatic Scalar Value Binding

🔴 **Use value object types — not primitives — in controller action parameters.** Trellis automatically converts route parameters, query parameters, and JSON body properties via model binding and JSON converters. Never call `.Create()` or `.TryCreate()` manually in controllers for **scalar** value objects — the binding layer handles them.

🟡 **Composite value objects** (e.g., `ShippingAddress` with multiple fields) are NOT auto-bound. Construct them in the controller using `TryCreate` → `BindAsync` → `Send`:
```csharp
// ✅ Composite VO — construct in controller, chain into command
return await ShippingAddress.TryCreate(
        request.ShippingAddress.Street,
        request.ShippingAddress.City,
        request.ShippingAddress.State,
        request.ShippingAddress.PostalCode,
        request.ShippingAddress.Country)
    .BindAsync(address => _sender.Send(
        new CreateCustomerCommand(request.FirstName, request.LastName, request.Email, request.PhoneNumber, address),
        cancellationToken))
    .ToCreatedAtActionResultAsync(this, nameof(GetCustomer), c => new { id = c.Id }, CustomerResponse.From);
```
This keeps commands receiving **valid domain types** (not raw strings) and the controller stays in the ROP pipeline. The `ShippingAddress` validation errors automatically flow through as Problem Details responses.

🟡 **Service Level Indicator (SLI) attributes** — from the `ServiceLevelIndicators` package. Applied to controller action parameters:
- `[CustomerResourceId]` — identifies the customer, customer group, or calling service
- `[LocationId]` — the location where the service is running (e.g., Public cloud, West US 3 region, Azure Core)
- `[Operation]` — the name of the operation

See the [ServiceLevelIndicators documentation](https://github.com/xavierjohn/ServiceLevelIndicators) for details.

**Registration** — add scalar value validation to the MVC pipeline in `Api/src/DependencyInjection.cs`:
```csharp
services.AddControllers().AddScalarValueValidation();
```
And activate the middleware in `Program.cs`:
```csharp
app.UseScalarValueValidation();
```

🟡 **Request/Response DTOs** live in `Api/src/{version}/Models/` (e.g., `Api/src/2026-03-26/Models/`). 🔴 Never expose domain types directly. Request DTOs can use scalar value object types and `Money` as properties — they will be validated automatically via JSON converters. Response DTOs should have a `static From(DomainType)` method for mapping from domain aggregates.
```csharp
// ✅ All Trellis value objects work directly in request DTOs
public record CreateProductRequest
{
    public ProductName Name { get; init; } = null!;    // scalar VO — validated by ParsableJsonConverter
    public Sku Sku { get; init; } = null!;             // scalar VO
    public Money UnitPrice { get; init; } = null!;     // structured VO — validated by MoneyJsonConverter
}
// JSON: { "name": "Widget", "sku": "WGT-001", "unitPrice": { "amount": 9.99, "currency": "USD" } }
```

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

🟡 **TRLS001 (`Result not handled`) in test setup code** — when calling methods like `todo.Start()` or `repo.SaveAsync()` in test setup where the result is intentionally ignored, suppress with `#pragma warning disable TRLS001` at the top of the file, or discard with `_ =`:
```csharp
#pragma warning disable TRLS001 // Result intentionally ignored in test setup

// Or per-line:
_ = todo.Start();
```

🔴 **`Maybe<T>` assertions** — use `.Should().HaveValue()` and `.Should().BeNone()` (from `Trellis.Testing`) to assert on `Maybe<T>` values. Do NOT use `.HasValue.Should().BeTrue()` or `.HasNoValue.Should().BeTrue()` — these bypass Trellis.Testing's assertion messages. Also available: `.Should().HaveValueEqualTo(expected)` and `.Should().HaveValueMatching(predicate)`.
```csharp
// ✅ Correct — Trellis.Testing assertions
customer.PhoneNumber.Should().HaveValue();
customer.PhoneNumber.Should().BeNone();
order.SubmittedAt.Should().HaveValue();
order.SubmittedAt.Should().HaveValueMatching(d => d >= expectedTime && d <= expectedTime.AddSeconds(1));

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

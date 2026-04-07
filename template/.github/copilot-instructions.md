# Copilot Instructions — Building with Trellis

This template builds ASP.NET Core services on the Trellis framework for .NET 10.

## 🔴 Before Writing Code — Read the API References

**STOP. Do not write or generate any code until you have read the API reference files listed below.** These files document the exact method signatures, overloads, conventions, and EF Core mapping rules. Guessing based on type names will produce code that compiles but fails at runtime (e.g., adding explicit EF `Property()` configuration on types that Trellis conventions already handle).

Read **every** file relevant to your implementation. For a typical service using aggregates, EF Core, and authorization, that means reading at least: `trellis-api-results.md`, `trellis-api-ddd.md`, `trellis-api-primitives.md`, `trellis-api-efcore.md`, `trellis-api-asp.md`, `trellis-api-authorization.md`, `trellis-api-stateless.md`, and `trellis-api-testing-reference.md`.

| When working on... | Read first |
|---|---|
| `Result<T>`, `Maybe<T>`, `Error`, `Bind`, `Map`, `Tap`, `Ensure`, `Combine`, `ParallelAsync` | `.github/trellis-api-results.md` |
| Aggregates, entities, value objects, specifications, ETag checks | `.github/trellis-api-ddd.md` |
| `RequiredString<T>`, `RequiredGuid<T>`, `RequiredEnum<T>`, built-in primitives | `.github/trellis-api-primitives.md` |
| MVC/Minimal API result mappers, `ETagHelper`, scalar binding, validation middleware | `.github/trellis-api-asp.md` |
| EF Core conventions, interceptors, `HasTrellisIndex`, `FirstOrDefaultMaybeAsync` | `.github/trellis-api-efcore.md` |
| Actor-based authorization, `IAuthorize`, resource authorization | `.github/trellis-api-authorization.md` |
| FluentValidation bridge | `.github/trellis-api-fluentvalidation.md` |
| `HttpClient` result extensions | `.github/trellis-api-http.md` |
| Mediator pipeline behaviors | `.github/trellis-api-mediator.md` |
| `LazyStateMachine<TState, TTrigger>` and `FireResult()` | `.github/trellis-api-stateless.md` |
| Testing helpers, `FakeRepository`, `TestActorProvider`, assertions, `Unwrap()` | `.github/trellis-api-testing-reference.md` |
| Analyzer diagnostics `TRLS001`–`TRLS022` and generator diagnostics | `.github/trellis-api-analyzers.md` |
| Supported cross-package patterns and known sample-app-only APIs | `.github/trellis-api-patterns.md` |
| Scalar vs composite value-object classification | `.github/trellis-value-object-taxonomy.md` |

## Critical Rules

### Study the template reference implementation first

- **Rule:** 🔴 MUST read the Todo sample before replacing it.
- **Rationale:** The shipped sample demonstrates the exact Trellis patterns this template expects.
- **Correct:** Use the reference implementation table below and inspect the listed files before generating your own service.
- **Incorrect:** Recreate the solution structure and patterns from scratch without checking the working sample.
- **Reference:** See `template/Domain/src/`, `template/Application/src/`, `template/Acl/src/`, `template/Api/src/`.

### Treat errors and optional values as explicit types

- **Rule:** 🔴 MUST use `Result<T>` for expected failures and `Maybe<T>` for optional values. Never throw for business logic. Never use `try/catch` in Domain or Application layers for expected outcomes.
- **Rationale:** Trellis relies on Railway Oriented Programming; exceptions for expected paths break the pipeline and reduce testability.
- **Correct:**
```csharp
using Trellis;

public static Result<Order> TryCreate(OrderName name) =>
    string.IsNullOrWhiteSpace(name.Value)
        ? Result.Failure<Order>(Error.Validation("Name is required.", "name"))
        : Result.Success(new Order(name));

public partial class Customer : Aggregate<CustomerId>
{
    public partial Maybe<PhoneNumber> PhoneNumber { get; private set; }
}
```
- **Incorrect:**
```csharp
using Trellis;

public static Order Create(string name)
{
    if (string.IsNullOrWhiteSpace(name))
        throw new InvalidOperationException("Name is required.");

    return new Order(name);
}

public sealed class Customer
{
    public PhoneNumber? PhoneNumber { get; private set; }
}
```
- **Reference:** See `.github/trellis-api-results.md`, `.github/trellis-api-ddd.md`, `.github/trellis-api-efcore.md`.

### Eliminate primitive obsession on domain surfaces

- **Rule:** 🔴 MUST expose value objects on aggregates, entities, commands, and public domain methods. Do not expose raw `Guid`, `string`, `int`, or `decimal` for domain concepts.
- **Rationale:** Trellis models validity at the type level; primitive-based domain APIs reintroduce invalid states.
- **Correct:**
```csharp
using Trellis;

public sealed record UpdateTodoCommand(TodoId TodoId, Title Title, DueDate DueDate);

public partial class Order : Aggregate<OrderId>
{
    public OrderStatus Status { get; private set; } = null!;
    public CustomerId CustomerId { get; private set; } = null!;
}
```
- **Incorrect:**
```csharp
public sealed record UpdateTodoCommand(Guid TodoId, string Title, DateTime DueDate);

public sealed class Order
{
    public string Status { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
}
```
- **Reference:** See `.github/trellis-api-primitives.md`, `.github/trellis-value-object-taxonomy.md`.

### Use `RequiredEnum<T>` for all domain enum-like concepts

- **Rule:** 🔴 MUST model domain enums as `RequiredEnum<T>` partial classes, not C# `enum`.
- **Rationale:** `RequiredEnum<T>` gives validation, JSON conversion, EF Core conversion, LINQ support, and attachable behavior.
- **Correct:**
```csharp
using Trellis;

public partial class OrderStatus : RequiredEnum<OrderStatus>
{
    public static readonly OrderStatus Draft = new();
    public static readonly OrderStatus Confirmed = new();
    public static readonly OrderStatus Shipped = new();
    public static readonly OrderStatus Cancelled = new();
}

public partial class PaymentMethod : RequiredEnum<PaymentMethod>
{
    [EnumValue("credit-card")]
    public static readonly PaymentMethod CreditCard = new();

    [EnumValue("bank-transfer")]
    public static readonly PaymentMethod BankTransfer = new();

    public static readonly PaymentMethod Cash = new();
}

public partial class FulfillmentStatus : RequiredEnum<FulfillmentStatus>
{
    public static readonly FulfillmentStatus Draft = new(canModify: true, isTerminal: false);
    public static readonly FulfillmentStatus Confirmed = new(canModify: false, isTerminal: false);
    public static readonly FulfillmentStatus Cancelled = new(canModify: false, isTerminal: true);

    public bool CanModify { get; }
    public bool IsTerminal { get; }

    private FulfillmentStatus(bool canModify, bool isTerminal)
    {
        CanModify = canModify;
        IsTerminal = isTerminal;
    }
}
```
- **Incorrect:**
```csharp
public enum OrderStatus
{
    Draft,
    Confirmed,
    Shipped,
    Cancelled
}
```
- **Reference:** See `.github/trellis-api-primitives.md §RequiredEnum<TSelf>` and `.github/trellis-api-efcore.md §ModelConfigurationBuilderExtensions`.
### Make commands always-valid and time-testable

- **Rule:** 🔴 MUST make commands receive value objects, use a private constructor plus `TryCreate` when cross-field validation exists, and use `TimeProvider` instead of `DateTime.UtcNow` or `DateTimeOffset.UtcNow`.
- **Rationale:** Command validity belongs at construction time, and time-dependent rules must remain testable.
- **Correct:**
```csharp
using Mediator;
using Trellis;
using Trellis.Authorization;

public sealed record UpdateTodoCommand : ICommand<Result<TodoItem>>, IAuthorize
{
    public TodoId TodoId { get; }
    public Title Title { get; }
    public DueDate DueDate { get; }

    private UpdateTodoCommand(TodoId todoId, Title title, DueDate dueDate)
    {
        TodoId = todoId;
        Title = title;
        DueDate = dueDate;
    }

    public static Result<UpdateTodoCommand> TryCreate(
        TodoId todoId,
        Title title,
        DueDate dueDate,
        TimeProvider? timeProvider = null) =>
        Result.Ensure(
                dueDate > (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime,
                Error.Validation("Due date must be in the future.", "dueDate"))
            .Map(_ => new UpdateTodoCommand(todoId, title, dueDate));
}

public Result<Order> Approve(TimeProvider timeProvider) =>
    _machine.FireResult(Triggers.Approve)
        .Tap(order => DomainEvents.Add(new OrderApprovedEvent(Id, OccurredAt: timeProvider.GetUtcNow().UtcDateTime)))
        .Map(_ => this);
```
- **Incorrect:**
```csharp
using Mediator;

public sealed record UpdateTodoCommand(Guid TodoId, string Title, DateTime DueDate) : ICommand<Result<TodoItem>>;

public Result<Order> Approve() =>
    _machine.FireResult(Triggers.Approve)
        .Tap(_ => DomainEvents.Add(new OrderApprovedEvent(Id, OccurredAt: DateTime.UtcNow)))
        .Map(_ => this);
```
- **Reference:** See `.github/trellis-api-results.md`, `.github/trellis-api-ddd.md`, `.github/trellis-api-patterns.md`.

### Build layer-by-layer and compile between layers

- **Rule:** 🔴 MUST implement Domain → Application → Acl → Api → Tests, running `dotnet build` between layers and `dotnet test` after tests are added.
- **Rationale:** Trellis uses source generators for `partial Maybe<T>` properties and Mediator code; later layers depend on generated output from earlier builds.
- **Correct:**
```text
1. Domain/src      -> dotnet build
2. Application/src -> dotnet build
3. Acl/src         -> dotnet build
4. Api/src         -> dotnet build
5. Tests           -> dotnet test
```
- **Incorrect:** Create all files across all projects first, then attempt a single build after generated code is already required by downstream layers.
- **Reference:** See the `## Implementation Order and Build Checkpoints` section below.

### Return `Maybe<T>` from repository lookups

- **Rule:** 🔴 MUST return `Maybe<T>` from repository lookups and convert to `Result<T>` in handlers with `.ToResult(Error.NotFound(...))`.
- **Rationale:** Absence is data, not failure; handlers own the domain meaning of “not found”.
- **Correct:**
```csharp
using Trellis;

public interface ITodoRepository
{
    Task<Maybe<TodoItem>> FindByIdAsync(TodoId id, CancellationToken cancellationToken);
}

public async ValueTask<Result<TodoItem>> Handle(GetTodoByIdQuery query, CancellationToken cancellationToken)
{
    var maybe = await _repository.FindByIdAsync(query.TodoId, cancellationToken);
    return maybe.ToResult(Error.NotFound("Todo not found.", query.TodoId));
}
```
- **Incorrect:**
```csharp
using Trellis;

public interface ITodoRepository
{
    Task<Result<TodoItem>> FindByIdAsync(TodoId id, CancellationToken cancellationToken);
}
```
- **Reference:** See `.github/trellis-api-results.md`, `.github/trellis-api-efcore.md §QueryableExtensions`.

### Keep handlers on the ROP track

- **Rule:** 🔴 MUST compose handler flows with `Bind`, `BindAsync`, `CheckAsync`, `Map`, and related result combinators. Do not unwrap and branch imperatively unless branching materially improves readability.
- **Rationale:** ROP chains preserve failure propagation and keep success paths explicit.
- **Correct:**
```csharp
using Trellis;

public async ValueTask<Result<Order>> Handle(SubmitOrderCommand command, CancellationToken cancellationToken) =>
    await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken)
        .BindAsync(order => order.Submit())
        .BindAsync(order => _orderRepository.SaveAsync(order, cancellationToken).MapAsync(_ => order));
```
- **Incorrect:**
```csharp
public async ValueTask<Result<Order>> Handle(SubmitOrderCommand command, CancellationToken cancellationToken)
{
    var result = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
    if (result.IsFailure)
        return result.Error;

    var order = result.Value;
    var submitResult = order.Submit();
    if (submitResult.IsFailure)
        return submitResult.Error;

    await _orderRepository.SaveAsync(order, cancellationToken);
    return order;
}
```
- **Reference:** See `.github/trellis-api-results.md`, `.github/trellis-api-patterns.md`, `.github/trellis-api-mediator.md`.

### Use `LazyStateMachine<TState, TTrigger>` in aggregates

- **Rule:** 🔴 MUST use `LazyStateMachine<TState, TTrigger>` instead of constructing `StateMachine<TState, TTrigger>` eagerly inside persisted aggregates.
- **Rationale:** EF Core materializes aggregates before state properties are populated; eager state-machine initialization can throw `NullReferenceException`.
- **Correct:**
```csharp
using Stateless;
using Trellis.Stateless;

private readonly LazyStateMachine<OrderStatus, string> _machine;

private Order() : base(default!)
{
    _machine = new LazyStateMachine<OrderStatus, string>(
        () => Status,
        state => Status = state,
        ConfigureStateMachine);
}

private static void ConfigureStateMachine(StateMachine<OrderStatus, string> machine)
{
    // Configure transitions here.
}

public Result<OrderStatus> Submit() => _machine.FireResult("Submit");
```
- **Incorrect:**
```csharp
using Stateless;

private readonly StateMachine<OrderStatus, string> _machine;

private Order() : base(default!)
{
    _machine = new StateMachine<OrderStatus, string>(() => Status, state => Status = state);
}
```
- **Reference:** See `.github/trellis-api-stateless.md §LazyStateMachine<TState, TTrigger>` and `.github/trellis-api-stateless.md §StateMachineExtensions`.
### Follow Trellis EF Core conventions exactly

- **Rule:** 🔴 MUST use `ApplyTrellisConventions`, `AddTrellisInterceptors`, `SaveChangesResultUnitAsync`, `partial Maybe<T>` properties, `HasTrellisIndex`, and EF materialization boilerplate exactly as Trellis expects.
- **Rationale:** Trellis persistence relies on conventions and generators; overriding them with manual EF patterns silently breaks mapping, timestamps, or generated backing fields.
- **Correct:**
```csharp
using Microsoft.EntityFrameworkCore;
using Trellis.EntityFrameworkCore;

public class Customer : Aggregate<CustomerId>
{
    public FirstName FirstName { get; private set; } = null!;
    public LastName LastName { get; private set; } = null!;
    public EmailAddress Email { get; private set; } = null!;
    public ShippingAddress ShippingAddress { get; private set; } = null!;
    public partial Maybe<PhoneNumber> Phone { get; set; }

    private Customer() : base(default!) { }
}

protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
    configurationBuilder.ApplyTrellisConventions(typeof(Customer).Assembly);

protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
    optionsBuilder.AddTrellisInterceptors();

builder.HasTrellisIndex(x => new { x.Name, x.SubmittedAt });

return await _context.SaveChangesResultUnitAsync(cancellationToken);
```
- **Incorrect:**
```csharp
using Microsoft.EntityFrameworkCore;

protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
}

builder.Property(x => x.Status).HasConversion<string>();
builder.OwnsOne(x => x.Money);
builder.HasIndex(x => new { x.Status, x.SubmittedAt });

return await _context.SaveChangesAsync(cancellationToken);
```
- **Reference:** See `.github/trellis-api-efcore.md §DbContextOptionsBuilderExtensions`, `.github/trellis-api-efcore.md §ModelConfigurationBuilderExtensions`, `.github/trellis-api-efcore.md §DbContextExtensions`, `.github/trellis-api-efcore.md §MaybeEntityTypeBuilderExtensions`.

### Keep controllers thin and value-object-first

- **Rule:** 🔴 MUST accept scalar value-object parameters directly in controllers, map domain results to DTOs in controllers, and add XML doc comments to all public API types and members.
- **Rationale:** Scalar binding and HTTP mapping are presentation concerns; handlers should stay domain-focused, and missing XML docs break builds with CS1591.
- **Correct:**
```csharp
using Mediator;
using Microsoft.AspNetCore.Mvc;
using TodoSample.Api.v2026_03_26.Models;
using TodoSample.Application.Todos;
using TodoSample.Domain;
using Trellis.Asp;

[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[Route("api/[controller]")]
public class TodosController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Constructor.
    /// </summary>
    public TodosController(ISender sender) => _sender = sender;

    /// <summary>
    /// Get a todo item by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async ValueTask<ActionResult<TodoResponse>> GetById(TodoId id, CancellationToken cancellationToken) =>
        await _sender.Send(new GetTodoByIdQuery(id), cancellationToken)
            .ToActionResultAsync(
                this,
                todo => RepresentationMetadata.WithStrongETag(todo.ETag),
                TodoResponse.From);
}
```
- **Incorrect:**
```csharp
[HttpGet("{id}")]
public async Task<TodoItem> GetById(Guid id, CancellationToken cancellationToken)
{
    var todoId = TodoId.Create(id);
    return (await _sender.Send(new GetTodoByIdQuery(todoId), cancellationToken)).Value;
}
```
- **Reference:** See `.github/trellis-api-asp.md §ActionResultExtensions`, `.github/trellis-api-asp.md §ActionResultExtensionsAsync`, `.github/trellis-api-asp.md §ServiceCollectionExtensions`.

### Read the testing reference before writing tests

- **Rule:** 🔴 MUST read `.github/trellis-api-testing-reference.md` before writing tests, and use Trellis testing assertions for `Result<T>` and `Maybe<T>`.
- **Rationale:** The testing package already provides assertions, fake repositories, actor providers, and safe unwrapping patterns expected by this template.
- **Correct:**
```csharp
using Trellis.Testing;

result.Should().BeSuccess();
var order = result.Unwrap();
customer.PhoneNumber.Should().HaveValue();
customer.AlternatePhoneNumber.Should().BeNone();
```
- **Incorrect:**
```csharp
result.Value.Should().NotBeNull();
customer.PhoneNumber.HasValue.Should().BeTrue();
customer.AlternatePhoneNumber.HasNoValue.Should().BeTrue();
```
- **Reference:** See `.github/trellis-api-testing-reference.md §Usage notes`, `.github/trellis-api-testing-reference.md §UnwrapExtensions`.

## Decision Tables

### Modeling decisions

| Scenario | Use | Not |
|---|---|---|
| Expected business failure | `Result<T>` | Exceptions for normal flow |
| Optional value object | `Maybe<T>` | `T?` |
| Optional entity navigation | `T?` | `Maybe<T>` |
| Domain enum-like concept | `RequiredEnum<T>` | C# `enum` |
| Scalar domain concept | `RequiredString<T>`, `RequiredGuid<T>`, `RequiredInt<T>`, `RequiredDecimal<T>`, `RequiredDateTime<T>`, built-in `Trellis.Primitives` | Raw primitives on domain surfaces |
| Reusable domain concept in two contexts | Separate value-object types | One shared primitive and comments |
| Single-currency money | `MonetaryAmount` | `Money` |
| Multi-currency money | `Money` | `MonetaryAmount` |
| Composite value object | `ValueObject` + `Result.Combine(...)` + `GetEqualityComponents()` | Scalar-shaped wrapper with fake `Value` |
| Optional composite value object in EF Core | `partial Maybe<T>` | Manual nullable owned-type plumbing |

### Validation and authorization decisions

| Scenario | Use | Not |
|---|---|---|
| Cross-field command validation | Private constructor + `TryCreate(...)` | Mutable command + later validation |
| Validation that cannot happen in `TryCreate` | `IValidate.Validate()` returning `IResult` | Late handler-only validation |
| Permission-based authorization | `IAuthorize` | Handler-side permission `if` statements |
| Resource-based authorization | `IAuthorizeResource<TResource>` + loader | Handler-side ownership checks |
| Shared loader by ID | `SharedResourceLoaderById<TResource, TId>` + `IIdentifyResource<TResource, TId>` | Repeating per-command loader code |
| Complex per-command load logic | `ResourceLoaderById<TMessage, TResource, TId>` | Overfitting a shared loader |
| Optional `If-Match` handling | `.OptionalETag(expectedETags)` | Manual ETag comparison |
| Required `If-Match` handling | `.RequireETag(expectedETags)` | Ad hoc 428/412 logic |

### Handler and controller decisions

| Scenario | Use | Not |
|---|---|---|
| Straight-through handler flow | `Bind` / `BindAsync` / `CheckAsync` / `Map` | Imperative unwrapping |
| Complex branching where chaining harms readability | Short explicit branching that still returns `Result<T>` | Deep nested `if` blocks everywhere |
| Two or more independent async fetches | `Result.ParallelAsync(...).WhenAllAsync()` | Sequential awaits |
| Save that returns `Result<Unit>` | `BindAsync` or `CheckAsync` | `TapAsync` when the save can fail |
| DTO mapping | Controller result mappers | Handler returns DTOs |
| POST create response | `ToCreatedAtActionResult(...)` / `ToCreatedAtActionResultAsync(...)` | `Ok(...)` |
| PUT/PATCH response with `Prefer` and ETag | `ToUpdatedActionResultAsync(...)` | Manual status-code branching |
| Scalar route/query/body binding | Accept Trellis value objects directly | `TryCreate` every scalar in controllers |
| Composite VO request binding | Build with `TryCreate(...).BindAsync(...)` in the controller | Primitive command properties |

### EF Core and query decisions

| Scenario | Use | Not |
|---|---|---|
| Conventions | `ApplyTrellisConventions(...)` | Manual `HasConversion()` / `OwnsOne()` for Trellis-supported types |
| Interceptors | `AddTrellisInterceptors()` | Reimplement timestamp or ETag plumbing |
| Save changes in repositories | `SaveChangesResultUnitAsync()` | Bare `SaveChangesAsync()` |
| Optional lookup | `FirstOrDefaultMaybeAsync(...)` | `FirstOrDefaultAsync(...)` + `null` |
| Required lookup | `FirstOrDefaultResultAsync(..., Error.NotFound(...))` | Returning `null` or throwing |
| `Maybe<T>` comparisons in LINQ | `WhereLessThan`, `WhereHasValue`, `WhereEquals`, etc. | Direct `Value` access in LINQ |
| Index containing `Maybe<T>` | `HasTrellisIndex(...)` | `HasIndex(...)` |
| Entity configuration placement | `IEntityTypeConfiguration<T>` in Acl | Inline `OnModelCreating` configuration |
## Reference Implementation

Study these files before replacing the Todo sample.

| Pattern | Files |
|---|---|
| Scalar value objects with `RequiredGuid`, `RequiredString`, `RequiredDateTime`, `ValidateAdditional` | `template/Domain/src/ValueObjects/` |
| `RequiredEnum` smart enum | `template/Domain/src/TodoStatus.cs` |
| Aggregate with `LazyStateMachine` and `Maybe<T>` partial properties | `template/Domain/src/Aggregates/TodoItem.cs` |
| Specification with `.And()` composition | `template/Domain/src/Specifications/OverdueTodoSpecification.cs` |
| Always-valid command with private constructor + `TryCreate` | `template/Application/src/Todos/UpdateTodoCommand.cs` |
| `Result.Ensure` authorization check | `template/Application/src/Todos/CompleteTodoCommand.cs` |
| `IAuthorizeResource<T>` with `SharedResourceLoaderById` or `ResourceLoaderById` | `template/Application/src/Todos/CompleteTodoCommand.cs`, `template/Acl/src/CompleteTodoResourceLoader.cs` |
| Repository returning `Maybe<T>` | `template/Application/src/Todos/ITodoRepository.cs` |
| Handlers returning domain types and controller DTO mapping | `template/Application/src/Todos/`, `template/Api/src/2026-03-26/Models/TodoResponse.cs` |
| `TimeProvider` for testable time validation | `template/Application/src/Todos/UpdateTodoCommand.cs` |
| Controller `TryCreate` → `BindAsync` → `Send` flow | `template/Api/src/2026-03-26/Controllers/TodosController.cs` |
| Domain, Application, and API tests | `template/Domain/tests/`, `template/Application/tests/`, `template/Api/tests/` |

## Architecture and Layout

### Layer dependency matrix

- **Rule:** 🟡 SHOULD keep dependencies flowing inward only.
- **Rationale:** Trellis expects domain purity, application orchestration, Acl persistence adapters, and API presentation boundaries.
- **Correct:**

| Layer | Can depend on | Cannot depend on | Contains |
|---|---|---|---|
| Domain | Trellis packages only (`Results`, `Primitives`, `DDD`, `Stateless`, `Authorization`) | EF Core, ASP.NET Core, Mediator | Aggregates, entities, value objects, domain events, specifications, permission constants |
| Application | Domain, Mediator, `Trellis.Mediator` | ASP.NET Core, EF Core providers | Commands, queries, handlers, repository interfaces |
| Acl | Application, `Trellis.EntityFrameworkCore`, EF Core provider | API types | `DbContext`, entity configurations, repository implementations, migrations, resource loaders |
| Api | Application, Acl, `Trellis.Asp` | Domain persistence implementation details | Controllers/endpoints, DTOs, `Program.cs`, `IActorProvider` |

- **Incorrect:** Let Domain reference EF Core or ASP.NET Core, place repository implementations in Application, or return DTOs from handlers.
- **Reference:** See `.github/trellis-api-ddd.md`, `.github/trellis-api-asp.md`, `.github/trellis-api-efcore.md`.

> **Why “Acl”?** ACL stands for Anti-Corruption Layer. It adapts external systems (SQL Server, message queues, other services) to the domain model and avoids overloading the word “Infrastructure”.

### Composition root and registration rules

- **Rule:** 🔴 MUST keep repository interfaces in Application, implementations in Acl, one `DependencyInjection.cs` per layer, `IActorProvider` as singleton in Api, and `TimeProvider.System` as a singleton in Application.
- **Rationale:** Trellis pipeline behaviors are singleton-based, and ASP.NET Core does not auto-register `TimeProvider`.
- **Correct:**
```csharp
using Microsoft.Extensions.DependencyInjection;

services.AddSingleton(TimeProvider.System);
services.AddCachingActorProvider<HttpActorProvider>();
```
- **Incorrect:**
```csharp
services.AddScoped<IActorProvider, HttpActorProvider>();
```
- **Reference:** See `.github/trellis-api-authorization.md`, `.github/trellis-api-patterns.md`, `.github/trellis-api-asp.md`.

> **`CachingActorProvider`:** When you need synchronous actor access after the async pipeline resolves it, use `AddCachingActorProvider<T>()`. It caches the actor per request in `HttpContext.Items` and prevents a singleton pipeline from depending on a scoped provider.

### Project layout

- **Rule:** 🟡 SHOULD preserve the template structure and only add code where the template expects it.
- **Rationale:** The solution, package management, and test props are already preconfigured.
- **Correct:**
```text
{ServiceName}/
├── {ServiceName}.slnx
├── Directory.Build.props          ← DO NOT MODIFY
├── Directory.Packages.props       ← ADD new packages here (versions only)
├── global.json                    ← DO NOT MODIFY
├── build/
│   └── test.props                 ← DO NOT MODIFY
├── .github/
│   ├── copilot-instructions.md
│   ├── trellis-api-results.md
│   ├── trellis-api-asp.md
│   ├── trellis-api-ddd.md
│   ├── trellis-api-primitives.md
│   ├── trellis-api-efcore.md
│   ├── trellis-api-mediator.md
│   ├── trellis-api-authorization.md
│   ├── trellis-api-http.md
│   ├── trellis-api-stateless.md
│   ├── trellis-api-fluentvalidation.md
│   ├── trellis-api-analyzers.md
│   ├── trellis-api-patterns.md
│   ├── trellis-api-testing-reference.md
│   └── trellis-value-object-taxonomy.md
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
- **Incorrect:** Recreate `Directory.Build.props`, put package versions in `.csproj`, or create alternative folder conventions that bypass the template.
- **Reference:** See the template tree under `template/`.

> **NuGet packages:** Add `<PackageVersion>` to `Directory.Packages.props`, then add `<PackageReference>` without a version in the relevant `.csproj`.

### HTTP request documentation files

- **Rule:** 🟡 SHOULD replace `Api/src/api.http` with end-to-end requests for every endpoint and keep complex header values in `Api/src/http-client.env.json` as escaped JSON strings.
- **Rationale:** The `.http` file is living API documentation, and the HTTP client only supports scalar variable substitution.
- **Correct:**
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
- **Incorrect:** Put nested JSON directly in `.http` variables or let `host` drift from `Properties/launchSettings.json`.
- **Reference:** See `template/Api/src/api.http`, `template/Api/src/http-client.env.json`, and `template/Api/src/Properties/launchSettings.json`.

## Implementation Order and Build Checkpoints

### Build between layers

- **Rule:** 🔴 MUST build after each layer because generated code appears only after compilation.
- **Rationale:** The `MaybePartialPropertyGenerator` emits `_camelCase` backing fields used later by EF Core configuration and query helpers.
- **Correct:**
```text
1. Domain/src — implement value objects, aggregates, entities, events, specifications, permissions. Then run dotnet build.
2. Application/src — implement repository interfaces, commands, queries, handlers. Then run dotnet build.
3. Acl/src — implement DbContext, entity configurations, repositories, resource loaders. Then run dotnet build.
4. Api/src — implement controllers, DTOs, Program.cs, IActorProvider. Then run dotnet build.
5. Tests — implement Domain.Tests, Application.Tests, Api.Tests. Then run dotnet test.
```
- **Incorrect:** Depend on `_submittedAt` or generated mediator code before the earlier projects have been built once.
- **Reference:** See `.github/trellis-api-efcore.md` and `.github/trellis-api-mediator.md`.
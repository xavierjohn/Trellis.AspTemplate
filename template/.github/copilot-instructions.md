# Copilot Instructions — Building with Trellis

This template builds ASP.NET Core services on the Trellis framework for .NET 10.

## 🔴 Before Writing Code — Read the API References

**STOP. Do not write or generate any code until you have read the API reference files listed below.** These files document the exact method signatures, overloads, conventions, and EF Core mapping rules. Guessing based on type names will produce code that compiles but fails at runtime (e.g., adding explicit EF `Property()` configuration on types that Trellis conventions already handle).

For a typical service using aggregates, EF Core, controllers, and authorization, read at least: `trellis-api-core.md`, `trellis-api-primitives.md`, `trellis-api-efcore.md`, `trellis-api-asp.md`, `trellis-api-asp-apiversioning.md`, `trellis-api-authorization.md`, `trellis-api-mediator.md`, `trellis-api-statemachine.md`, `trellis-api-servicedefaults.md`, `trellis-api-cookbook.md`, and `trellis-api-testing-reference.md`. Skim `trellis-api-analyzers.md` to know which compile-time rules apply to your code.

### Trellis package catalog

Every Trellis NuGet package ships an AI-targeted reference markdown. Each package is **scoped** — only some belong on each layer (`Trellis.Asp` is API-only, `Trellis.Core` is everywhere, `Trellis.EntityFrameworkCore` is Acl-only, etc.). Use the table to discover the surface; read the linked file before calling into it.

| Package | What it provides | Reference |
|---|---|---|
| `Trellis.Core` | `Result<T>`, `Maybe<T>`, `Error` types, ROP combinators (`Bind` / `Map` / `Tap` / `Ensure` / `Combine` / `ParallelAsync` / `Match`), aggregates (`Aggregate<TId>`), entities (`Entity<TId>`), value-object base types, specifications, `EntityTagValue` / `OptionalETag` / `RequireETag`, domain events. The base abstraction every other Trellis package builds on. | `.github/trellis-api-core.md` |
| `Trellis.Primitives` | Built-in scalar value-object base classes — `RequiredString<T>`, `RequiredGuid<T>`, `RequiredInt<T>`, `RequiredDecimal<T>`, `RequiredLong<T>`, `RequiredBool<T>`, `RequiredDateTime<T>`, `RequiredEnum<T>` — plus shipped concrete scalars like `Money`, `EmailAddress`, `PhoneNumber`. Use these instead of raw primitives for any domain identifier or measured value. | `.github/trellis-api-primitives.md` |
| `Trellis.Mediator` | Mediator pipeline behaviors (exception, tracing, logging, authorization, resource authorization, validation), `AddTrellisBehaviors`, `AddResourceAuthorization` (explicit + assembly-scanning overloads). The Application layer's request/response pipeline. | `.github/trellis-api-mediator.md` |
| `Trellis.Authorization` | Actor abstractions, `IAuthorize` (static permissions), `IAuthorizeResource<TResource>` (resource-bound auth), `IIdentifyResource<TResource, TId>` + `SharedResourceLoaderById<TResource, TId>` (one loader per resource type). | `.github/trellis-api-authorization.md` |
| `Trellis.FluentValidation` | FluentValidation bridge: `AddTrellisFluentValidation` registers `FluentValidationMessageValidatorAdapter<TMessage>` so FluentValidation rules participate in the Mediator `ValidationBehavior` pipeline. | `.github/trellis-api-fluentvalidation.md` |
| `Trellis.StateMachine` | `LazyStateMachine<TState, TTrigger>` over Stateless, `FireResult` returning `Result<TState>`. Use for aggregate state transitions; never put business mutations inside Stateless configuration. | `.github/trellis-api-statemachine.md` |
| `Trellis.EntityFrameworkCore` | EF Core integration: `MaybeConvention`, `MoneyConvention`, `CompositeValueObjectConvention`, `ApplyTrellisConventions(...)` / source-generated `ApplyTrellisConventionsFor<TContext>()`, `AddTrellisInterceptors`, `RepositoryBase<T,TId>` (`Add` / `Remove` / `RemoveByIdAsync`), `HasTrellisIndex`, `FirstOrDefaultMaybeAsync`, `SaveChangesResultAsync`, `AddTrellisUnitOfWork<TContext>()` (`TransactionalCommandBehavior`). Acl layer only. | `.github/trellis-api-efcore.md` |
| `Trellis.Http` | Typed `HttpClient` result extensions: `SendResultAsync`, `ReadJsonAsync`, status-mapping helpers. For consuming external HTTP APIs from inside the service. | `.github/trellis-api-http.md` |
| `Trellis.Asp` | ASP.NET Core integration: `result.ToHttpResponse(...)` / `ToHttpResponseAsync` / `AsActionResult<T>`, `HttpResponseOptionsBuilder<T>` (`WithETag`, `WithLastModified`, `Vary`, `Created` / `CreatedAtRoute` / `CreatedAtAction`, `EvaluatePreconditions`, `HonorPrefer`, `WithRange`, `WithErrorMapping`, `WithRouteValueResolver`), actor providers (`ClaimsActorProvider`, `EntraActorProvider`, `DevelopmentActorProvider`, `CachingActorProvider`), scalar value validation middleware. Api layer only. | `.github/trellis-api-asp.md` |
| `Trellis.Asp.ApiVersioning` | API-version-aware `Location` headers: `CreatedAtVersionedRoute(...)` extensions on `HttpResponseOptionsBuilder<T>` that auto-inject `?api-version=…` per request. Use **instead of** `CreatedAtRoute` whenever the controller carries `[ApiVersion]`. Handles `[ApiVersionNeutral]`, URL-segment versioning, and multi-version actions. | `.github/trellis-api-asp-apiversioning.md` |
| `Trellis.ServiceDefaults` | Composition root: `services.AddTrellis(...)` and `TrellisServiceBuilder` module ordering. `AddTrellisUnitOfWork<TContext>()` must run **after** all other behavior registrations so it lands innermost. | `.github/trellis-api-servicedefaults.md` |
| `Trellis.ServiceLevelIndicators` | SLI emission for console/worker/library code with no ASP.NET dependency. The base `ServiceLevelIndicator` API. | `.github/trellis-api-sli.md` |
| `Trellis.ServiceLevelIndicators.Asp` | ASP.NET Core SLI middleware: `[ServiceLevelIndicator]` attribute, `app.UseServiceLevelIndicator()`, customer-resource-id tagging, request enrichment. | `.github/trellis-api-sli-asp.md` |
| `Trellis.ServiceLevelIndicators.Asp.ApiVersioning` | Adds the `http.api.version` SLI dimension when `Asp.Versioning` is configured. Tags `Neutral` for `[ApiVersionNeutral]` endpoints, `Unspecified` for endpoints with no version metadata, the version string otherwise. | `.github/trellis-api-sli-apiversioning.md` |
| `Trellis.Testing` | `FakeRepository<T,TId>`, `TestActorProvider`, `Result<T>` / `Maybe<T>` assertions, `Unwrap()` test helpers (test-only; never use in production). | `.github/trellis-api-testing-reference.md` |
| `Trellis.Testing.AspNetCore` | `WebApplicationFactory` extensions, `CreateClientWithActor`, MSAL/Entra token plumbing, `.http` replay helpers. For end-to-end MVC/Minimal API integration tests. | `.github/trellis-api-testing-aspnetcore.md` |
| `Trellis.Analyzers` | Roslyn analyzers `TRLS001`–`TRLS023` and code fixes; generator diagnostics `TRLS031`–`TRLS039`. Surfaces compile-time mistakes (unsafe `Maybe.Value`, sentinel `default(Result)`, missing api-version on `CreatedAtRoute`, etc.). | `.github/trellis-api-analyzers.md` |
| **Cross-cutting docs (not packages)** | | |
| Cookbook | Cross-package end-to-end recipes spanning DDD, Mediator, FluentValidation, EF Core, ASP.NET Core, authorization, state machine, testing. Patterns Index at the top of the file. | `.github/trellis-api-cookbook.md` |
| Anti-pattern gallery | Ready-to-apply WRONG/FIX shapes for each `TRLSxxx` analyzer trigger. Read first when debugging an analyzer warning; preserve the control-flow shape and adapt identifiers, types, and error values to the caller. | `.github/trellis-api-anti-patterns.md` |
| Value-object taxonomy | Scalar vs composite value-object classification rules — when to use `IScalarValue<T,TPrimitive>` vs `[OwnedEntity]`. | `.github/trellis-value-object-taxonomy.md` |

### Preflight verification — required before generating non-trivial code

Reading the references is necessary but not sufficient. Before producing any non-trivial Trellis code, **explicitly answer these in your reasoning** (one or two lines is enough, but skipping the step is not allowed):

1. **Which task am I doing?** Name the task in the cookbook's task-lookup table — verbatim if possible.
2. **Which recipe applies?** Cite the recipe number (e.g. *"Recipe 1 — CRUD aggregate"* or *"Recipe 21 — Parallel independent loads"*). If no recipe applies, name the cookbook section or package reference that does.
3. **Which inherited surface does my type already get?** For any type derived from `Aggregate<TId>`, `Entity<TId>`, `RequiredGuid<T>`, `RequiredString<T>`, `RequiredEnum<T>`, the scalar `Required*<T>` primitives (`RequiredInt<T>`, `RequiredLong<T>`, `RequiredDecimal<T>`, `RequiredBool<T>`, `RequiredDateTime<T>`), `ValueObject`, or `ScalarValueObject<TSelf, T>`, list the inherited members you will *not* redeclare. Recipe 1 in the cookbook enumerates the standard set for `RequiredGuid<T>`, `RequiredString<T>`, `ValueObject`, and `Aggregate<TId>` and points at the compiled `Recipe1InheritedSurface` demonstrator in the framework's `Examples/CookbookSnippets/Recipe01_CrudAggregate.cs` for byte-exact signatures. The most common Recipe 1 mistake is redeclaring `Id`, equality methods, or `TryCreate` that the base class already provides.
4. **Am I about to invent an API?** If you cannot point at a specific reference file + line range for the method/extension/attribute you are about to use, stop and load that reference. Do not synthesize the signature from prior knowledge.
5. **What does the analyzer say?** If the change is in a `Result`/`Maybe`/EF-Core/value-object pipeline, list which `TRLSxxx` IDs are relevant. Cite the matching section in `.github/trellis-api-anti-patterns.md` if one exists; otherwise cite `.github/trellis-api-analyzers.md` and the relevant package reference. Preserve the WRONG/FIX control-flow shape from the anti-pattern file, adapting identifiers, types, and error values to the caller — the snippets are pattern examples, not self-contained replacements.

If you cannot answer any of these, stop and load the missing reference before continuing.

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
        ? Result.Fail<Order>(Error.UnprocessableContent.ForField("name", "required", "Name is required."))
        : Result.Ok(new Order(name));

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
- **Reference:** See `.github/trellis-api-core.md`, `.github/trellis-api-efcore.md`.

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
                Error.UnprocessableContent.ForField("dueDate", "out_of_range", "Due date must be in the future."))
            .Map(() => new UpdateTodoCommand(todoId, title, dueDate));
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
- **Reference:** See `.github/trellis-api-core.md`.

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

- **Rule:** 🔴 MUST return `Maybe<T>` from repository lookups and convert to `Result<T>` in handlers with `.ToResult(new Error.NotFound(new ResourceRef(...)))`.
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
    return maybe.ToResult(new Error.NotFound(new ResourceRef("Todo", query.TodoId.ToString(System.Globalization.CultureInfo.InvariantCulture))) { Detail = $"Todo {query.TodoId} not found." });
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
- **Reference:** See `.github/trellis-api-core.md`, `.github/trellis-api-efcore.md §QueryableExtensions`.

### Keep handlers on the ROP track

- **Rule:** 🔴 MUST compose handler flows with `Bind`, `BindAsync`, `CheckAsync`, `Map`, and related result combinators. Do not unwrap and branch imperatively unless branching materially improves readability.
- **Rule:** 🔴 MUST stage persistence with `repo.Add(...)` / `repo.Remove(...)` and let `TransactionalCommandBehavior` (registered by `AddTrellisUnitOfWork<TContext>()`) commit on handler success. Never call a `repo.SaveAsync(...)` from a production handler — `SaveAsync` is the `FakeRepository` test convenience and is **not** part of `RepositoryBase<T,TId>`. Mutations to a tracked aggregate (e.g. `todo.Update(...)`, `order.Submit()`) require **no** explicit save call: EF tracks the change, the unit-of-work commits.
- **Rationale:** ROP chains preserve failure propagation and keep success paths explicit. The unit-of-work pattern guarantees a single commit per command and lets `FakeRepository<T,TId>` shape-match your custom interface directly (no test adapter for the save path).
- **Correct (insert):**
```csharp
using Trellis;

public async ValueTask<Result<Order>> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
{
    var maybeActor = await _actorProvider.GetCurrentActorAsync(cancellationToken);
    if (!maybeActor.TryGetValue(out var actor))
        return Result.Fail<Order>(new Error.Unauthorized() { Detail = "No authenticated actor." });

    return Order.TryCreate(command.CustomerId, command.LineItems, actor.Id, _timeProvider)
        .Tap(_repository.Add); // stage; UoW commits on handler success
}
```
- **Correct (mutate existing):**
```csharp
public async ValueTask<Result<Order>> Handle(SubmitOrderCommand command, CancellationToken cancellationToken)
{
    var maybe = await _orderRepository.FindByIdAsync(command.OrderId, cancellationToken);
    return maybe
        .ToResult(new Error.NotFound(new ResourceRef("Order", command.OrderId.ToString(System.Globalization.CultureInfo.InvariantCulture))))
        .Bind(order => order.Submit(_timeProvider).Map(_ => order));
        // No save call: the loaded aggregate is tracked; UoW commits on handler success.
}
```
- **Correct (delete):**
```csharp
public ValueTask<Result<Unit>> Handle(DeleteOrderCommand command, CancellationToken cancellationToken) =>
    new(_orderRepository.RemoveByIdAsync(command.OrderId, cancellationToken));
```
- **Incorrect (calls `SaveAsync` from a production handler — anti-pattern):**
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

    await _orderRepository.SaveAsync(order, cancellationToken); // ❌ — IRepository.SaveAsync should not exist
    return order;
}
```
- **Reference:** See `.github/trellis-api-core.md`, `.github/trellis-api-mediator.md`, `.github/trellis-api-cookbook.md` Recipe 16 (handler → `Add` → `TransactionalCommandBehavior`).

> **Multi-aggregate orchestration example.** When a single command needs to mutate two or more aggregates, the handler loads each aggregate via its repository, calls a method on each that mutates only that aggregate, and stages any inserts. Each domain method stays inside its own aggregate's transactional consistency boundary; the handler — which lives in the Application layer, *inside* the bounded context — owns the cross-aggregate coordination. `TransactionalCommandBehavior` commits both aggregates in one EF `SaveChangesAsync`.
>
> ```csharp
> using Trellis;
>
> public async ValueTask<Result<Order>> Handle(ReturnOrderCommand command, CancellationToken cancellationToken) =>
>     await Result.ParallelAsync(
>             _orderRepository.FindByIdAsync(command.OrderId, cancellationToken)
>                 .ToResultAsync(new Error.NotFound(new ResourceRef("Order", command.OrderId.ToString(System.Globalization.CultureInfo.InvariantCulture)))),
>             _productRepository.GetByIdsAsync(command.ProductIds, cancellationToken))
>         .WhenAllAsync()
>         .BindAsync((order, products) =>
>             order.Return(command.Reason, _timeProvider)               // Order mutates only Order state + ReturnedAt
>                 .Bind(_ => products.Traverse(p => p.ReleaseStock(...)))  // Each Product mutates only its own stock
>                 .Map(_ => order));
>         // No SaveAsync calls: both aggregates are tracked; UoW commits on handler success.
> ```
>
> **Anti-pattern:** `order.Return(IEnumerable<Product> products, ...)` that calls `p.ReleaseStock()` inside `Order.Return`. This makes Order know about Product invariants, conflates two aggregates' transactional scopes, and forces consumers to mock Product to unit-test Order. **Anti-pattern:** the same orchestration in a controller or middleware — that leaks a domain concern outside the bounded context.

### Use `LazyStateMachine<TState, TTrigger>` in aggregates

- **Rule:** 🔴 MUST use `LazyStateMachine<TState, TTrigger>` instead of constructing `StateMachine<TState, TTrigger>` eagerly inside persisted aggregates.
- **Rationale:** EF Core materializes aggregates before state properties are populated; eager state-machine initialization can throw `NullReferenceException`.
- **Correct:**
```csharp
using Stateless;
using Trellis.StateMachine;

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
- **Reference:** See `.github/trellis-api-statemachine.md §LazyStateMachine<TState, TTrigger>` and `.github/trellis-api-statemachine.md §StateMachineExtensions`.
### Follow Trellis EF Core conventions exactly

- **Rule:** 🔴 MUST use `ApplyTrellisConventions`, `AddTrellisInterceptors`, `AddTrellisUnitOfWork<TContext>()`, `partial Maybe<T>` properties, `HasTrellisIndex`, and EF materialization boilerplate exactly as Trellis expects.
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

services.AddTrellisUnitOfWork<AppDbContext>();
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

### Use attribute-based API versioning

- **Rule:** 🔴 MUST declare API versions with `[ApiVersion("yyyy-MM-dd")]` attributes on controller classes. Do NOT register `VersionByNamespaceConvention`. Do NOT create per-version namespaces or folders as a mechanism for version assignment (folder organization for readability is fine; the namespace must not be load-bearing).
- **Rationale:** Attribute-based versioning lets a single controller class serve multiple versions concurrently — when a feature adds v2 fields or a v2-only endpoint, you add `[ApiVersion("2026-12-01")]` alongside the existing `[ApiVersion("2026-11-12")]` on the same class and use `[MapToApiVersion("2026-12-01")]` per action where the behavior differs. `VersionByNamespaceConvention` forces full controller duplication into a v2 namespace for any feature that needs cross-version reuse, multiplies the surface to maintain, and is the source of subtle bugs (route conflicts, ETag plumbing duplicated per copy, OpenAPI documents that disagree between versions).
- **Correct:**
```csharp
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[ApiVersion("2026-11-12")]
[ApiVersion("2026-12-01")]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    // v1 + v2 share this action (same shape both versions).
    [HttpGet("{id}", Name = "Orders_GetById")]
    public Task<ActionResult<OrderResponse>> GetById(OrderId id, CancellationToken ct) =>
        _sender.Send(new GetOrderByIdQuery(id), ct)
            .AsTask()
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();

    // v2-only endpoint. Returns 404 under ?api-version=2026-11-12 automatically.
    [HttpPost("{id}/return")]
    [MapToApiVersion("2026-12-01")]
    public Task<ActionResult<OrderResponse>> Return(
        OrderId id,
        [FromBody] ReturnOrderRequest request,
        CancellationToken ct) =>
        _sender.Send(new ReturnOrderCommand(id, request.Reason), ct)
            .AsTask()
            .ToHttpResponseAsync(OrderResponse.From)
            .AsActionResultAsync<OrderResponse>();
}

// In DependencyInjection.cs:
services.AddApiVersioning(options =>
        {
            // 404 (not 400) when a client requests an endpoint at a version where it doesn't exist.
            // Semantically correct: the syntax is valid; the resource doesn't exist at this version.
            options.UnsupportedApiVersionStatusCode = StatusCodes.Status404NotFound;
        })
        .AddMvc()                                 // ✅ no VersionByNamespaceConvention
        .AddApiExplorer()
        .AddOpenApi(o => o.Document.AddScalarTransformers());
```
- **Incorrect:**
```csharp
// ❌ Per-version namespace duplication.
namespace MyService.Api.v2026_11_12.Controllers;
public class OrdersController : ControllerBase { /* v1 actions */ }

namespace MyService.Api.v2026_12_01.Controllers;
public class OrdersController : ControllerBase { /* identical v2 actions + Return */ }

// ❌ DI registers namespace convention.
services.AddApiVersioning()
        .AddMvc(options => options.Conventions.Add(new VersionByNamespaceConvention()));

// ❌ Explicit v1 stub action returning 404 — let the framework versioning interceptor
//    do this so route-param validation can't mask the 404 with a 422.
[HttpPost("{id}/return")]
public IActionResult ReturnV1Stub(OrderId id) => NotFound();

// ❌ v2-only action without [MapToApiVersion] — silently exposes the action under v1.
// On a controller with multiple [ApiVersion] attributes, an action without
// [MapToApiVersion] is reachable under EVERY declared version. Always mark
// version-specific actions explicitly:
[ApiController]
[ApiVersion("2026-11-12")]
[ApiVersion("2026-12-01")]
public class OrdersController : ControllerBase
{
    [HttpPost("{id}/return")]                     // ❌ reachable under v1 AND v2
    public Task<ActionResult<OrderResponse>> Return(...) => ...;

    [HttpPost("{id}/return")]
    [MapToApiVersion("2026-12-01")]               // ✅ v2 only
    public Task<ActionResult<OrderResponse>> Return(...) => ...;
}
```
- **Reference:** See `.github/trellis-api-asp-apiversioning.md` for the `CreatedAtVersionedRoute` helpers that compose with this pattern, and `template/Api/src/2026-03-26/Controllers/TodosController.cs` for the reference implementation (single-version sample; multi-version is the same shape with additional `[ApiVersion]` attributes on the class).

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
    [HttpGet("{id}", Name = "Todos_GetById")]
    public Task<ActionResult<TodoResponse>> GetById(TodoId id, CancellationToken cancellationToken) =>
        _sender.Send(new GetTodoByIdQuery(id), cancellationToken)
            .AsTask()
            .ToHttpResponseAsync(
                TodoResponse.From,
                opts => opts.WithETag(t => EntityTagValue.Strong(t.ETag)).EvaluatePreconditions())
            .AsActionResultAsync<TodoResponse>();

    /// <summary>
    /// Create a todo item (JSON body).
    /// </summary>
    [HttpPost]
    [Consumes("application/json")]
    public Task<ActionResult<TodoResponse>> Create(
        [FromBody] CreateTodoRequest request, CancellationToken cancellationToken) => /* ... */;

    /// <summary>
    /// Complete a todo (bodyless transition — id is in the URL).
    /// </summary>
    [HttpPost("{id}/completion")]
    public Task<ActionResult<TodoResponse>> Complete(
        TodoId id, CancellationToken cancellationToken) =>
        _sender.Send(new CompleteTodoCommand(id), cancellationToken)
            .AsTask()
            .ToHttpResponseAsync(TodoResponse.From)
            .AsActionResultAsync<TodoResponse>();
}
```
- **Incorrect:**
```csharp
[HttpGet("{id}")]
public async Task<TodoItem> GetById(Guid id, CancellationToken cancellationToken)
{
    var todoId = TodoId.Create(id);
    var result = await _sender.Send(new GetTodoByIdQuery(todoId), cancellationToken);
    return result.TryGetValue(out var todo) ? todo : throw new InvalidOperationException();
}
```
- **Reference:** See `.github/trellis-api-asp.md §HttpResponseExtensions`, `.github/trellis-api-asp.md §ActionResultAdapterExtensions`, `.github/trellis-api-asp.md §ServiceCollectionExtensions`.

### Place `[Consumes("application/json")]` on body-taking actions, never on the controller

- **Rule:** 🔴 MUST attach `[Consumes("application/json")]` to individual actions that read a request body (create, add-child, update). MUST NOT put it at the controller class level when the controller mixes body POSTs with bodyless transition POSTs (`{id}/submission`, `{id}/approval`, `{id}/completion`, etc.).
- **Rationale:** A class-level `[Consumes]` is enforced by MVC against **every** action on the controller. A bodyless transition POST has no `Content-Type` header (and no body to declare one for), so MVC rejects it with **415 Unsupported Media Type** before the action runs. The caller has done nothing wrong — RFC 9110 §8.3 does not require `Content-Type` on requests with no body. The 415 is a self-inflicted misconfiguration that the action would otherwise handle correctly.
- **Correct:** see the body POST + bodyless transition pair in the previous rule's example. `[Consumes("application/json")]` annotates `Create` (body) and is absent on `Complete` (bodyless).
- **Incorrect:**
```csharp
[ApiController]
[Route("api/[controller]")]
[Consumes("application/json")]                       // ❌ Applied to every action, including bodyless transitions
public class OrdersController : ControllerBase
{
    [HttpPost]
    public Task<ActionResult<OrderResponse>> CreateDraft([FromBody] CreateDraftOrderRequest request, ...) => ...;

    [HttpPost("{id}/submission")]                    // ❌ Now returns 415 unless the caller sends an empty `{}` JSON body
    public Task<ActionResult<OrderResponse>> Submit(OrderId id, ...) => ...;
}
```
- **Symptom to watch for:** transition endpoints returning 415 in integration tests or `api.http` flows, "fixed" by sending an empty `{}` body with `Content-Type: application/json`. That is a workaround, not a fix — the right fix is removing the class-level `[Consumes]`.
- **Reference:** See `.github/trellis-api-asp.md §HttpResponseExtensions`. RFC 9110 §8.3 (`Content-Type` is required only when a representation is enclosed).

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
| **Mutation spans multiple aggregates** | **Orchestrate in the application-layer command handler; each aggregate exposes a method that mutates only itself** | **(A) An aggregate method that takes other aggregates and mutates them; (B) Cross-aggregate orchestration in API controllers or middleware.** Rationale: aggregate = transactional consistency boundary; cross-aggregate coordination is a domain concern executed at the application orchestration tier (which is inside the bounded context). |
| Save that returns non-generic `Result` | `BindAsync` or `CheckAsync` | `TapAsync` when the save can fail |
| DTO mapping | Controller result mappers | Handler returns DTOs |
| POST create response | `ToHttpResponseAsync(body, opts => opts.CreatedAtRoute(...))` | `Ok(...)` |
| PUT/PATCH response with `Prefer` and ETag | `ToHttpResponseAsync(body, opts => opts.WithETag(...).EvaluatePreconditions())` | Manual status-code branching |
| Scalar route/query/body binding | Accept Trellis value objects directly | `TryCreate` every scalar in controllers |
| Composite VO request binding | Build with `TryCreate(...).BindAsync(...)` in the controller | Primitive command properties |

### EF Core and query decisions

| Scenario | Use | Not |
|---|---|---|
| Conventions | `ApplyTrellisConventions(...)` | Manual `HasConversion()` / `OwnsOne()` for Trellis-supported types |
| Interceptors | `AddTrellisInterceptors()` | Reimplement timestamp or ETag plumbing |
| Commit EF changes | `AddTrellisUnitOfWork<TContext>()`; repositories stage changes | Calling `SaveChangesAsync()` from handlers |
| Optional lookup | `FirstOrDefaultMaybeAsync(...)` | `FirstOrDefaultAsync(...)` + `null` |
| Required lookup | `FirstOrDefaultResultAsync(..., new Error.NotFound(new ResourceRef(...)))` | Returning `null` or throwing |
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
| `IAuthorizeResource<T>` with `IIdentifyResource<TResource, TId>` + `SharedResourceLoaderById<TResource, TId>` | `template/Application/src/Todos/CompleteTodoCommand.cs`, `template/Acl/src/TodoItemResourceLoader.cs` |
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
| Application | Domain, Mediator, `Trellis.Mediator` | ASP.NET Core, EF Core providers | Commands, queries, handlers, repository interfaces, **cross-aggregate orchestration** |
| Acl | Application, `Trellis.EntityFrameworkCore`, EF Core provider | API types | `DbContext`, entity configurations, repository implementations, migrations, resource loaders |
| Api | Application, Acl, `Trellis.Asp` | Domain persistence implementation details | Controllers/endpoints, DTOs, `Program.cs`, `IActorProvider` |

- **Incorrect:** Let Domain reference EF Core or ASP.NET Core, place repository implementations in Application, return DTOs from handlers, or place cross-aggregate orchestration in API controllers/middleware.
- **Reference:** See `.github/trellis-api-core.md`, `.github/trellis-api-asp.md`, `.github/trellis-api-efcore.md`.

> **Why “Acl”?** ACL stands for Anti-Corruption Layer. It adapts external systems (SQL Server, message queues, other services) to the domain model and avoids overloading the word “Infrastructure”.

> **Domain library boundary.** Domain.dll **and** Application.dll together constitute the bounded context — i.e., the domain library. The Application layer is the *orchestration tier of the domain*, not infrastructure. This is why cross-aggregate orchestration belongs in command handlers, not in API controllers, middleware, or aggregate methods that take other aggregates as parameters. See "Handler and controller decisions" → "Mutation spans multiple aggregates" for the canonical pattern.

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
- **Reference:** See `.github/trellis-api-authorization.md`, `.github/trellis-api-core.md`, `.github/trellis-api-asp.md`.

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
│   ├── trellis-api-core.md
│   ├── trellis-api-asp.md
│   ├── trellis-api-asp-apiversioning.md
│   ├── trellis-api-primitives.md
│   ├── trellis-api-efcore.md
│   ├── trellis-api-mediator.md
│   ├── trellis-api-authorization.md
│   ├── trellis-api-http.md
│   ├── trellis-api-statemachine.md
│   ├── trellis-api-fluentvalidation.md
│   ├── trellis-api-analyzers.md
│   ├── trellis-api-cookbook.md
│   ├── trellis-api-servicedefaults.md
│   ├── trellis-api-sli.md
│   ├── trellis-api-sli-asp.md
│   ├── trellis-api-sli-apiversioning.md
│   ├── trellis-api-testing-reference.md
│   ├── trellis-api-testing-aspnetcore.md
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

> **Upgrading Trellis packages:** After changing `TrellisVersion` in `Directory.Packages.props`, run `dotnet build ./{ServiceName}.slnx /t:TrellisSyncApiReference` from the service repository root to update the `.github/trellis-api-*.md` reference files from the new package versions.

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

## Running Tests

This template is configured for `dotnet test` in **Microsoft.Testing.Platform (MTP) native mode** on .NET 10. The wiring is in two places: `global.json` declares `"test": { "runner": "Microsoft.Testing.Platform" }`, and every test csproj inherits `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>` via `build/test.props`. Both are required; neither file should be edited.

### Canonical invocations

- **Whole solution:** `dotnet test` from the repository root. Discovers and runs every test csproj.
- **Single project:** `dotnet test --project Domain/tests/Domain.Tests.csproj` (or any other test csproj).
- **Single test:** `dotnet test --project Domain/tests/Domain.Tests.csproj --filter-method "*Submit_with_*"` (MTP filter syntax — note `--filter-method`, not the VSTest-era `--filter`).

All three run through MTP. A successful run ends with a block like:

```
Test run summary: Passed!
  total: N
  failed: 0
  succeeded: N
  skipped: 0
```

### "Zero tests ran" — diagnosis

If `dotnet test` reports `Zero tests ran` or exits with code 5 despite test methods existing, the run did **not** activate MTP-native mode and fell back to VSTest. Check, in order:

1. The current working directory is at or below the one containing `global.json` (run `cat global.json` to confirm). Without `global.json`, `dotnet test` defaults to VSTest, which doesn't see MTP-built test executables.
2. The SDK on PATH is .NET 10.0.100 or later (`dotnet --version`). Earlier SDKs ignore the `"test": { "runner": ... }` schema.
3. The csproj actually inherits `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>` (check that `build/test.props` is being imported — it's wired in via `Directory.Build.props`).

### Don't use VSTest-era arguments

The following are VSTest-only and silently no-op under MTP. Do not paste them from older Trellis docs or Stack Overflow answers:

- `--logger:trx`, `--logger:console;verbosity=detailed` — under MTP use `--report-trx` and `--output Detailed`.
- `--filter "FullyQualifiedName~Foo"` — under MTP use `--filter-method "*Foo*"`, `--filter-class "*FooTests"`, or `--filter-namespace "Foo.*"`.
- `--collect "Code Coverage"` — MTP collects via `Microsoft.Testing.Extensions.CodeCoverage` (already referenced by the template); pass `--coverage` to enable.

If unsure whether you're in MTP mode, run `dotnet test --help` from the test project's directory — the MTP help text starts with `Microsoft(R) Testing Platform Execution Command Line Tool`. The VSTest help text starts with `MSTest Platform`.
# Copilot Instructions — Building with Trellis

This template builds ASP.NET Core services on the Trellis framework for .NET 10.

## 🔴 Before Writing Code — Read the API References

**STOP. Do not write or generate any code until you have read the API reference files listed below.** These files document the exact method signatures, overloads, conventions, and EF Core mapping rules. Guessing based on type names will produce code that compiles but fails at runtime (e.g., adding explicit EF `Property()` configuration on types that Trellis conventions already handle).

Read **every** file relevant to your implementation. For a typical service using aggregates, EF Core, and authorization, that means reading at least: `trellis-api-core.md`, `trellis-api-primitives.md`, `trellis-api-efcore.md`, `trellis-api-asp.md`, `trellis-api-authorization.md`, `trellis-api-statemachine.md`, `trellis-api-cookbook.md`, and `trellis-api-testing-reference.md`.

**Reference docs are authoritative.** If anything in this file conflicts with one of the `trellis-api-*.md` reference files, the reference file wins — those files are auto-synced from package metadata (`dotnet build /t:TrellisSyncApiReference`) and reflect the current framework surface. This file is curated guidance that can drift. Please file any contradiction as feedback.

| When working on... | Read first |
|---|---|
| `Result<T>`, `Maybe<T>`, `Error`, `Bind`, `Map`, `Tap`, `Ensure`, `Combine`, `ParallelAsync` | `.github/trellis-api-core.md` |
| Aggregates, entities, value objects, specifications, ETag checks | `.github/trellis-api-core.md` |
| `RequiredString<T>`, `RequiredGuid<T>`, `RequiredEnum<T>`, built-in primitives | `.github/trellis-api-primitives.md` |
| MVC/Minimal API result mappers, `ETagHelper`, scalar binding, validation middleware | `.github/trellis-api-asp.md` |
| EF Core conventions, interceptors, `HasTrellisIndex`, `FirstOrDefaultMaybeAsync` | `.github/trellis-api-efcore.md` |
| Actor-based authorization, `IAuthorize`, resource authorization | `.github/trellis-api-authorization.md` |
| FluentValidation bridge: `AddTrellisFluentValidation` DI registration + pipeline adapter | `.github/trellis-api-mediator-fluentvalidation.md` |
| FluentValidation bridge: low-level `IResult` converters + JSON-pointer normalization | `.github/trellis-api-fluentvalidation.md` |
| `HttpClient` result extensions | `.github/trellis-api-http.md` |
| Mediator pipeline behaviors | `.github/trellis-api-mediator.md` |
| `LazyStateMachine<TState, TTrigger>` and `FireResult()` | `.github/trellis-api-statemachine.md` |
| Testing helpers, `FakeRepository`, `TestActorProvider`, assertions, `Unwrap()` | `.github/trellis-api-testing-reference.md` |
| Analyzer diagnostics `TRLS001`–`TRLS022` and generator diagnostics | `.github/trellis-api-analyzers.md` |
| Cross-package patterns, recipes, and task lookup table | `.github/trellis-api-cookbook.md` |
| Scalar vs composite value-object classification | `.github/trellis-value-object-taxonomy.md` |

## Critical Rules

### Study the template reference implementation first

- **Rule:** 🔴 MUST read the Todo sample before replacing it.
- **Rationale:** The shipped sample demonstrates the exact Trellis patterns this template expects.
- **Correct:** Use the reference implementation table below and inspect the listed files before generating your own service.
- **Incorrect:** Recreate the solution structure and patterns from scratch without checking the working sample.
- **Reference:** See `Domain/src/`, `Application/src/`, `Acl/src/`, `Api/src/`.

### Treat errors and optional values as explicit types

- **Rule:** 🔴 MUST use `Result<T>` for expected failures and `Maybe<T>` for optional values. Never throw for business logic. Never use `try/catch` in Domain or Application layers for expected outcomes.
- **Rationale:** Trellis relies on Railway Oriented Programming; exceptions for expected paths break the pipeline and reduce testability.
- **Exceptions are still for the _exceptional_.** The rule is "never throw for an **expected** outcome" (validation, not-found, conflict, forbidden, optional absence — model these as `Result<T>` / `Maybe<T>`), **not** "never throw at all". `throw` remains correct for unrecoverable faults that signal a bug or broken environment: API misuse, failed startup/configuration checks, and infrastructure errors. For internal "shouldn't happen" faults you may also return the value `Error.Unexpected(reasonCode, faultId?)` instead of throwing. Analyzer **TRLS010** enforces the no-throw rule inside Result chains (`Bind`/`Map`/`Tap`/`Ensure`).
- **Correct:**
```csharp
using Trellis;

public static Result<Order> TryCreate(OrderName name) =>
    string.IsNullOrWhiteSpace(name.Value)
        ? Result.Fail<Order>(Error.InvalidInput.ForField("name", "required", "Name is required."))
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
                Error.InvalidInput.ForField("dueDate", "out_of_range", "Due date must be in the future."))
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
- **Reference:** See `.github/trellis-api-core.md`, `.github/trellis-api-cookbook.md`.

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

### Run `dotnet test` without legacy VSTest arguments

- **Rule:** 🔴 MUST NOT pass legacy VSTest arguments such as `--nologo`, `--logger`, `-l`, or `--results-directory` to `dotnet test`. Test projects in this template use xUnit v3 + Microsoft.Testing.Platform (MTP), which forwards unknown arguments to the test host and exits with code 5 plus `Zero tests ran` when it sees a flag it doesn't recognize — easily misread as a test failure.
- **Rationale:** MTP does not share a CLI surface with the legacy VSTest runner. `Unknown option '--nologo'` followed by `Zero tests ran` and `Exit code: 5` is the diagnostic signature of this mistake.
- **Correct:**
```powershell
dotnet test                                     # all defaults
dotnet test --no-build                          # skip rebuild
dotnet test --filter-not-trait "Category=Integration"
dotnet test --coverage --report-trx
```
- **Incorrect:**
```powershell
dotnet test --nologo                            # Unknown option '--nologo' -> exit code 5
dotnet test --logger trx                        # Unknown option '--logger' -> exit code 5
dotnet test -l "console;verbosity=minimal"      # rejected by MTP runner
```
- **Reference:** `runtests.cmd` (at the repo root) and `.github/workflows/build.yml` show the CI invocation. For the full MTP option list, run any compiled test exe directly: `./bin/Debug/net10.0/*.Tests.exe --help`.

### Return `Maybe<T>` from repository lookups

- **Rule:** 🔴 MUST return `Maybe<T>` from repository lookups and convert to `Result<T>` in handlers with `.ToResult(new Error.NotFound(...))`.
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
    return maybe.ToResult(new Error.NotFound(ResourceRef.For("Todo", query.TodoId)) { Detail = "Todo not found." });
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
- **Reference:** See `.github/trellis-api-core.md`, `.github/trellis-api-cookbook.md`, `.github/trellis-api-mediator.md`.

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

- **Rule:** 🔴 MUST use `ApplyTrellisConventionsFor<TContext>()`, `AddTrellisInterceptors`, `SaveChangesResultUnitAsync`, `partial Maybe<T>` properties, `HasTrellisIndex`, and EF materialization boilerplate exactly as Trellis expects.
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
    configurationBuilder.ApplyTrellisConventionsFor<AppDbContext>();

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

- **Rule:** 🔴 MUST accept scalar value-object parameters directly in controllers, map domain results to DTOs in controllers, place `[Consumes("application/json")]` per-action on body-bearing endpoints only (never at the class level), and add XML doc comments to all public API types and members.
- **Rationale:** Scalar binding and HTTP mapping are presentation concerns; handlers should stay domain-focused, and missing XML docs break builds with CS1591. Class-level `[Consumes("application/json")]` causes `415 Unsupported Media Type` on body-less POSTs such as state-transition triggers (`/orders/{id}/submission`, `/complete`, `/cancel`), because the request has no `Content-Type` header.
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
    [HttpGet("{id}")]
    public async ValueTask<ActionResult<TodoResponse>> GetById(TodoId id, CancellationToken cancellationToken) =>
        await _sender.Send(new GetTodoByIdQuery(id), cancellationToken)
            .ToActionResultAsync(
                this,
                todo => RepresentationMetadata.WithStrongETag(todo.ETag),
                TodoResponse.From);

    /// <summary>
    /// Create a new todo item.
    /// </summary>
    [HttpPost]
    [Consumes("application/json")]
    public async ValueTask<IActionResult> Create([FromBody] CreateTodoRequest request, CancellationToken cancellationToken) =>
        await _sender.Send(new CreateTodoCommand(request.Title), cancellationToken)
            .ToCreatedAtActionResultAsync(this, nameof(GetById), id => new { id }, TodoResponse.From);
}
```
- **Incorrect:**
```csharp
// ❌ Class-level [Consumes] returns 415 on body-less POSTs (state-transition triggers).
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[Route("api/[controller]")]
public class TodosController : ControllerBase { /* ... */ }

// ❌ Raw primitives in controller signatures, no DTO mapping, no XML docs.
[HttpGet("{id}")]
public async Task<TodoItem> GetById(Guid id, CancellationToken cancellationToken)
{
    var todoId = TodoId.Create(id);
    return (await _sender.Send(new GetTodoByIdQuery(todoId), cancellationToken)).Value;
}
```
- **Reference:** See `.github/trellis-api-asp.md §Endpoint checklist for generated APIs` for the `[Consumes]` placement rule, `.github/trellis-api-asp.md §ActionResultExtensions`, `.github/trellis-api-asp.md §ActionResultExtensionsAsync`, `.github/trellis-api-asp.md §ServiceCollectionExtensions`.

### Require `If-Match` on body-overwriting mutations; omit it on guarded state-transition POSTs

- **Rule:** 🔴 MUST wire `If-Match` precondition checking on every endpoint whose body can silently overwrite a concurrent write — `PUT`, `PATCH`, `DELETE`, body-carrying mutating `POST` endpoints, and non-commutative additive set operations. The controller parses `ETagHelper.ParseIfMatch(Request)`, the command carries `EntityTagValue[]? IfMatchETags`, and the handler chain includes `.RequireETag(command.IfMatchETags)` between the `NotFound` projection and the mutation. Use `.OptionalETag(...)` only for genuinely idempotent best-effort updates — never as a default.
- **Rule:** 🟡 SHOULD NOT wire `If-Match` on **body-less state-transition `POST`** endpoints (e.g., `POST /orders/{id}/approve`, `.../submit`, `.../cancel`, `.../return`). The state machine + transition guards already check the current state, so a stale client calling `.../approve` on an order that has already shipped gets `422 Unprocessable Content` from the guard — there is nothing to overwrite. Adding `RequireETag` here is ceremony without benefit. Wire it only if the user-provided spec explicitly requires `412`/`428` on transitions.
- **Rationale:** Skipping the precondition on body-overwriting mutations lets concurrent clients silently overwrite each other (lost-update race). On body-less guarded transitions there is no body to overwrite — the state machine is the precondition. The full decision table (full-update PUT, partial PATCH, DELETE, additive set ops, resource creation) lives in `.github/trellis-api-cookbook.md` Recipe 23.
- **Correct (body-carrying PUT — `RequireETag`):**
```csharp
// Application/src/Todos/UpdateTodoCommand.cs
public sealed class UpdateTodoCommand : ICommand<Result<Todo>>
{
    public UpdateTodoCommand(TodoId id, Title title, EntityTagValue[]? ifMatchETags = null)
    {
        Id = id;
        Title = title;
        IfMatchETags = ifMatchETags;
    }

    public TodoId Id { get; }
    public Title Title { get; }
    public EntityTagValue[]? IfMatchETags { get; }
}

internal sealed class UpdateTodoCommandHandler(ITodoRepository repository)
    : ICommandHandler<UpdateTodoCommand, Result<Todo>>
{
    public Task<Result<Todo>> Handle(UpdateTodoCommand command, CancellationToken cancellationToken) =>
        repository.FindByIdAsync(command.Id, cancellationToken)
            .ToResult(Error.NotFound.ForResource(nameof(Todo), command.Id.Value.ToString()))
            .RequireETag(command.IfMatchETags)
            .Bind(todo => todo.Rename(command.Title))
            .Tap(repository.Update)
            .Bind(_ => repository.SaveChangesResultUnitAsync(cancellationToken).Map(_ => _));
}

// Api/src/{version}/Controllers/TodosController.cs
[HttpPut("{id:guid}")]
[Consumes("application/json")]
[ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
[ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
public Task<IActionResult> Update(TodoId id, [FromBody] UpdateTodoRequest body, CancellationToken cancellationToken)
{
    var ifMatchETags = ETagHelper.ParseIfMatch(Request);
    return _sender.Send(new UpdateTodoCommand(id, body.Title, ifMatchETags), cancellationToken)
        .ToActionResultAsync(this, t => RepresentationMetadata.WithStrongETag(t.ETag), TodoResponse.From);
}
```
- **Correct (body-less state-transition POST — no `If-Match`):**
```csharp
// Application/src/Todos/CompleteTodoCommand.cs
public sealed record CompleteTodoCommand(TodoId Id) : ICommand<Result<Todo>>;

internal sealed class CompleteTodoCommandHandler(ITodoRepository repository)
    : ICommandHandler<CompleteTodoCommand, Result<Todo>>
{
    public Task<Result<Todo>> Handle(CompleteTodoCommand command, CancellationToken cancellationToken) =>
        repository.FindByIdAsync(command.Id, cancellationToken)
            .ToResult(Error.NotFound.ForResource(nameof(Todo), command.Id.Value.ToString()))
            .Bind(todo => todo.Complete(DateTime.UtcNow))  // state machine guards the transition
            .Tap(repository.Update)
            .Bind(_ => repository.SaveChangesResultUnitAsync(cancellationToken).Map(_ => _));
}

// Api/src/{version}/Controllers/TodosController.cs
[HttpPost("{id:guid}/complete")]
[ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
public Task<IActionResult> Complete(TodoId id, CancellationToken cancellationToken) =>
    _sender.Send(new CompleteTodoCommand(id), cancellationToken)
        .ToActionResultAsync(this, t => RepresentationMetadata.WithStrongETag(t.ETag), TodoResponse.From);
```
- **Incorrect:** PUT/PATCH/DELETE handler that calls `new UpdateXyzCommand(id, body)` without `ETagHelper.ParseIfMatch(Request)` and omits `.RequireETag(...)`. Returns `200` even when the client supplied a stale (or missing) `If-Match`, silently overwriting a concurrent change.
- **Reference:** See `.github/trellis-api-cookbook.md` Recipe 23 for the full endpoint-shape decision table; `Application/src/Todos/UpdateTodoCommand.cs`, `CompleteTodoCommand.cs`, `DeleteTodoCommand.cs` and the matching `Api/src/{date}/Controllers/TodosController.cs` for the canonical patterns; `.github/trellis-api-core.md §RequireETag` for the framework primitive.

### Use namespace-based API versioning

- **Rule:** 🔴 MUST place each API version's controllers in its own `Api/src/{yyyy-MM-dd}/Controllers/` folder with a matching `{ServiceName}.Api.v{yyyy_MM_dd}.Controllers` namespace. Do NOT add `[ApiVersion("...")]` attributes — `VersionByNamespaceConvention` derives the version from the namespace segment.
- **Rationale:** Trellis template controllers are deliberately thin (route binding + `_sender.Send(...)` + response mapping), so duplicating a controller per version is cheaper than maintaining a single shared controller with version-aware projection seams (`HttpContext.RequestedApiVersion` branches, per-version DTO selection, `[MapToApiVersion]` per action). One folder = one version is easier to reason about and impossible to silently break across versions (a v2 edit cannot affect v1 by accident).
- **When to add a new version:** Copy the latest version's `Api/src/{date}/Controllers/` and `Api/src/{date}/Models/` folders to a new `{date}` folder, change the namespace from `v{yyyy_MM_dd}` to the new value everywhere in the copy, then evolve the v2 copy independently — add fields to its `TodoResponse`, change endpoint shapes, etc. Older versions stay frozen.
- **Correct:**
```csharp
// Api/src/2026-03-26/Controllers/TodosController.cs — v1
namespace TodoSample.Api.v2026_03_26.Controllers;
[ApiController]
[Route("api/[controller]")]
public class TodosController : ControllerBase { /* ... */ }

// Api/src/2026-12-01/Controllers/TodosController.cs — v2 (independent copy)
namespace TodoSample.Api.v2026_12_01.Controllers;
[ApiController]
[Route("api/[controller]")]
public class TodosController : ControllerBase { /* same shape; new IsOverdue field on TodoResponse */ }

// Api/src/DependencyInjection.cs
services.AddApiVersioning()
        .AddMvc(options => options.Conventions.Add(new VersionByNamespaceConvention()))
        .AddApiExplorer()
        .AddOpenApi(options => options.Document.AddScalarTransformers());
```
- **Incorrect:**
```csharp
// ❌ [ApiVersion] attribute when the namespace already provides the version.
[ApiController]
[ApiVersion("2026-12-01")]                                                 // ❌ redundant
[Route("api/[controller]")]
public class TodosController : ControllerBase { /* ... */ }

// ❌ Single shared controller that branches on the requested api-version.
[ApiController]
[ApiVersion("2026-03-26")]
[ApiVersion("2026-12-01")]
[Route("api/[controller]")]
public class TodosController : ControllerBase
{
    [HttpGet("{id}")]
    public ActionResult<object> GetById(TodoId id)
    {
        var version = HttpContext.RequestedApiVersion();
        return version >= new ApiVersion(new DateOnly(2026, 12, 1))
            ? (object)v2Projection(todo)                                   // ❌ projection seam
            : (object)v1Projection(todo);
    }
}

// ❌ DI uses attribute-based discovery (no VersionByNamespaceConvention).
services.AddApiVersioning().AddMvc();                                      // ❌
```
- **Reference:** See `Api/src/2026-03-26/` and `Api/src/2026-12-01/` for the two-version reference layout, plus `Api/src/DependencyInjection.cs` for the DI wiring.

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
| Required `If-Match` on body-overwriting mutations (PUT/PATCH/DELETE, body-carrying POST, non-commutative additive ops) | `.RequireETag(expectedETags)` — see critical rule "Require `If-Match` on body-overwriting mutations" and cookbook Recipe 23 | `.OptionalETag(...)` or omitting the check (lost-update race, silent 200) |
| Body-less state-transition POST (e.g., `.../approve`, `.../cancel`, `.../submit`) | Rely on the state-machine transition guard (returns `422` on a stale transition) | `.RequireETag(...)` — ceremony without benefit; see cookbook Recipe 23 |

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
| Conventions | `ApplyTrellisConventionsFor<TContext>()` (source-generated, preferred) | Reflection `ApplyTrellisConventions(assembly)` fallback unless the context is private/generic/abstract; manual `HasConversion()` / `OwnsOne()` for Trellis-supported types |
| Interceptors | `AddTrellisInterceptors()` | Reimplement timestamp or ETag plumbing |
| Save changes in repositories | `SaveChangesResultUnitAsync()` | Bare `SaveChangesAsync()` |
| Optional lookup | `FirstOrDefaultMaybeAsync(...)` | `FirstOrDefaultAsync(...)` + `null` |
| Required lookup | `FirstOrDefaultResultAsync(..., new Error.NotFound(...))` | Returning `null` or throwing |
| `Maybe<T>` comparisons in LINQ | `WhereLessThan`, `WhereHasValue`, `WhereEquals`, etc. | Direct `Value` access in LINQ |
| Index containing `Maybe<T>` | `HasTrellisIndex(...)` | `HasIndex(...)` |
| Entity configuration placement | `IEntityTypeConfiguration<T>` in Acl | Inline `OnModelCreating` configuration |
## Reference Implementation

Study these files before replacing the Todo sample.

| Pattern | Files |
|---|---|
| Scalar value objects with `RequiredGuid`, `RequiredString`, `RequiredDateTime`, `ValidateAdditional` | `Domain/src/ValueObjects/` |
| `RequiredEnum` smart enum | `Domain/src/TodoStatus.cs` |
| Aggregate with `LazyStateMachine` and `Maybe<T>` partial properties | `Domain/src/Aggregates/TodoItem.cs` |
| Specification with `.And()` composition | `Domain/src/Specifications/OverdueTodoSpecification.cs` |
| Always-valid command with private constructor + `TryCreate` | `Application/src/Todos/UpdateTodoCommand.cs` |
| `Result.Ensure` authorization check | `Application/src/Todos/CompleteTodoCommand.cs` |
| `IAuthorizeResource<T>` with `SharedResourceLoaderById` or `ResourceLoaderById` | `Application/src/Todos/CompleteTodoCommand.cs`, `Acl/src/CompleteTodoResourceLoader.cs` |
| Repository returning `Maybe<T>` | `Application/src/Todos/ITodoRepository.cs` |
| Handlers returning domain types and controller DTO mapping | `Application/src/Todos/`, `Api/src/2026-03-26/Models/TodoResponse.cs` |
| `TimeProvider` for testable time validation | `Application/src/Todos/UpdateTodoCommand.cs` |
| Controller `TryCreate` → `BindAsync` → `Send` flow | `Api/src/2026-03-26/Controllers/TodosController.cs` |
| Domain, Application, and API tests | `Domain/tests/`, `Application/tests/`, `Api/tests/` |

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
- **Reference:** See `.github/trellis-api-core.md`, `.github/trellis-api-asp.md`, `.github/trellis-api-efcore.md`.

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
- **Reference:** See `.github/trellis-api-authorization.md`, `.github/trellis-api-cookbook.md`, `.github/trellis-api-asp.md`.

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
│   ├── trellis-api-primitives.md
│   ├── trellis-api-asp.md
│   ├── trellis-api-asp-apiversioning.md
│   ├── trellis-api-efcore.md
│   ├── trellis-api-efcore-outbox.md
│   ├── trellis-api-mediator.md
│   ├── trellis-api-authorization.md
│   ├── trellis-api-http.md
│   ├── trellis-api-http-abstractions.md
│   ├── trellis-api-statemachine.md
│   ├── trellis-api-fluentvalidation.md
│   ├── trellis-api-mediator-fluentvalidation.md
│   ├── trellis-api-analyzers.md
│   ├── trellis-api-cookbook.md
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
- **Reference:** See `Api/src/api.http`, `Api/src/http-client.env.json`, and `Api/src/Properties/launchSettings.json`.

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
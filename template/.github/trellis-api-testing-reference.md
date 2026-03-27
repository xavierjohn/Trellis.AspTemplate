# Trellis — AI Testing API Reference

> This document covers the `Trellis.Testing` package: FluentAssertions extensions, test builders,
> fake repositories, actor providers, and testing patterns for Trellis applications.
> For the core framework API, see `trellis-api-reference.md`.

## Namespaces

```csharp
using Trellis.Testing;          // Assertions, WebApplicationFactory extensions, MSAL types
using Trellis.Testing.Builders; // ResultBuilder, ValidationErrorBuilder
using Trellis.Testing.Fakes;    // FakeRepository, TestActorProvider, TestActorScope
```

---

## Result Assertions

```csharp
result.Should().BeSuccess()                              // returns AndWhichConstraint with value
result.Should().BeFailure()                              // returns AndWhichConstraint with Error
result.Should().BeFailureOfType<NotFoundError>()
result.Should().HaveValue(expected)
result.Should().HaveValueMatching(v => v.Name == "test")
result.Should().HaveValueEquivalentTo(expected)
result.Should().HaveErrorCode("not.found")
result.Should().HaveErrorDetail("Order not found")
result.Should().HaveErrorDetailContaining("not found")

// Async — works with both Task<Result<T>> and ValueTask<Result<T>>
await result.Should().BeSuccessAsync()
await result.Should().BeFailureAsync()
await result.Should().BeFailureOfTypeAsync<ValidationError>()
```

## Maybe Assertions

```csharp
maybe.Should().HaveValue()
maybe.Should().BeNone()
maybe.Should().HaveValueEqualTo(expected)
maybe.Should().HaveValueMatching(v => v > 0)
maybe.Should().HaveValueEquivalentTo(expected)
```

🔴 **Do NOT use** `.HasValue.Should().BeTrue()` or `.HasNoValue.Should().BeTrue()` — these bypass
Trellis.Testing's assertion messages. Always use `.Should().HaveValue()` / `.Should().BeNone()`.

## Error Assertions

```csharp
error.Should().Be(expectedError)
error.Should().HaveCode("validation.error")
error.Should().HaveDetail("Field is required")
error.Should().HaveDetailContaining("required")
error.Should().HaveInstance("/orders/123")
error.Should().BeOfType<ValidationError>()
```

## ValidationError Assertions

```csharp
validationError.Should().HaveFieldError("email")
validationError.Should().HaveFieldErrorWithDetail("email", "Email is required")
validationError.Should().HaveFieldCount(2)
```

## Test Builders

```csharp
// ResultBuilder
ResultBuilder.Success(value)
ResultBuilder.Failure<T>(error)
ResultBuilder.NotFound<T>("Order not found")
ResultBuilder.NotFound<T>("Order", "123")      // "Order 123 not found"
ResultBuilder.Validation<T>("Invalid", "field")
ResultBuilder.Unauthorized<T>()
ResultBuilder.Forbidden<T>()
// ... Conflict, Unexpected, Domain, RateLimit, BadRequest, ServiceUnavailable

// ValidationErrorBuilder
ValidationErrorBuilder.Create()
    .WithFieldError("email", "Required")
    .WithFieldError("name", "Too short", "Too long")
    .Build()           // → ValidationError
    .BuildFailure<T>() // → Result<T>
```

## FakeRepository

**Namespace: `Trellis.Testing.Fakes`**

In-memory repository for Application-layer handler tests. Stores entities in a dictionary, returns
`Result<T>` (NotFound if missing), and captures published domain events.

```csharp
// Construction
var repo = new FakeRepository<Order, OrderId>();

// Unique constraint — SaveAsync returns ConflictError on duplicate
repo.WithUniqueConstraint(o => o.Email);

// CRUD operations
await repo.SaveAsync(order);                               // Result<Unit> (ConflictError if unique violation)
var result = await repo.GetByIdAsync(orderId);             // Result<Order> (NotFound if missing)
var maybe = await repo.FindByIdAsync(orderId);             // Result<Maybe<Order>>
await repo.DeleteAsync(orderId);                           // Result<Unit> (NotFound if missing)

// Synchronous helpers for test setup and assertions
repo.Clear();                                              // Remove all entities
bool exists = repo.Exists(orderId);                        // Check existence
Order? order = repo.Get(orderId);                          // Get or null (no Result)
IEnumerable<Order> all = repo.GetAll();                    // All stored entities
int count = repo.Count;                                    // Number of stored entities

// Domain event inspection
repo.PublishedEvents                                       // IReadOnlyList<IDomainEvent>
```

## TestActorProvider and TestActorScope

**Namespace: `Trellis.Testing.Fakes`**

Mutable `IActorProvider` and `IAsyncActorProvider` for authorization testing. Uses `AsyncLocal<Actor?>` internally so parallel tests sharing a singleton provider never interfere. `WithActor` returns a scope that restores the previous actor on dispose, eliminating `try/finally` boilerplate.

Implements both `IActorProvider` (sync) and `IAsyncActorProvider` (async). Register as both interfaces in DI when the system under test uses `IAsyncActorProvider`.

### Construction

```csharp
var actorProvider = new TestActorProvider("admin", "Orders.Read", "Orders.Write");
var actorFromInstance = new TestActorProvider(actor);               // from Actor instance

// Access the current actor (implements IActorProvider and IAsyncActorProvider)
Actor actor = actorProvider.GetCurrentActor();
Actor actor = await actorProvider.GetCurrentActorAsync(cancellationToken);
```

### Scoped Actor Switching

```csharp
// Temporarily switch actor — restored on dispose
await using var scope1 = actorProvider.WithActor("user-1", "Orders.Read");
await using var scope2 = actorProvider.WithActor(actor);           // from Actor instance

// Synchronous dispose also supported
using var scope3 = actorProvider.WithActor("user-1", "Orders.Read");
```

### Nested Scopes

```csharp
await using (actorProvider.WithActor("user-1", "Read"))
{
    await using (actorProvider.WithActor("user-2", "Write"))
    {
        // actor is user-2
    }
    // actor is user-1
}
// actor is admin
```

## ServiceCollection Extensions

Replaces existing `IResourceLoader<TMessage, TResource>` DI registrations with a test implementation. Registered as scoped, matching the production lifetime.

```csharp
// Stateless fake — capture a pre-created instance
var fakeLoader = new FakeOrderResourceLoader(fakeRepo);
services.ReplaceResourceLoader<CancelOrderCommand, Order>(_ => fakeLoader);

// Scoped dependency — resolve from the container
services.ReplaceResourceLoader<CancelOrderCommand, Order>(
    sp => new FakeOrderResourceLoader(sp.GetRequiredService<AppDbContext>()));
// Internally: RemoveAll + AddScoped
```

## WebApplicationFactory Extensions

Creates an `HttpClient` with the `X-Test-Actor` header pre-set, encoding actor identity and permissions as JSON.

```csharp
// Extension on WebApplicationFactory<TEntryPoint>
var client = factory.CreateClientWithActor("user-1", "Orders.Create", "Orders.Read");
// Sets header: X-Test-Actor: {"Id":"user-1","Permissions":["Orders.Create","Orders.Read"]}

// Overload with full Actor object (for ForbiddenPermissions, Attributes)
var actor = Actor.Create("user-1", new HashSet<string> { "Orders.Create" });
var client = factory.CreateClientWithActor(actor);
```

The full Actor JSON shape supports `ForbiddenPermissions` and `Attributes` in addition to `Id` and `Permissions`:
```json
{
  "Id": "user-1",
  "Permissions": ["Orders.Create", "Orders.Read"],
  "ForbiddenPermissions": ["Orders.Delete"],
  "Attributes": { "tenantId": "tenant-42" }
}
```

---

## MSAL E2E Test Support

**Namespace: `Trellis.Testing`**

For end-to-end tests against a real Entra ID (Azure AD) tenant. Acquires real tokens using ROPC (Resource Owner Password Credentials) flow.

```csharp
// Configuration
public sealed class MsalTestOptions
{
    public string TenantId { get; set; }
    public string ClientId { get; set; }
    public string[] Scopes { get; set; }
    public Dictionary<string, TestUserCredentials> TestUsers { get; set; }
}

public sealed class TestUserCredentials
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string[] ExpectedPermissions { get; set; }
}

// Token acquisition
var tokenProvider = new MsalTestTokenProvider(msalOptions);
string token = await tokenProvider.AcquireTokenAsync("admin-user", cancellationToken);

// WebApplicationFactory extension — creates client with real Bearer token
var client = await factory.CreateClientWithEntraTokenAsync(tokenProvider, "admin-user", cancellationToken);
```

> ⚠️ MSAL types use reflection and are not AOT-compatible.

---

## Test Patterns

### Testing Result<T> with TRLS003 Analyzer

The `TRLS003` analyzer warns when accessing `result.Value` without checking `IsSuccess`. Since
`TreatWarningsAsErrors` is typically enabled, use FluentAssertions to access values safely:

```csharp
// ✅ Correct — chain off .Which after asserting success
var result = Customer.TryCreate(firstName, lastName, email, phone, address);
result.Should().BeSuccess()
    .Which.Email.Should().Be(email);

// ✅ Also correct — assert then access .Value (TRLS003 still fires but assertion guarantees safety)
var result = Order.TryCreate(customerId, lineItems);
result.Should().BeSuccess();
var order = result.Value;    // safe after assertion, suppress TRLS003 if needed

// ✅ Correct — failure assertions
var result = order.Submit();
result.Should().BeFailure()
    .Which.Should().BeOfType<ValidationError>();

// ❌ Wrong — TRLS003 compile error
var customer = Customer.TryCreate(...).Value;  // Accessing .Value without guard
```

### Domain Unit Tests

```csharp
using Trellis.Testing;

[Fact]
public void CreateOrder_ValidInput_ReturnsSuccess()
{
    var customerId = CustomerId.NewUniqueV4();
    var result = Order.TryCreate(customerId);

    result.Should().BeSuccess()
        .Which.CustomerId.Should().Be(customerId);
}

[Fact]
public void CreateOrder_EmptySubmit_ReturnsFailure()
{
    var orderResult = Order.TryCreate(CustomerId.NewUniqueV4());
    orderResult.Should().BeSuccess();

    var order = orderResult.Value;
    var result = order.Submit();

    result.Should().BeFailure()
        .Which.Should().BeOfType<DomainError>()
        .Which.Should().HaveDetailContaining("empty");
}
```

### Application Handler Tests with FakeRepository

```csharp
[Fact]
public async Task GetOrder_NotFound_ReturnsNotFoundError()
{
    var repo = new FakeRepository<Order, OrderId>();
    var result = await repo.GetByIdAsync(OrderId.NewUniqueV4());

    result.Should().BeFailure()
        .Which.Should().BeOfType<NotFoundError>();
}
```

### Maybe<T> Assertions in Tests

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

### Authorization Tests

```csharp
[Fact]
public async Task Cancel_ByOwner_Succeeds()
{
    var actorProvider = new TestActorProvider("owner-1", Permissions.OrdersCancel);
    // ... set up order with CreatedByActorId = "owner-1"
    var result = await sender.Send(new CancelOrderCommand(orderId));
    result.Should().BeSuccess();
}

[Fact]
public async Task Cancel_ByNonOwner_ReturnsForbidden()
{
    var actorProvider = new TestActorProvider("other-user", Permissions.OrdersCancel);
    // ... set up order with CreatedByActorId = "owner-1"
    var result = await sender.Send(new CancelOrderCommand(orderId));
    result.Should().BeFailureOfType<ForbiddenError>();
}
```

### Conflict/Uniqueness Test

```csharp
[Fact]
public async Task Save_duplicate_email_returns_conflict()
{
    var repo = new FakeRepository<Customer, CustomerId>();
    repo.WithUniqueConstraint(c => c.Email);

    var customer1 = Customer.TryCreate(email: EmailAddress.Create("a@b.com")).Value;
    await repo.SaveAsync(customer1);

    var customer2 = Customer.TryCreate(email: EmailAddress.Create("a@b.com")).Value;
    var result = await repo.SaveAsync(customer2);

    result.Should().BeFailureOfType<ConflictError>();
}
```

### Maybe Query Test

```csharp
[Fact]
public async Task FindById_missing_returns_none()
{
    var repo = new FakeRepository<Order, OrderId>();

    var result = await repo.FindByIdAsync(OrderId.NewUniqueV7());

    result.Should().BeSuccess();
    result.Value.Should().BeNone();
}

[Fact]
public async Task FindById_existing_returns_value()
{
    var repo = new FakeRepository<Order, OrderId>();
    var order = Order.TryCreate(...).Value;
    await repo.SaveAsync(order);

    var result = await repo.FindByIdAsync(order.Id);

    result.Should().BeSuccess();
    result.Value.Should().HaveValue();
}
```

### State Machine Transition Test

```csharp
[Fact]
public void Valid_transition_returns_new_state()
{
    var todo = TodoItem.TryCreate(title, dueDate, tag, actorId).Value;
    todo.Start().Should().BeSuccess();

    var result = todo.Complete();

    result.Should().BeSuccess()
        .Which.Should().Be(TodoStatus.Completed);
}

[Fact]
public void Invalid_transition_returns_failure()
{
    var todo = TodoItem.TryCreate(title, dueDate, tag, actorId).Value;
    // Skip Start — try to Complete from Pending

    var result = todo.Complete();

    result.Should().BeFailure();
}
```

### Domain Event Assertions

```csharp
[Fact]
public async Task Save_publishes_domain_events()
{
    var repo = new FakeRepository<Order, OrderId>();
    var order = Order.TryCreate(customerId).Value;
    await repo.SaveAsync(order);

    repo.PublishedEvents.Should().ContainSingle()
        .Which.Should().BeOfType<OrderCreated>();
}
```

### HTTP Authorization Matrix

Test all permission scenarios for an endpoint:

```csharp
[Fact]
public async Task Complete_by_owner_returns_200()
{
    var client = factory.CreateClientWithActor("owner-1", "todos:complete");
    // ... create todo as owner-1, then complete
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}

[Fact]
public async Task Complete_by_non_owner_returns_403()
{
    // ... create todo as owner-1
    var client = factory.CreateClientWithActor("other-user", "todos:complete");
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}

[Fact]
public async Task Complete_without_permission_returns_403()
{
    var client = factory.CreateClientWithActor("owner-1"); // no todos:complete permission
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

### Decision Table — When to Use What

| Scenario | Tool | Why |
|----------|------|-----|
| Domain unit tests (value objects, aggregates, specs) | Direct construction, no DI | Pure logic, no infrastructure |
| Handler tests (commands, queries) | `FakeRepository` + `TestActorProvider` + `ISender` via DI | Tests handler logic with mocked persistence and auth |
| Authorization tests | `TestActorProvider.WithActor(...)` scoped switching | Tests permission and ownership checks |
| Resource authorization tests | `ReplaceResourceLoader(...)` + `TestActorProvider` | Tests `IAuthorizeResource<T>` pipeline |
| API integration tests | `WebApplicationFactory` + `CreateClientWithActor(...)` | Tests full HTTP round-trip with real middleware |
| E2E with real Entra ID | `CreateClientWithEntraTokenAsync(...)` + `MsalTestTokenProvider` | Tests against real identity provider |

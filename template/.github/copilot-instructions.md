# Copilot Instructions — Building with Trellis

This file contains **service-architecture rules** for the scaffolded service. It deliberately does **not** restate framework API. For framework knowledge — types, methods, factories, generics, conventions, error shapes — read the API reference files listed below. They ship inside each Trellis NuGet package and are kept in sync with the package version on every build.

## 🔴 Before Writing Code — Read the API References

### API reference delivery

The `.github/trellis-api-*.md` files are auto-copied from the Trellis NuGet packages on every restore/build by the `_CopyTrellisApiReference` MSBuild target (shipped as `buildTransitive` in each Trellis package). Treat them as authoritative — they describe the **exact** API surface of the version pinned in `Directory.Packages.props`.

To force-refresh after bumping `TrellisVersion`:
```
dotnet build ./{ServiceName}.slnx /t:TrellisSyncApiReference
```

### Required reading

Before writing any code, read these in order. They are the source of truth for every Trellis idiom — do not pattern-match from memory.

| Order | File | Why |
|---|---|---|
| 1 | `.github/trellis-api-cookbook.md` | **Start here.** 14 ready-to-copy recipes covering the most common patterns: CRUD aggregate, command/query handlers, paginated queries, minimal-API and MVC wiring, conditional GET, authorization, EF Core mappings, state machines, tests, anti-patterns, DI wiring, owned value objects, optional DTO fields. |
| 2 | `.github/trellis-api-core.md` | `Result<T>`, `Maybe<T>`, `Error` (closed ADT), `WriteOutcome<T>`, value-object base types, DDD primitives, `EquatableArray`, `FieldViolation`, `InputPointer`. |
| 3 | `.github/trellis-api-primitives.md` | `RequiredString<T>`, `RequiredGuid<T>`, `RequiredInt<T>`, `RequiredDecimal<T>`, `RequiredDateTime<T>`, `RequiredEnum<T>`, `Money`, `MonetaryAmount`, `EmailAddress`, `PhoneNumber`, `CurrencyCode`, etc. |
| 4 | `.github/trellis-api-mediator.md` | `ICommand<T>`, `IQuery<T>`, `IMessageHandler<,>`, pipeline behaviors, `AddTrellis*` registration. |
| 5 | `.github/trellis-api-efcore.md` | `ApplyTrellisConventions`, `AddTrellisInterceptors`, `SaveChangesResultUnitAsync`, `MaybeConvention`, `CompositeValueObjectConvention`, `[OwnedEntity]`, `HasTrellisIndex`, `FirstOrDefaultMaybeAsync`, `FirstOrDefaultResultAsync`, `MaybePropertyMapping`. |
| 6 | `.github/trellis-api-asp.md` | `AddTrellisAsp`, scalar-VO model binding, `Maybe<T>` JSON converters, `ToHttpResponse`, `ToActionResult`, `HttpResponseOptionsBuilder`, ETag/range/preference helpers, ProblemDetails wiring. |
| 7 | `.github/trellis-api-authorization.md` | `IAuthorize`, `IAuthorizeResource<T>`, `IIdentifyResource<T,TId>`, `SharedResourceLoaderById<T,TId>`, `IActorProvider`, `AddResourceAuthorization`, caching providers. |
| 8 | `.github/trellis-api-statemachine.md` | `LazyStateMachine<TState, TTrigger>`, `FireResult`, `StateMachineExtensions`, transition guards. |
| 9 | `.github/trellis-api-fluentvalidation.md` | Trellis ↔ FluentValidation bridge, validators as pipeline behaviors, `Result`-shaped failures. |
| 10 | `.github/trellis-api-http.md` | `EntityTagValue`, `RangeOutcome`, conditional-request helpers, content negotiation. |
| 11 | `.github/trellis-api-testing-reference.md` | `Trellis.Testing` and `Trellis.Testing.AspNetCore`: `Should().BeSuccess()`, `UnwrapError()`, `Unwrap()` (test-only), `FakeRepository`, `TestWebApplicationFactoryFixture`, `CreateClientWithActor`. |
| 12 | `.github/trellis-api-analyzers.md` | All `TRLS###` analyzer diagnostics with rationale and code-fix behavior. |

> **Hard rule:** when an example, factory, or method shape would be useful, look it up in the file above instead of guessing. Stale knowledge of v1 (e.g., `Result.Success(...)`, `Result.Failure(...)`, `Error.Validation("msg", "field")`, `result.Value`) will silently produce wrong code that compiles only by accident.

## Critical Rules

Each rule is normative for **this service**. The "Reference" pointer is where to find the supporting framework API. Do not duplicate API teaching here.

### Study the template reference implementation first

- **Rule:** 🔴 MUST read the `TodoSample.*` reference implementation before replacing it.
- **Rationale:** The template is a complete worked example covering every layer (Domain, Application, Acl, Api, Tests). Replacing it cold loses the wiring proof points it demonstrates.
- **Reference:** See the *Reference Implementation* table below for the exact files to study.

### Treat errors and optional values as explicit types

- **Rule:** 🔴 MUST use `Result<T>` for expected failures and `Maybe<T>` for optional values. Never throw for business logic. Never use `try/catch` in Domain or Application layers for expected outcomes.
- **Rationale:** Trellis relies on Railway Oriented Programming; exceptions for expected paths break the pipeline and reduce testability. The `TRLS00x` analyzers enforce this.
- **Reference:** See `trellis-api-core.md` (Result, Maybe, Error ADT) and `trellis-api-cookbook.md` Recipes 1, 2, 11.

### Eliminate primitive obsession on domain surfaces

- **Rule:** 🔴 MUST expose value objects on aggregates, entities, commands, and public domain methods. Do not expose raw `Guid`, `string`, `int`, `decimal`, or `DateTime`/`DateTimeOffset` for domain concepts.
- **Rationale:** Trellis models validity at the type level; primitive-based domain APIs reintroduce invalid states and bypass the source-generated converters/binders.
- **Reference:** See `trellis-api-primitives.md` for the `Required*<T>` family and ready-made primitives (`EmailAddress`, `PhoneNumber`, `CurrencyCode`, `Money`, `MonetaryAmount`).

### Use `RequiredEnum<T>` for all domain enum-like concepts

- **Rule:** 🔴 MUST model domain enums as `RequiredEnum<T>` partial classes, not C# `enum`.
- **Rationale:** `RequiredEnum<T>` provides validation, JSON conversion, EF Core conversion, LINQ support, and attachable per-member behavior — none of which a C# `enum` gives you.
- **Reference:** See `trellis-api-primitives.md §RequiredEnum<TSelf>` and `trellis-api-efcore.md §ModelConfigurationBuilderExtensions`.

### Make commands always-valid and time-testable

- **Rule:** 🔴 MUST make commands receive value objects, use a private constructor plus `TryCreate` when cross-field validation exists, and use `TimeProvider` instead of `DateTime.UtcNow`/`DateTimeOffset.UtcNow`.
- **Rationale:** Command validity belongs at construction time; time-dependent rules must remain testable. `TimeProvider.System` is registered as a singleton in `Application/src/DependencyInjection.cs`.
- **Reference:** See `trellis-api-mediator.md` and `trellis-api-cookbook.md` Recipe 2.

### Prefer shared resource loaders over per-command loaders

- **Rule:** 🟡 SHOULD use `SharedResourceLoaderById<TResource, TId>` + `IIdentifyResource<TResource, TId>` as the default resource-authorization pattern. Use a per-command `ResourceLoaderById<TMessage, TResource, TId>` only when the command requires custom load logic (composite keys, non-ID lookups, message-specific projections).
- **Rationale:** `SharedResourceLoaderById` eliminates per-command loader classes — one loader per aggregate type serves every command that authorizes against that aggregate. `AddResourceAuthorization(assemblies)` auto-bridges commands implementing `IIdentifyResource` to the shared loader.
- **Reference:** See `trellis-api-authorization.md §SharedResourceLoaderById` and `trellis-api-cookbook.md` Recipe 7.

### Build layer-by-layer and compile between layers

- **Rule:** 🔴 MUST implement Domain → Application → Acl → Api → Tests, running `dotnet build` between layers and `dotnet test` after tests are added.
- **Rationale:** Trellis uses source generators for `partial Maybe<T>` properties, `[OwnedEntity]` plumbing, scalar-VO converters, and Mediator handler registrations; later layers depend on generated output from earlier builds.
- **Reference:** See *Implementation Order and Build Checkpoints* below.

### Return `Maybe<T>` from repository lookups

- **Rule:** 🔴 MUST return `Maybe<T>` from repository lookups and convert to `Result<T>` in handlers with `.ToResult(new Error.NotFound(...))`.
- **Rationale:** Absence is data, not failure; handlers own the domain meaning of "not found" (which `ResourceRef` to attach, what `Detail` to surface).
- **Reference:** See `trellis-api-core.md` (Maybe → Result conversion) and `trellis-api-efcore.md §QueryableExtensions` (`FirstOrDefaultMaybeAsync`, `FirstOrDefaultResultAsync`).

### Keep handlers on the ROP track

- **Rule:** 🔴 MUST compose handler flows with `Bind`, `BindAsync`, `CheckAsync`, `Map`, `Tap`, and related result combinators. Do not destructure and branch imperatively unless branching materially improves readability of a non-linear flow.
- **Rationale:** ROP chains preserve failure propagation, keep the success path explicit, and let analyzers spot `Map`-vs-`Bind` mistakes (`TRLS010`/`011`) and orphaned `IsSuccess` checks (`TRLS008`).
- **Reference:** See `trellis-api-core.md` (`Bind`, `BindAsync`, `CheckAsync`, `Match`, `TryGetValue`, deconstruction) and `trellis-api-cookbook.md` Recipes 2, 4, 5.

### Prefer a single final save in multi-aggregate handlers

- **Rule:** 🔴 MUST prefer a single final `SaveChangesResultUnitAsync` call when a handler mutates multiple aggregates or repositories. Do not call `SaveAsync` on each repository independently.
- **Rationale:** EF Core's `DbContext` is already a unit of work — all repositories share the same scoped `DbContext`, so a single `SaveChanges` at the end atomically persists every tracked change. Per-repository `SaveAsync` calls risk committing partial state if a later operation fails.
- **Exceptions:** Single-aggregate handlers using `RepositoryBase.SaveAsync` are fine. If a store-generated value is needed mid-flow, prefer client-generated IDs (Trellis typed IDs are GUIDs); when impossible, wrap multiple saves in an explicit EF Core transaction.
- **Reference:** See `trellis-api-efcore.md §DbContextExtensions` (`SaveChangesResultUnitAsync`).

### Use `LazyStateMachine<TState, TTrigger>` in aggregates

- **Rule:** 🔴 MUST use `LazyStateMachine<TState, TTrigger>` instead of constructing a Stateless `StateMachine<TState, TTrigger>` eagerly inside persisted aggregates.
- **Rationale:** EF Core materializes aggregates before state properties are populated; eager state-machine initialization throws `NullReferenceException` at materialization time.
- **Reference:** See `trellis-api-statemachine.md §LazyStateMachine<TState, TTrigger>` and `trellis-api-cookbook.md` Recipe 9.

### Follow Trellis EF Core conventions exactly

- **Rule:** 🔴 MUST use `ApplyTrellisConventions`, `AddTrellisInterceptors`, `SaveChangesResultUnitAsync`, `partial Maybe<T>` properties, and `HasTrellisIndex` exactly as Trellis expects. **Do NOT** explicitly configure `Maybe<T>` properties or composite owned value objects (`[OwnedEntity]` types like `Money`, `ShippingAddress`) in `IEntityTypeConfiguration` — the conventions own those mappings.
- **Rationale:** Trellis persistence relies on conventions and source generators; overriding them with manual `builder.Property(c => c.SomeMaybeT)` or `builder.OwnsOne(c => c.SomeOwnedVo)` either fails at model validation ("type not supported by the database provider") or silently shadows the convention's annotations.
- **Reference:** See `trellis-api-efcore.md §ModelConfigurationBuilderExtensions`, `trellis-api-efcore.md §MaybeEntityTypeBuilderExtensions`, `trellis-api-efcore.md §DbContextExtensions`, and `trellis-api-cookbook.md` Recipes 8, 13.

### Keep controllers thin and value-object-first

- **Rule:** 🔴 MUST accept scalar value-object parameters directly in controllers (not raw `Guid`/`string`), map domain results to DTOs in controllers (not in handlers), and write `///` XML doc comments on **every** `public` / `protected` type and member as you create it (controllers, action methods, DTOs and their properties, handlers, commands/queries, aggregates, value objects, domain events).
- **Rationale:** Scalar binding and HTTP wire-mapping are presentation concerns; handlers stay domain-focused. XML docs are non-negotiable: (1) the template enables `GenerateDocumentationFile` + `TreatWarningsAsErrors`, so missing docs break the build with CS1591; (2) `Microsoft.AspNetCore.OpenApi` reads the generated `<assembly>.xml` at runtime to populate `summary` / `description` / `param` / `remarks` on every operation and schema in the published OpenAPI document — without docs, Swagger UI and generated clients show opaque, undescribed endpoints; (3) docfx consumes the same XML for static reference docs. Suppressing CS1591 silently degrades all three. Write the docs upfront, not as a build-fix afterthought.
- **Reference:** See `trellis-api-asp.md` (`ActionResultExtensions`, `ServiceCollectionExtensions`, `HttpResponseOptionsBuilder`) and `trellis-api-cookbook.md` Recipes 4, 5, 14.

### Read the testing reference before writing tests

- **Rule:** 🔴 MUST read `trellis-api-testing-reference.md` before writing tests, and use the assertions, fakes, and fixtures it provides.
- **Rationale:** The testing package already provides `Should().BeSuccess()`/`UnwrapError()` assertions, `FakeRepository<T,TId>`, `TestWebApplicationFactoryFixture`, `CreateClientWithActor`, and `Unwrap()` (test-only — production code uses `TryGetValue`/`Match`/deconstruction). Hand-rolling these wastes time and diverges from the conventions analyzers expect.
- **Reference:** See `trellis-api-testing-reference.md` and `trellis-api-cookbook.md` Recipe 10.

### Keep `Smoke.cs` in every test project

- **Rule:** 🔴 MUST NOT delete `Smoke.cs` from any test project (`Domain.Tests`, `Application.Tests`, `Api.Tests`, `AntiCorruptionLayer.Tests`).
- **Rationale:** Microsoft.Testing.Platform (MTP) returns exit code 8 ("zero tests ran") when a test project compiles successfully but contains no `[Fact]` methods, and the slnx-level `dotnet test` treats that as a run failure. While replacing the `TodoSample.*` tests with your domain tests, there is a window where a project may have no real tests yet — `Smoke.cs` keeps `dotnet test` green during that window. Replace TodoSample tests around it; do not delete it.

## Decision Tables

These tables condense the rules above into "if you see this scenario, reach for that idiom" lookups. They do not introduce new rules — they index the API references.

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
| Composite value object (multi-field) | `ValueObject` + `Result.Combine(...)` + `GetEqualityComponents()` + `[OwnedEntity]` + `[JsonConverter(typeof(CompositeValueObjectJsonConverter<T>))]` | Scalar-shaped wrapper with fake `Value` |
| Optional composite value object on a domain model | `partial Maybe<T>` (where `T` is the composite VO) | Manual nullable owned-type plumbing |
| Optional composite value object on a request DTO | Nullable transport (`T?`) + controller-seam `Maybe.From(...)` | `Maybe<T>` directly on the DTO (silently bypasses `TryCreate`) |

### Validation and authorization decisions

| Scenario | Use | Not |
|---|---|---|
| Cross-field command validation | Private constructor + `TryCreate(...)` returning `Result<TCommand>` | Mutable command + later validation |
| Validation that cannot happen in `TryCreate` | `IValidate.Validate()` returning `IResult` | Late handler-only validation |
| Permission-based authorization | `IAuthorize` | Handler-side permission `if` statements |
| Resource-based authorization | `IAuthorizeResource<TResource>` + loader | Handler-side ownership checks |
| Shared loader by ID | `SharedResourceLoaderById<TResource, TId>` + `IIdentifyResource<TResource, TId>` | Per-command loader code |
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
| Single-aggregate handler save | `RepositoryBase.SaveAsync` | Direct `SaveChangesAsync()` |
| Multi-aggregate handler save | One final `_context.SaveChangesResultUnitAsync()` | Per-repository `SaveAsync` calls |
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
| Optional lookup | `FirstOrDefaultMaybeAsync(...)` | `FirstOrDefaultAsync(...)` + `null` check |
| Required lookup | `FirstOrDefaultResultAsync(..., new Error.NotFound(...))` | Returning `null` or throwing |
| `Maybe<T>` comparisons in LINQ | `WhereLessThan`, `WhereHasValue`, `WhereEquals`, etc. | Direct `Value` access in LINQ |
| Index containing `Maybe<T>` | `HasTrellisIndex(...)` | `HasIndex(...)` |
| `Maybe<T>` property in `IEntityTypeConfiguration` | Say nothing — `MaybeConvention` owns it | `builder.Property(c => c.SomeMaybeT)` (model-validation error) |
| Composite owned VO in `IEntityTypeConfiguration` | Say nothing — `CompositeValueObjectConvention` owns it; use `OwnsOne` only to **override** column metadata | `builder.Property(p => p.UnitPrice)` for `Money`/`ShippingAddress`/etc. |
| Entity configuration placement | `IEntityTypeConfiguration<T>` in Acl | Inline `OnModelCreating` configuration |

## Reference Implementation

Study these files before replacing the `TodoSample.*` code.

| Pattern | Files |
|---|---|
| Scalar value objects with `RequiredGuid`, `RequiredString`, `RequiredDateTime`, `ValidateAdditional` | `Domain/src/ValueObjects/` |
| `RequiredEnum` smart enum | `Domain/src/TodoStatus.cs` |
| Aggregate with `LazyStateMachine` and `Maybe<T>` partial properties | `Domain/src/Aggregates/TodoItem.cs` |
| Specification with `.And()` composition | `Domain/src/Specifications/OverdueTodoSpecification.cs` |
| Always-valid command with private constructor + `TryCreate` | `Application/src/Todos/UpdateTodoCommand.cs` |
| `Result.Ensure` authorization check | `Application/src/Todos/CompleteTodoCommand.cs` |
| `IAuthorizeResource<T>` with `SharedResourceLoaderById` and `IIdentifyResource` | `Application/src/Todos/CompleteTodoCommand.cs`, `Acl/src/TodoItemResourceLoader.cs` |
| Repository returning `Maybe<T>` | `Application/src/Todos/ITodoRepository.cs` |
| Handlers returning domain types and controller DTO mapping | `Application/src/Todos/`, `Api/src/2026-03-26/Models/TodoResponse.cs` |
| `TimeProvider` for testable time validation | `Application/src/Todos/UpdateTodoCommand.cs` |
| Controller `TryCreate` → `BindAsync` → `Send` flow | `Api/src/2026-03-26/Controllers/TodosController.cs` |
| Domain, Application, and API tests | `Domain/tests/`, `Application/tests/`, `Api/tests/` |

## Architecture and Layout

### Layer dependency matrix

- **Rule:** 🟡 SHOULD keep dependencies flowing inward only.
- **Rationale:** Trellis expects domain purity, application orchestration, Acl persistence adapters, and API presentation boundaries.

| Layer | Can depend on | Cannot depend on | Contains |
|---|---|---|---|
| Domain | Trellis packages only (`Trellis.Core`, `Trellis.Primitives`, `Trellis.Authorization`, `Trellis.StateMachine`) | EF Core, ASP.NET Core, Mediator | Aggregates, entities, value objects, domain events, specifications, permission constants |
| Application | Domain, Mediator, `Trellis.Mediator` | ASP.NET Core, EF Core providers | Commands, queries, handlers, repository interfaces |
| Acl | Application, `Trellis.EntityFrameworkCore`, EF Core provider | API types | `DbContext`, entity configurations, repository implementations, migrations, resource loaders |
| Api | Application, Acl, `Trellis.Asp` | Domain persistence implementation details | Controllers/endpoints, DTOs, `Program.cs`, `IActorProvider` |

> **Why "Acl"?** ACL stands for Anti-Corruption Layer. It adapts external systems (SQL Server, message queues, other services) to the domain model and avoids overloading the word "Infrastructure".

### Composition root and registration rules

- **Rule:** 🔴 MUST keep repository interfaces in Application, implementations in Acl, one `DependencyInjection.cs` per layer, `IActorProvider` registered in Api, and `TimeProvider.System` as a singleton in Application.
- **Rationale:** ASP.NET Core does not auto-register `TimeProvider`. `IActorProvider` must be configured per environment (development vs. production). For custom providers that perform expensive async operations (database permission lookups, etc.), wrap with `AddCachingActorProvider<T>()` so the `Task<Actor>` is cached per DI scope and not recomputed on every authorization check.
- **Reference:** See `trellis-api-authorization.md` (`IActorProvider`, `AddDevelopmentActorProvider`, `AddEntraActorProvider`, `AddClaimsActorProvider`, `AddCachingActorProvider<T>`) and `trellis-api-asp.md §ServiceCollectionExtensions` (`AddTrellisAsp`).

### Project layout

- **Rule:** 🟡 SHOULD preserve the template structure and only add code where the template expects it.
- **Rationale:** The solution, central package management, build props, and test props are all preconfigured.

```text
{ServiceName}/
├── {ServiceName}.slnx
├── Directory.Build.props          ← DO NOT MODIFY
├── Directory.Packages.props       ← ADD new packages here (versions only)
├── global.json                    ← DO NOT MODIFY
├── build/
│   └── test.props                 ← DO NOT MODIFY
├── .github/
│   ├── copilot-instructions.md    ← this file
│   └── trellis-api-*.md           ← auto-synced from Trellis NuGet packages
├── Domain/
│   ├── src/Domain.csproj
│   └── tests/Domain.Tests.csproj         ← keep Smoke.cs
├── Application/
│   ├── src/Application.csproj
│   └── tests/Application.Tests.csproj    ← keep Smoke.cs
├── Acl/
│   ├── src/AntiCorruptionLayer.csproj
│   └── tests/AntiCorruptionLayer.Tests.csproj   ← keep Smoke.cs
└── Api/
    ├── src/Api.csproj
    └── tests/Api.Tests.csproj            ← keep Smoke.cs
```

> **NuGet packages:** Add `<PackageVersion>` to `Directory.Packages.props`, then add `<PackageReference>` *without* a version in the relevant `.csproj`.

> **Upgrading Trellis packages:** After changing `TrellisVersion` in `Directory.Packages.props`, run `dotnet build ./{ServiceName}.slnx /t:TrellisSyncApiReference` from the service repository root to refresh the `.github/trellis-api-*.md` reference files from the new package versions.

### HTTP request documentation files

- **Rule:** 🟡 SHOULD replace `Api/src/api.http` with end-to-end requests for every endpoint, and keep complex header values in `Api/src/http-client.env.json` as escaped JSON strings.
- **Rationale:** The `.http` file is living API documentation, and the HTTP client only supports scalar variable substitution.
- **Reference:** See `Api/src/api.http`, `Api/src/http-client.env.json`, and `Api/src/Properties/launchSettings.json`.

## Implementation Order and Build Checkpoints

### Build between layers

- **Rule:** 🔴 MUST build after each layer because generated code appears only after compilation.
- **Rationale:** `MaybePartialPropertyGenerator` emits `_camelCase` backing fields used later by EF Core configuration and query helpers; `OwnedEntityGenerator` emits parameterless constructors required by EF materialization; the Mediator source generator emits handler registrations consumed by `AddTrellis*` extensions.

```text
1. Domain/src       — value objects, aggregates, entities, events, specifications, permissions   → dotnet build
2. Application/src  — repository interfaces, commands, queries, handlers                          → dotnet build
3. Acl/src          — DbContext, entity configurations, repositories, resource loaders            → dotnet build
4. Api/src          — controllers, DTOs, Program.cs, IActorProvider                               → dotnet build
5. Tests            — Domain.Tests, Application.Tests, AntiCorruptionLayer.Tests, Api.Tests       → dotnet test
```

# Trellis.ServiceLevelIndicators.Asp — API Reference

**Package:** `Trellis.ServiceLevelIndicators.Asp`  
**Namespace:** `Trellis.ServiceLevelIndicators`  
**Purpose:** ASP.NET Core integration for [`Trellis.ServiceLevelIndicators`](trellis-api-sli.md). Provides middleware that automatically emits SLI latency metrics for every request, MVC and Minimal API attribute conventions for tagging the customer resource id and additional measured route values, and an enrichment pipeline for adding custom attributes (e.g. HTTP method, API version).

For API-versioning-specific enrichment, see [`trellis-api-sli-apiversioning.md`](trellis-api-sli-apiversioning.md).

See also: [`trellis-api-sli.md`](trellis-api-sli.md).

---

## Auto-emitted dimensions (in addition to the core ones)

When the middleware is registered, every measured request emits these tags on top of the core `CustomerResourceId` / `LocationId` / `Operation` / `Outcome` dimensions:

| Tag | Source |
|---|---|
| `Operation` | HTTP method plus route template (e.g. `GET WeatherForecast` or `GET /teams/{teamId}`) — derived from `ControllerActionDescriptor.AttributeRouteInfo.Template` for MVC, or from the route pattern for Minimal APIs. Overridable via `[ServiceLevelIndicator(operation)]` or `AddServiceLevelIndicator("operation")`. |
| `Outcome` | `Success` for 2xx/3xx, `ClientError` for common caller errors (400/401/403/404/409/412/422), `Failure` for 429/5xx and unhandled exceptions, and `Ignored` for request-aborted cancellations. |
| `http.response.status.code` | `HttpContext.Response.StatusCode`. |
| `http.request.method` | `HttpContext.Request.Method`; emitted by default. |

---

## Types

### IServiceLevelIndicatorBuilder

**Declaration**

```csharp
public interface IServiceLevelIndicatorBuilder
```

Builder returned by `AddServiceLevelIndicator(...)` to chain MVC integration, HTTP-method enrichment, and custom enrichments.

**Properties**

| Name | Type | Description |
|---|---|---|
| `Services` | `IServiceCollection` | The DI service collection where SLI services are registered. |

---

### ServiceLevelIndicatorCoreServiceCollectionExtensions

**Declaration**

```csharp
public static class ServiceLevelIndicatorCoreServiceCollectionExtensions
```

**Methods**

| Signature | Returns | Description |
|---|---|---|
| `public static IServiceLevelIndicatorBuilder AddServiceLevelIndicator(this IServiceCollection services, Action<ServiceLevelIndicatorOptions> configureOptions)` | `IServiceLevelIndicatorBuilder` | Registers the `ServiceLevelIndicator` singleton, configures `ServiceLevelIndicatorOptions`, and returns a builder for additional setup. This extension is defined by the core package and used by the ASP.NET Core integration. |

---

### ServiceLevelIndicatorServiceCollectionExtensions

**Declaration**

```csharp
public static class ServiceLevelIndicatorServiceCollectionExtensions
```

Extensions on `IServiceLevelIndicatorBuilder` for opting into MVC support and enrichments.

**Methods**

| Signature | Returns | Description |
|---|---|---|
| `public static IServiceLevelIndicatorBuilder AddMvc(this IServiceLevelIndicatorBuilder builder)` | `IServiceLevelIndicatorBuilder` | Registers the MVC convention so that `[CustomerResourceId]` and `[Measure]` parameter attributes contribute endpoint metadata. **Required** for any MVC controller that uses these attributes. |
| `public static IServiceLevelIndicatorBuilder AddHttpMethod(this IServiceLevelIndicatorBuilder builder)` | `IServiceLevelIndicatorBuilder` | No-op compatibility method; `http.request.method` is emitted by default. |
| `public static IServiceLevelIndicatorBuilder ClassifyHttpOutcome(this IServiceLevelIndicatorBuilder builder, Func<HttpContext, SliOutcome> classifier)` | `IServiceLevelIndicatorBuilder` | Configures a global HTTP outcome classifier. The returned `SliOutcome` overrides the default status-code mapping for completed requests. |
| `public static IServiceLevelIndicatorBuilder Enrich(this IServiceLevelIndicatorBuilder builder, Action<WebEnrichmentContext> action)` | `IServiceLevelIndicatorBuilder` | Registers a synchronous enrichment delegate. |
| `public static IServiceLevelIndicatorBuilder EnrichAsync(this IServiceLevelIndicatorBuilder builder, Func<WebEnrichmentContext, CancellationToken, ValueTask> func)` | `IServiceLevelIndicatorBuilder` | Registers an asynchronous enrichment delegate. |

---

### ServiceLevelIndicatorApplicationBuilderExtensions

**Declaration**

```csharp
public static class ServiceLevelIndicatorApplicationBuilderExtensions
```

**Methods**

| Signature | Returns | Description |
|---|---|---|
| `public static IApplicationBuilder UseServiceLevelIndicator(this IApplicationBuilder app)` | `IApplicationBuilder` | Adds the `ServiceLevelIndicatorMiddleware` to the request pipeline. Place after routing and before endpoint execution so the endpoint is already resolved and request handling is measured. |

---

### EndpointBuilderExtensions

**Declaration**

```csharp
public static class EndpointBuilderExtensions
```

Minimal API integration.

**Methods**

| Signature | Returns | Description |
|---|---|---|
| `public static TBuilder AddServiceLevelIndicator<TBuilder>(this TBuilder builder, string? operation = default) where TBuilder : notnull, IEndpointConventionBuilder` | `TBuilder` | Marks a Minimal API endpoint for SLI emission. Scans the handler delegate's parameters for `[CustomerResourceId]` and `[Measure]` and projects them into endpoint metadata. When `operation` is `null`, the route template is used. |

---

### ServiceLevelIndicatorAttribute

**Declaration**

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ServiceLevelIndicatorAttribute : Attribute
```

Marks a controller or action as opting in to SLI emission. Required only when `ServiceLevelIndicatorOptions.AutomaticallyEmitted` is `false`. Also accepted as endpoint metadata by `EndpointBuilderExtensions.AddServiceLevelIndicator`.

**Properties**

| Name | Type | Description |
|---|---|---|
| `Operation` | `string?` | Optional override for the `Operation` tag. When `null`, the route template is used. |

**Constructors**

| Signature | Description |
|---|---|
| `public ServiceLevelIndicatorAttribute()` | Use the route template as the operation name. |
| `public ServiceLevelIndicatorAttribute(string operation)` | Use a custom operation name. |

---

### CustomerResourceIdAttribute

**Declaration**

```csharp
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class CustomerResourceIdAttribute : Attribute
```

Tag a route/handler parameter as the source of the `CustomerResourceId` tag. The middleware reads the matching route value at request time. Only one parameter per endpoint may carry this attribute (enforced at startup for MVC and at endpoint-finalize for Minimal APIs).

---

### MeasureAttribute

**Declaration**

```csharp
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class MeasureAttribute(string? name = default) : Attribute
```

Tag a route/handler parameter to emit it as an additional measurement attribute.

**Properties**

| Name | Type | Description |
|---|---|---|
| `Name` | `string?` | Optional attribute name; when `null`, the parameter name is used. |

---

### CustomerResourceIdMetadata

**Declaration**

```csharp
public sealed class CustomerResourceIdMetadata(string routeValueName)
```

Endpoint metadata produced by the MVC convention and the Minimal API endpoint finalizer. Identifies the route value that supplies the customer resource id.

**Properties**

| Name | Type | Description |
|---|---|---|
| `RouteValueName` | `string` | Name of the route value to read at request time. |

---

### MeasureMetadata

**Declaration**

```csharp
public sealed class MeasureMetadata(string routeValueName, string? attributeName = default)
```

Endpoint metadata for an additional measured route value.

**Properties**

| Name | Type | Description |
|---|---|---|
| `RouteValueName` | `string` | Route value to read. |
| `AttributeName` | `string` | Tag key emitted with the measurement (defaults to `RouteValueName`). |

---

### WebEnrichmentContext

**Declaration**

```csharp
public class WebEnrichmentContext : IEnrichmentContext
```

Concrete enrichment context for ASP.NET Core. Passed to every registered `IEnrichment<WebEnrichmentContext>` after the response status code is known but before the metric is recorded.

**Properties**

| Name | Type | Description |
|---|---|---|
| `Operation` | `string` | The current operation name. |
| `HttpContext` | `HttpContext` | The current request's `HttpContext`. |

**Methods**

| Signature | Returns | Description |
|---|---|---|
| `public void AddAttribute(string name, object? value)` | `void` | Append an attribute to the in-flight measurement. |
| `public void SetCustomerResourceId(string id)` | `void` | Override the customer resource id for this measurement. |

---

### IServiceLevelIndicatorFeature

**Declaration**

```csharp
public interface IServiceLevelIndicatorFeature
```

`HttpContext` feature exposing the current request's `MeasuredOperation`. Set by the middleware on entry and cleared on exit.

**Properties**

| Name | Type | Description |
|---|---|---|
| `MeasuredOperation` | `MeasuredOperation` | The in-flight measurement for the current request. |

---

### HttpContextExtensions

**Declaration**

```csharp
public static class HttpContextExtensions
```

**Methods**

| Signature | Returns | Description |
|---|---|---|
| `public static MeasuredOperation GetMeasuredOperation(this HttpContext context)` | `MeasuredOperation` | Returns the current measurement. Throws `InvalidOperationException` if the route is not configured to emit SLI metrics. |
| `public static bool TryGetMeasuredOperation(this HttpContext context, [MaybeNullWhen(false)] out MeasuredOperation measuredOperation)` | `bool` | Non-throwing variant. Returns `false` when no SLI feature is attached. |

---

## Middleware behavior summary

The `ServiceLevelIndicatorMiddleware` (registered by `UseServiceLevelIndicator()`):

1. Reads the resolved endpoint metadata. Skips emission when neither `AutomaticallyEmitted == true` nor a `ServiceLevelIndicatorAttribute` is present.
2. Resolves the operation name (custom override via attribute, else route template, else `"<METHOD> <unrouted>"`).
3. Collects `MeasureMetadata` route values into measurement attributes.
4. Starts a `MeasuredOperation` and attaches an `IServiceLevelIndicatorFeature` to `HttpContext.Features`.
5. Optionally overrides the customer id from a `CustomerResourceIdMetadata`-tagged route value.
6. Invokes the next middleware. On unhandled exceptions, sets status to 500 (when not started) and rethrows.
7. In `finally`, sets `Outcome`, `http.request.method`, and `http.response.status.code`, then runs all registered `IEnrichment<WebEnrichmentContext>` enrichments. Enrichment exceptions are caught and logged.
8. Disposes the `MeasuredOperation` (which records the metric) and removes the feature.

Throws `InvalidOperationException` if a second instance of the middleware tries to attach an SLI feature to the same request.

---

## Typical usage (MVC)

```csharp
using Trellis.ServiceLevelIndicators;

builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m.AddServiceLevelIndicatorInstrumentation().AddOtlpExporter());

builder.Services.AddServiceLevelIndicator(o =>
{
    o.LocationId = ServiceLevelIndicator.CreateLocationId("public", "westus3");
    // Automatic emission is enabled by default. Set AutomaticallyEmitted = false
    // to opt in endpoint-by-endpoint with AddServiceLevelIndicator().
})
.AddMvc();

var app = builder.Build();
app.UseRouting();
app.UseServiceLevelIndicator();
```

```csharp
[ApiController]
[Route("subscriptions/{subscriptionId}/widgets")]
public class WidgetsController : ControllerBase
{
    [HttpGet("{widgetId}")]
    public IActionResult Get(
        [CustomerResourceId] Guid subscriptionId,
        [Measure("widget.id")] Guid widgetId) => Ok();
}
```

## Typical usage (Minimal API)

```csharp
// Required only when AutomaticallyEmitted = false.
app.MapGet("/subs/{subscriptionId}/widgets/{widgetId}",
    ([CustomerResourceId] Guid subscriptionId, [Measure("widget.id")] Guid widgetId) => Results.Ok())
   .AddServiceLevelIndicator();
```

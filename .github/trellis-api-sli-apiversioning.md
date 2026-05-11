# Trellis.ServiceLevelIndicators.Asp.ApiVersioning — API Reference

**Package:** `Trellis.ServiceLevelIndicators.Asp.ApiVersioning`  
**Namespace:** `Trellis.ServiceLevelIndicators`  
**Purpose:** Adds the resolved API version as the `http.api.version` measurement dimension to every request emitted by [`Trellis.ServiceLevelIndicators.Asp`](trellis-api-sli-asp.md). Use only when your ASP.NET Core application also uses the [`Asp.Versioning`](https://github.com/dotnet/aspnet-api-versioning) package family. This package is an extension; it does **not** replace `Trellis.ServiceLevelIndicators.Asp`.

See also: [`trellis-api-sli.md`](trellis-api-sli.md), [`trellis-api-sli-asp.md`](trellis-api-sli-asp.md).

---

## Emitted dimension

| Tag | Value |
|---|---|
| `http.api.version` | The single resolved API version (e.g. `2023-06-06`), `"Neutral"` if the endpoint is API-version-neutral, `"Unspecified"` if no version was requested and no default was assumed, or `""` if the request is invalid or ambiguous. |

---

## Types

### ServiceLevelIndicatorServiceCollectionExtensions

**Declaration**

```csharp
public static class ServiceLevelIndicatorServiceCollectionExtensions
```

**Methods**

| Signature | Returns | Description |
|---|---|---|
| `public static IServiceLevelIndicatorBuilder AddApiVersion(this IServiceLevelIndicatorBuilder builder)` | `IServiceLevelIndicatorBuilder` | Registers `ApiVersionEnrichment` so every measured request emits an `http.api.version` tag. Chain this onto the builder returned by `AddServiceLevelIndicator(...)`. |

---

### ApiVersionEnrichment

**Declaration**

```csharp
public sealed class ApiVersionEnrichment : IEnrichment<WebEnrichmentContext>
```

Reads `HttpContext.ApiVersioningFeature` and emits `http.api.version`. Resolution rules:

| `RawRequestedApiVersions.Count` | Endpoint metadata | Emitted value |
|---|---|---|
| `1` | — | `RequestedApiVersion?.ToString()` (e.g. `"2023-06-06"`) |
| `> 1` | — | `""` (ambiguous) |
| `0` | `ApiVersionMetadata.IsApiVersionNeutral == true` | `"Neutral"` |
| `0` | otherwise | `"Unspecified"` |

This enrichment is registered as a singleton via `TryAddEnumerable`, so calling `AddApiVersion()` more than once is idempotent.

**Methods**

| Signature | Returns | Description |
|---|---|---|
| `public ValueTask EnrichAsync(WebEnrichmentContext context, CancellationToken cancellationToken)` | `ValueTask` | Implements `IEnrichment<WebEnrichmentContext>.EnrichAsync`. Adds `http.api.version` to the in-flight `MeasuredOperation`. |

---

## Typical usage

```csharp
using Asp.Versioning;
using Trellis.ServiceLevelIndicators;

builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(2023, 6, 6);
    o.ReportApiVersions = true;
}).AddMvc();

builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m.AddServiceLevelIndicatorInstrumentation().AddOtlpExporter());

builder.Services.AddServiceLevelIndicator(o =>
{
    o.LocationId = ServiceLevelIndicator.CreateLocationId("public", "westus3");
})
.AddMvc()
.AddApiVersion();   // ← from Trellis.ServiceLevelIndicators.Asp.ApiVersioning

var app = builder.Build();
app.UseRouting();
app.UseServiceLevelIndicator();
```

A request to a `[ApiVersion("2023-06-06")]` endpoint then emits the standard SLI tags plus `http.api.version=2023-06-06`. Requests to an `[ApiVersionNeutral]` endpoint emit `http.api.version=Neutral`.

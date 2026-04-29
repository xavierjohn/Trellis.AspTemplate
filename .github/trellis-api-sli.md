# Trellis.ServiceLevelIndicators — API Reference

**Package:** `Trellis.ServiceLevelIndicators`  
**Namespace:** `Trellis.ServiceLevelIndicators`  
**Purpose:** Core library for emitting Service Level Indicator (SLI) latency metrics in milliseconds via `System.Diagnostics.Metrics` / OpenTelemetry. Use directly in console apps, worker services, background jobs, and shared libraries that should stay independent from ASP.NET Core. For ASP.NET Core integration, see [`trellis-api-sli-asp.md`](trellis-api-sli-asp.md).

See also: [`trellis-api-sli-asp.md`](trellis-api-sli-asp.md), [`trellis-api-sli-apiversioning.md`](trellis-api-sli-apiversioning.md).

---

## Default emitted dimensions

Every measurement emits the following tags on instrument `operation.duration` (ms, `Histogram<long>`):

| Tag | Source |
|---|---|
| `CustomerResourceId` | `ServiceLevelIndicatorOptions.CustomerResourceId` (or per-call override) |
| `LocationId` | `ServiceLevelIndicatorOptions.LocationId` |
| `Operation` | Caller-supplied operation name |
| `activity.status.code` | Set on `MeasuredOperation` (`Unset` / `Ok` / `Error`) |

Additional attributes can be appended via `MeasuredOperation.AddAttribute(...)` or the `attributes` parameter of `Record`/`StartMeasuring`.

---

## Types

### ServiceLevelIndicator

**Declaration**

```csharp
public sealed class ServiceLevelIndicator : IDisposable
```

Singleton service that creates and records SLI metrics using an OpenTelemetry `Histogram<long>`. Resolved via DI from `IOptions<ServiceLevelIndicatorOptions>`.

**Disposal and meter ownership.** `ServiceLevelIndicator` owns the `Meter` only when it created it (i.e. when no `Meter` was supplied in `ServiceLevelIndicatorOptions`). On `Dispose()`, an internally-created meter is disposed; a user-supplied meter is never disposed by this class. Because the type is registered as a singleton via `AddSingleton<ServiceLevelIndicator>()`, the DI container disposes it at host shutdown — applications do not need to call `Dispose()` manually. `Dispose()` is idempotent. After disposal, recording is a silent no-op per OpenTelemetry convention.

**Constants**

| Name | Type | Value | Description |
|---|---|---|---|
| `DefaultMeterName` | `const string` | `"ServiceLevelIndicator"` | Default meter name used when no `Meter` is supplied in options. |

**Properties**

| Name | Type | Description |
|---|---|---|
| `ServiceLevelIndicatorOptions` | `ServiceLevelIndicatorOptions` | The options instance the indicator was constructed with. |

**Constructors**

| Signature | Description |
|---|---|
| `public ServiceLevelIndicator(IOptions<ServiceLevelIndicatorOptions> options)` | Validates `LocationId` and `DurationInstrumentName`, creates the default meter (`"ServiceLevelIndicator"` + assembly version) when one isn't supplied, and creates the `operation.duration` histogram. |

**Methods**

| Signature | Returns | Description |
|---|---|---|
| `public void Record(string operation, long elapsedTime, params KeyValuePair<string, object?>[] attributes)` | `void` | Records a measurement using the configured default `CustomerResourceId`. |
| `public void Record(string operation, string customerResourceId, long elapsedTime, params KeyValuePair<string, object?>[] attributes)` | `void` | Records a measurement with an explicit `CustomerResourceId`. |
| `public MeasuredOperation StartMeasuring(string operation, params KeyValuePair<string, object?>[] attributes)` | `MeasuredOperation` | Starts a stopwatch-backed measurement; dispose the returned object to record the elapsed time as a metric. |
| `public void Dispose()` | `void` | Disposes the internally-created `Meter` if this instance created it; never disposes a user-supplied meter. Idempotent. Normally invoked by the DI container at host shutdown. |
| `public static string CreateCustomerResourceId(Guid serviceId)` | `string` | Builds a `ServiceTreeId://<guid>` customer resource id. Throws `ArgumentNullException` if `serviceId` is `Guid.Empty`. |
| `public static string CreateLocationId(string cloud, string? region = null, string? zone = null)` | `string` | Builds an `ms-loc://az/<cloud>/<region>/<zone>` location id, omitting empty segments. |

---

### ServiceLevelIndicatorOptions

**Declaration**

```csharp
public class ServiceLevelIndicatorOptions
```

Bound via `IOptions<ServiceLevelIndicatorOptions>`. `LocationId` and `DurationInstrumentName` are required (validated in the `ServiceLevelIndicator` constructor).

**Properties**

| Name | Type | Default | Description |
|---|---|---|---|
| `Meter` | `Meter` | `null` (auto-created) | The meter used to create the duration histogram. Set during startup; read once when the `ServiceLevelIndicator` singleton is constructed. |
| `CustomerResourceId` | `string` | `"Unset"` | Default `CustomerResourceId` tag value (per-tenant / per-subscription identifier). Can be overridden on each call. |
| `LocationId` | `string` | `""` | **Required.** Where the service is running (e.g. `ms-loc://az/public/westus3`). Must be non-empty. |
| `DurationInstrumentName` | `string` | `"operation.duration"` | **Required.** Histogram instrument name. Must be non-empty. |
| `ActivityStatusCodeAttributeName` | `string` | `"activity.status.code"` | Tag name used to emit the operation's `ActivityStatusCode`. |
| `AutomaticallyEmitted` | `bool` | `true` | When `false`, only operations explicitly opted-in (e.g. via the `[ServiceLevelIndicator]` attribute in the ASP package) emit metrics. |

---

### MeasuredOperation

**Declaration**

```csharp
public class MeasuredOperation : IDisposable
```

Represents an in-flight measurement. The stopwatch starts in the constructor; disposing records the elapsed milliseconds plus the `activity.status.code` tag.

**Properties**

| Name | Type | Description |
|---|---|---|
| `Operation` | `string` | Operation name emitted as the `Operation` tag. |
| `CustomerResourceId` | `string` | Customer resource id emitted as the `CustomerResourceId` tag. |
| `Attributes` | `List<KeyValuePair<string, object?>>` | Mutable list of additional OpenTelemetry attributes to emit. |

**Constructors**

| Signature | Description |
|---|---|
| `public MeasuredOperation(ServiceLevelIndicator sli, string operation, params KeyValuePair<string, object?>[] attributes)` | Uses the default `CustomerResourceId` from the indicator's options. |
| `public MeasuredOperation(ServiceLevelIndicator sli, string operation, string customerResourceId, params KeyValuePair<string, object?>[] attributes)` | Uses an explicit `CustomerResourceId`. |

**Methods**

| Signature | Returns | Description |
|---|---|---|
| `public void SetActivityStatusCode(ActivityStatusCode code)` | `void` | Sets the `ActivityStatusCode` recorded with the measurement. Default is `Unset`. |
| `public void AddAttribute(string attribute, object? value)` | `void` | Appends a custom attribute to be emitted with the measurement. |
| `public void Dispose()` | `void` | Stops the stopwatch and records the metric. Idempotent. |
| `protected virtual void Dispose(bool disposing)` | `void` | Standard dispose pattern hook. |

---

### IEnrichmentContext

**Declaration**

```csharp
public interface IEnrichmentContext
```

Marker contract implemented by enrichment contexts (e.g. `WebEnrichmentContext` in the ASP package). Lets enrichments add attributes and override the customer id without coupling to a specific host.

**Properties**

| Name | Type | Description |
|---|---|---|
| `Operation` | `string` | The operation name being measured. |

**Methods**

| Signature | Returns | Description |
|---|---|---|
| `void SetCustomerResourceId(string id)` | `void` | Overrides the customer resource identifier for this measurement. |
| `void AddAttribute(string name, object? value)` | `void` | Adds a custom attribute to the measurement. |

---

### IEnrichment&lt;T&gt;

**Declaration**

```csharp
public interface IEnrichment<T>
    where T : IEnrichmentContext
```

Strategy contract for adding attributes to a measurement context. Multiple enrichments are invoked in DI registration order.

**Methods**

| Signature | Returns | Description |
|---|---|---|
| `ValueTask EnrichAsync(T context, CancellationToken cancellationToken)` | `ValueTask` | Add attributes (or override the customer id) on the context. Exceptions thrown here are caught and logged by the host (in the ASP package). |

---

### ServiceLevelIndicatorMeterProviderBuilderExtensions

**Declaration**

```csharp
public static class ServiceLevelIndicatorMeterProviderBuilderExtensions
```

Helpers for wiring the SLI meter into an OpenTelemetry pipeline.

**Methods**

| Signature | Returns | Description |
|---|---|---|
| `public static MeterProviderBuilder AddServiceLevelIndicatorInstrumentation(this MeterProviderBuilder builder)` | `MeterProviderBuilder` | Adds the default meter `"ServiceLevelIndicator"`. |
| `public static MeterProviderBuilder AddServiceLevelIndicatorInstrumentation(this MeterProviderBuilder builder, string meterName)` | `MeterProviderBuilder` | Adds a specific meter by name. Used when the application configures `ServiceLevelIndicatorOptions.Meter` with a custom meter. |
| `public static MeterProviderBuilder AddServiceLevelIndicatorInstrumentation(this MeterProviderBuilder builder, Meter meter)` | `MeterProviderBuilder` | Convenience overload that reads the meter's name. |

---

## Typical usage

```csharp
using Trellis.ServiceLevelIndicators;

builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m
        .AddServiceLevelIndicatorInstrumentation()
        .AddOtlpExporter());

builder.Services.AddSingleton<ServiceLevelIndicator>();
builder.Services.Configure<ServiceLevelIndicatorOptions>(o =>
{
    o.LocationId = ServiceLevelIndicator.CreateLocationId("public", "westus3");
});

// In a worker / job:
async Task DoWorkAsync(ServiceLevelIndicator sli, CancellationToken ct)
{
    using var op = sli.StartMeasuring("ProcessBatch");
    try
    {
        await ProcessAsync(ct);
        op.SetActivityStatusCode(ActivityStatusCode.Ok);
    }
    catch
    {
        op.SetActivityStatusCode(ActivityStatusCode.Error);
        throw;
    }
}
```

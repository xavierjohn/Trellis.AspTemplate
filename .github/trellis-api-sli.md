# Trellis.ServiceLevelIndicators — API Reference

**Package:** `Trellis.ServiceLevelIndicators`  
**Namespace:** `Trellis.ServiceLevelIndicators`  
**Purpose:** Core library for emitting Service Level Indicator (SLI) latency metrics in milliseconds via `System.Diagnostics.Metrics` / OpenTelemetry. Use directly in console apps, worker services, background jobs, and shared libraries that should stay independent from ASP.NET Core. For ASP.NET Core integration, see [`trellis-api-sli-asp.md`](trellis-api-sli-asp.md).

See also: [`trellis-api-sli-asp.md`](trellis-api-sli-asp.md), [`trellis-api-sli-apiversioning.md`](trellis-api-sli-apiversioning.md).

---

## Default emitted dimensions

`StartMeasuring(...)` scopes emit the following tags on instrument `operation.duration` (ms, `Histogram<long>`) when the returned `MeasuredOperation` is disposed:

| Tag | Source |
|---|---|
| `CustomerResourceId` | `ServiceLevelIndicatorOptions.CustomerResourceId` (or per-call override) |
| `LocationId` | `ServiceLevelIndicatorOptions.LocationId` |
| `Operation` | Caller-supplied operation name |
| `Outcome` | `Success`, `Failure`, `ClientError`, or `Ignored` |

Additional attributes can be appended via `MeasuredOperation.AddAttribute(...)` or the `attributes` parameter of `Record`/`StartMeasuring`. Custom attributes must not reuse `CustomerResourceId`, `LocationId`, `Operation`, `Outcome`, `activity.status.code`, `http.request.method`, `http.response.status.code`, or any other attribute name already present on the measurement. Direct `Record(...)` calls emit `CustomerResourceId`, `LocationId`, `Operation`, `Outcome`, and any supplied custom attributes.

---

## Types

### ServiceLevelIndicator

**Declaration**

```csharp
public sealed class ServiceLevelIndicator : IDisposable
```

Singleton service that creates and records SLI metrics using an OpenTelemetry `Histogram<long>`. Resolved via DI from `IOptions<ServiceLevelIndicatorOptions>`.

**Disposal and meter ownership.** `ServiceLevelIndicator` owns the `Meter` only when it created it (i.e. when no `Meter` was supplied in `ServiceLevelIndicatorOptions`). On `Dispose()`, an internally-created meter is disposed; a user-supplied meter is never disposed by this class. Because `AddServiceLevelIndicator(...)` registers the type as a singleton, the DI container disposes it at host shutdown — applications do not need to call `Dispose()` manually. `Dispose()` is idempotent. After disposal, recording is a silent no-op per OpenTelemetry convention.

**Constants**

| Name | Type | Value | Description |
|---|---|---|---|
| `DefaultMeterName` | `const string` | `"Trellis.SLI"` | Default meter name used when no `Meter` is supplied in options. |

**Properties**

| Name | Type | Description |
|---|---|---|
| `ServiceLevelIndicatorOptions` | `ServiceLevelIndicatorOptions` | The options instance the indicator was constructed with. |

**Constructors**

| Signature | Description |
|---|---|
| `public ServiceLevelIndicator(IOptions<ServiceLevelIndicatorOptions> options)` | Validates `LocationId` and `DurationInstrumentName`, creates the default meter (`"Trellis.SLI"` + assembly version) when one isn't supplied, and creates the `operation.duration` histogram. |

**Methods**

| Signature | Returns | Description |
|---|---|---|
| `public void Record(string operation, long elapsedTime, params KeyValuePair<string, object?>[] attributes)` | `void` | Records a measurement using the configured default `CustomerResourceId` and default outcome `Ignored`. |
| `public void Record(string operation, long elapsedTime, SliOutcome outcome, params KeyValuePair<string, object?>[] attributes)` | `void` | Records a measurement using the configured default `CustomerResourceId` and explicit outcome. |
| `public void Record(string operation, string customerResourceId, long elapsedTime, params KeyValuePair<string, object?>[] attributes)` | `void` | Records a measurement with an explicit `CustomerResourceId` and default outcome `Ignored`. |
| `public void Record(string operation, string customerResourceId, long elapsedTime, SliOutcome outcome, params KeyValuePair<string, object?>[] attributes)` | `void` | Records a measurement with an explicit `CustomerResourceId` and outcome. |
| `public MeasuredOperation StartMeasuring(string operation, params KeyValuePair<string, object?>[] attributes)` | `MeasuredOperation` | Starts a stopwatch-backed measurement; dispose the returned object to record the elapsed time as a metric. |
| `public void Measure(string operation, Action action, params KeyValuePair<string, object?>[] attributes)` | `void` | Measures a synchronous operation and infers `Success`, `Failure`, or `Ignored` for `OperationCanceledException`. |
| `public T Measure<T>(string operation, Func<T> action, params KeyValuePair<string, object?>[] attributes)` | `T` | Measures a synchronous operation and returns its result. |
| `public Task MeasureAsync(string operation, Func<Task> action, params KeyValuePair<string, object?>[] attributes)` | `Task` | Measures an asynchronous operation and infers outcome before rethrowing exceptions. |
| `public Task<T> MeasureAsync<T>(string operation, Func<Task<T>> action, params KeyValuePair<string, object?>[] attributes)` | `Task<T>` | Measures an asynchronous operation and returns its result. |
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
| `CustomerResourceId` | `string` | `"Unknown"` | Default `CustomerResourceId` tag value. Can be overridden on each call. When emitted as `Unknown`, the diagnostic counter `sli.diagnostics.unknown_customer_resource_id` is incremented. |
| `LocationId` | `string` | `""` | **Required.** Where the service is running (e.g. `ms-loc://az/public/westus3`). Must be non-empty. |
| `DurationInstrumentName` | `string` | `"operation.duration"` | **Required.** Histogram instrument name. Must be non-empty. |
| `ActivityStatusCodeAttributeName` | `string` | `"activity.status.code"` | Obsolete compatibility option. Activity status is no longer emitted as a metric dimension. |
| `AutomaticallyEmitted` | `bool` | `true` | When `false`, only operations explicitly opted-in (e.g. via the `[ServiceLevelIndicator]` attribute in the ASP package) emit metrics. |

---

### DI registration

**Declaration**

```csharp
public static IServiceLevelIndicatorBuilder AddServiceLevelIndicator(
    this IServiceCollection services,
    Action<ServiceLevelIndicatorOptions> configureOptions)
```

Registers `ServiceLevelIndicator` as a singleton and configures `ServiceLevelIndicatorOptions`. This lives in the core package, so console apps, workers, and shared libraries do not need to reference the ASP package just to use DI. The returned `IServiceLevelIndicatorBuilder` exposes `Services` and is the chaining point for host packages such as `.AddMvc()`, `.AddHttpMethod()`, and `.AddApiVersion()`.

---

### MeasuredOperation

**Declaration**

```csharp
public class MeasuredOperation : IDisposable
```

Represents an in-flight measurement. The stopwatch starts in the constructor; disposing records the elapsed milliseconds plus the `Outcome` tag.

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
| `public void SetOutcome(SliOutcome outcome)` | `void` | Sets the SLI outcome recorded with the measurement. Default is `Ignored`. |
| `public void SetActivityStatusCode(ActivityStatusCode code)` | `void` | Obsolete compatibility shim that maps `Ok` to `Success`, `Error` to `Failure`, and other values to `Ignored`. |
| `public void AddAttribute(string attribute, object? value)` | `void` | Appends a custom attribute to be emitted with the measurement. Throws if the name collides with a reserved SLI tag. |
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
| `public static MeterProviderBuilder AddServiceLevelIndicatorInstrumentation(this MeterProviderBuilder builder)` | `MeterProviderBuilder` | Adds the default meter `"Trellis.SLI"`. |
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

builder.Services.AddServiceLevelIndicator(o =>
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
        op.SetOutcome(SliOutcome.Success);
    }
    catch
    {
        op.SetOutcome(SliOutcome.Failure);
        throw;
    }
}
```

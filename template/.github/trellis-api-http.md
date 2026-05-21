# Trellis.Http — API Reference

**Package:** `Trellis.Http`  
**Namespace:** `Trellis.Http`  
**Purpose:** Bridge `Task<HttpResponseMessage>` into Trellis `Result` pipelines and deserialize JSON payloads into `Result<T>` and `Result<Maybe<T>>`.

## Types

### `HttpResponseExtensions`

**Declaration**

```csharp
public static partial class HttpResponseExtensions
```

| Name | Type | Description |
| --- | --- | --- |
| — | — | No public properties. |

| Signature | Returns | Description |
| --- | --- | --- |
| `ToResultAsync(this Task<HttpResponseMessage> response, Func<HttpStatusCode, Error?>? statusMap = null)` | `Task<Result<HttpResponseMessage>>` | Converts the response task to `Result<HttpResponseMessage>`. With no `statusMap`, 2xx statuses become `Ok(response)` and non-2xx statuses use the strict default mapper below. A non-null custom error becomes `Fail(error)` and disposes the response; `null` passes the response through. |
| `ToResultAsync(this Task<HttpResponseMessage> response, Func<HttpResponseMessage, CancellationToken, Task<Error?>> mapper, CancellationToken ct = default)` | `Task<Result<HttpResponseMessage>>` | Body-aware mapping for non-success responses. Return `null` to keep the response on the success track; return an `Error` to fail and dispose the response. |
| `HandleNotFoundAsync(this Task<HttpResponseMessage> response, Error.NotFound error)` | `Task<Result<HttpResponseMessage>>` | Maps `404` to `Fail(error)` and disposes the response; any other status becomes `Ok(response)`. |
| `HandleConflictAsync(this Task<HttpResponseMessage> response, Error.Conflict error)` | `Task<Result<HttpResponseMessage>>` | Maps `409` to `Fail(error)` and disposes the response; any other status becomes `Ok(response)`. |
| `HandleUnauthorizedAsync(this Task<HttpResponseMessage> response, Error.AuthenticationRequired error)` | `Task<Result<HttpResponseMessage>>` | Maps `401` to `Fail(error)` and disposes the response; any other status becomes `Ok(response)`. |
| `ReadJsonAsync<T>(this Task<Result<HttpResponseMessage>> response, JsonTypeInfo<T> jsonTypeInfo, CancellationToken ct = default) where T : notnull` | `Task<Result<T>>` | Terminal required-body helper. Upstream failures short-circuit; successful responses are disposed after reading. Empty, null, non-success, or invalid JSON payloads fail with `Error.Unexpected`. |
| `ReadJsonMaybeAsync<T>(this Task<Result<HttpResponseMessage>> response, JsonTypeInfo<T> jsonTypeInfo, CancellationToken ct = default) where T : notnull` | `Task<Result<Maybe<T>>>` | Terminal optional-body helper. Upstream failures short-circuit; `204`, `205`, empty body, and JSON `null` become `Ok(Maybe.None)`. Invalid JSON is not caught. |
| `ReadJsonOrNoneOn404Async<T>(this Task<HttpResponseMessage> response, JsonTypeInfo<T> jsonTypeInfo, CancellationToken ct = default) where T : notnull` | `Task<Result<Maybe<T>>>` | Terminal optional-resource helper. `404` becomes `Ok(Maybe.None)`; other non-2xx statuses use strict default mapping. |

## Strict default status mapping

Bare `ToResultAsync()` is strict by default: non-2xx responses become typed Trellis failures instead of staying on the success track. The mapper preserves useful header context.

| Status | Becomes |
| --- | --- |
| `400` | `Error.InvalidInput.ForRule("http.bad_request")` |
| `401` | `new Error.AuthenticationRequired()` |
| `403` | `new Error.Forbidden("http.forbidden")` |
| `404` | `new Error.NotFound(resource)` |
| `405` with `Allow` | `new Error.TransportFault(new HttpError.MethodNotAllowed(allow))` |
| `406` | `new Error.TransportFault(new HttpError.NotAcceptable(EquatableArray<string>.Empty))` |
| `409` | `new Error.Conflict(null, "http.conflict")` |
| `410` | `new Error.Gone(resource)` |
| `412` | `new Error.TransportFault(new HttpError.PreconditionFailed(resource, PreconditionKind.IfMatch))` |
| `413` | `new Error.TransportFault(new HttpError.ContentTooLarge())` |
| `415` | `new Error.TransportFault(new HttpError.UnsupportedMediaType(EquatableArray<string>.Empty))` |
| `416` with known `Content-Range` length | `new Error.TransportFault(new HttpError.RangeNotSatisfiable(length, unit))` |
| `422` | `Error.InvalidInput.ForRule("http.unprocessable_content")` |
| `428` | `new Error.TransportFault(new HttpError.PreconditionRequired(PreconditionKind.IfMatch))` |
| `429` | `new Error.RateLimited(retryAdvice)`; parses `Retry-After` into `RetryAdvice` when present |
| `501` | `new Error.Unexpected("not_implemented")` |
| `503` | `new Error.Unavailable(reasonCode, retryAdvice)`; parses `Retry-After` into `RetryAdvice` when present |
| other / fallback `5xx` | `new Error.Unexpected(Guid.NewGuid().ToString("N"))` |

`405` without `Allow` and `416` without a known range length also fall back to `Error.Unexpected`. HTTP-specific cases are wrapped in `Error.TransportFault`; `HttpError` is the closed union from `Trellis.Http.Abstractions`.

## Enums

This package exposes no public enums.

## Code examples

### Read a required JSON payload

```csharp
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Trellis;
using Trellis.Http;

public sealed record TodoDto(int Id, string Title);

[JsonSerializable(typeof(TodoDto))]
public partial class AppJsonContext : JsonSerializerContext
{
}

public static class TodoClient
{
    public static Task<Result<TodoDto>> GetTodoAsync(HttpClient httpClient, CancellationToken cancellationToken) =>
        httpClient.GetAsync("/todos/1", cancellationToken)
            .ToResultAsync()
            .ReadJsonAsync(AppJsonContext.Default.TodoDto, cancellationToken);
}
```

### Read an optional JSON payload

```csharp
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Trellis;
using Trellis.Http;

public sealed record ProfileDto(string DisplayName);

[JsonSerializable(typeof(ProfileDto))]
public partial class ProfileJsonContext : JsonSerializerContext
{
}

public static class ProfileClient
{
    public static Task<Result<Maybe<ProfileDto>>> GetProfileAsync(HttpClient httpClient, CancellationToken cancellationToken) =>
        httpClient.GetAsync("/profile", cancellationToken)
            .ToResultAsync()
            .ReadJsonMaybeAsync(ProfileJsonContext.Default.ProfileDto, cancellationToken);
}
```

## Cross-references

- [trellis-api-results.md](trellis-api-results.md)
- [trellis-api-asp.md](trellis-api-asp.md)

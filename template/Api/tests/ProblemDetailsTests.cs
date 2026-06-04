namespace Api.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[Collection(TestWebApplicationFactoryCollectionFixture.Id)]
public class ProblemDetailsTests
{
    private readonly TestWebApplicationFactoryFixture _factory;

    public ProblemDetailsTests(TestWebApplicationFactoryFixture factory, ITestOutputHelper output)
    {
        _factory = factory;
        _factory.OutputHelper = output;
    }

    // The UseExceptionHandler middleware (paired with AddProblemDetails) converts unhandled
    // exceptions into a 500 ProblemDetails body. The CustomizeProblemDetails callback in
    // DependencyInjection rewrites the detail message to a support-friendly form and attaches
    // the active trace id so operators can correlate the client error with server-side logs.
    [Fact]
    public async Task Unhandled_exceptions_become_500_problem_details_with_trace_id()
    {
        var client = _factory.CreateClient();
        var traceId = "0af7651916cd43dd8448eb211c80319c";
        var traceParent = $"00-{traceId}-00f067aa0ba902b7-01";
        var request = new HttpRequestMessage(HttpMethod.Get, "api/Todos/throw?api-version=2026-03-26");
        request.Headers.Add("traceparent", traceParent);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsWithTrace>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        problem.Status.Should().Be(StatusCodes.Status500InternalServerError);
        problem.Title.Should().Be("An error occurred while processing your request.");
        problem.Detail.Should().Be("An error occurred in our API. Please refer the trace id with our support team.");
        problem.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.6.1");
        problem.TraceId.Should().Contain(traceId);
    }

    // The UseStatusCodePages middleware (paired with AddProblemDetails) converts the empty-body
    // 415 short-circuit emitted by the input formatter into a ProblemDetails body. Without it,
    // the response would be a bare "415 Unsupported Media Type" with no Content-Type or body —
    // the gap the production-failure audit surfaced.
    [Fact]
    public async Task Wrong_content_type_returns_415_problem_details()
    {
        var client = _factory.CreateClient();
        var body = new StringContent("not json", Encoding.UTF8, "text/plain");
        var request = new HttpRequestMessage(HttpMethod.Post, "api/Todos?api-version=2026-03-26") { Content = body };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsWithTrace>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        problem.Status.Should().Be(StatusCodes.Status415UnsupportedMediaType);
        problem.TraceId.Should().NotBeNullOrEmpty();
    }

    // The /fault demo endpoint returns new Error.Unexpected("todos_fault", "TODOS-FAULT-001").ToHttpResponse(),
    // demonstrating the typed Error.Unexpected -> RFC 9457 ProblemDetails mapping. The fault id
    // is the support-ticket correlation key callers quote when reporting an issue; the reason
    // code is the stable programmatic identifier the framework guarantees to surface verbatim.
    // This test pins both extensions so a framework-side change to where Error.Unexpected lands
    // its payload does not silently regress the showcase pattern the template is teaching.
    [Fact]
    public async Task Fault_endpoint_returns_500_with_stable_fault_id_and_reason_code()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("api/Todos/fault?api-version=2026-03-26", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        doc.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status500InternalServerError);
        doc.GetProperty("code").GetString().Should().Be("todos_fault",
            "the Error.Unexpected reason code is the stable programmatic identifier the framework guarantees to surface");
        doc.GetProperty("faultId").GetString().Should().Be("TODOS-FAULT-001",
            "the fault id is the support-ticket correlation key clients quote when reporting an issue");
        doc.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }

    // The 405 short-circuit emitted by routing must reach UseStatusCodePages so the body
    // carries a ProblemDetails payload. The CustomizeProblemDetails callback additionally
    // surfaces the Allow header (set by routing per RFC 9110 §15.5.6) as a structured
    // "allow" extension so clients that ignore headers still discover the supported methods.
    // We PATCH /api/Todos/{id} (anything other than the "throw" subroute) so multiple
    // methods land in the Allow list, exercising the split-and-array branch of the callback.
    [Fact]
    public async Task Wrong_method_returns_405_problem_details_with_allow_extension()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Patch, "api/Todos/00000000-0000-0000-0000-000000000001?api-version=2026-03-26");

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        response.Content.Headers.Allow.Should().NotBeEmpty(
            "RFC 9110 §15.5.6 requires 405 responses to list supported methods via the Allow representation header");

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        doc.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status405MethodNotAllowed);
        doc.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();

        doc.TryGetProperty("allow", out var allow).Should().BeTrue("the 405 body should echo the Allow header as a structured array");
        allow.ValueKind.Should().Be(JsonValueKind.Array);
        var allowValues = allow.EnumerateArray().Select(e => e.GetString()).ToArray();
        allowValues.Should().Contain("GET").And.Contain("PUT").And.Contain("DELETE");
    }

    // The 404 emitted by routing when no route template matches must reach UseStatusCodePages.
    // Without it, the response is a bare "404 Not Found" with no Content-Type or body — clients
    // cannot distinguish a route miss from any other dead-air response.
    [Fact]
    public async Task Route_miss_returns_404_problem_details_with_trace_id()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("api/this-route-does-not-exist", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsWithTrace>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        problem.Status.Should().Be(StatusCodes.Status404NotFound);
        problem.TraceId.Should().NotBeNullOrEmpty();
    }

    public class ProblemDetailsWithTrace : ProblemDetails
    {
        public string TraceId { get; set; } = string.Empty;
    }
}

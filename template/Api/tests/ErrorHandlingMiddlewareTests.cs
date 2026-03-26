namespace Api.Tests;

using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[Collection(TestWebApplicationFactoryCollectionFixture.Id)]
public class ErrorHandlingMiddlewareTests
{
    private readonly TestWebApplicationFactoryFixture _factory;

    public ErrorHandlingMiddlewareTests(TestWebApplicationFactoryFixture factory, ITestOutputHelper output)
    {
        _factory = factory;
        _factory.OutputHelper = output;
    }

    [Fact]
    public async Task Unhandled_exceptions_are_handled_by_middleware()
    {
        // Arrange
        var client = _factory.CreateClient();
        var traceId = "0af7651916cd43dd8448eb211c80319c";
        var traceParent = $"00-{traceId}-00f067aa0ba902b7-01";
        var request = new HttpRequestMessage(HttpMethod.Get, "api/Todos/throw?api-version=2026-03-26");
        request.Headers.Add("traceparent", traceParent);

        // Act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);
        var problemDetailsWithTrace = await response.Content.ReadFromJsonAsync<ProblemDetailsWithTrace>(TestContext.Current.CancellationToken);
        problemDetailsWithTrace.Should().NotBeNull();
        Assert.NotNull(problemDetailsWithTrace);
        problemDetailsWithTrace.Status.Should().Be(StatusCodes.Status500InternalServerError);
        problemDetailsWithTrace.Title.Should().Be("An error occurred while processing your request.");
        problemDetailsWithTrace.Detail.Should().Be("An error occurred in our API. Please refer the trace id with our support team.");
        problemDetailsWithTrace.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.6.1");
        problemDetailsWithTrace.TraceId.Should().Contain(traceId);
    }

    public class ProblemDetailsWithTrace : ProblemDetails
    {
        public string TraceId { get; set; } = string.Empty;
    }
}

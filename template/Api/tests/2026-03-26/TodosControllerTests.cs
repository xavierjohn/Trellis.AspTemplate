namespace Api.Tests._2026_03_26;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Trellis.Testing;
using Trellis.Testing.AspNetCore;

[Collection(TestWebApplicationFactoryCollectionFixture.Id)]
public class TodosControllerTests
{
    private readonly TestWebApplicationFactoryFixture _factory;
    private const string BaseUrl = "api/Todos?api-version=2026-03-26";
    private const string VersionParam = "api-version=2026-03-26";

    public TodosControllerTests(TestWebApplicationFactoryFixture factory, ITestOutputHelper output)
    {
        _factory = factory;
        _factory.OutputHelper = output;
    }

    private HttpClient CreateClient(string actorId = "test-user", params string[] permissions) =>
        _factory.CreateClientWithActor(actorId, permissions);

    // The [Idempotent] attribute on POST /todos opts the Create endpoint into the IETF
    // Idempotency-Key middleware. On a same-key retry with a matching request fingerprint
    // the middleware replays the captured first-response snapshot byte-for-byte and adds
    // the "Idempotent-Replayed" response header so clients can tell a retry from a fresh
    // invocation. This test pins the showcase contract end-to-end: a duplicate POST does
    // NOT create a second todo, the replayed body and Location match the original, and the
    // replay marker header is present on the second response.
    [Fact]
    public async Task Create_with_same_Idempotency_Key_replays_first_response_and_does_not_create_duplicate()
    {
        var client = CreateClient("owner-1", "todos:create", "todos:read");
        var body = new { title = "Replay me", dueDate = DateTime.UtcNow.AddDays(7), tag = "replay" };
        var key = Guid.NewGuid().ToString();

        var first = await client.PostJsonWithKeyAsync(BaseUrl, body, key, TestContext.Current.CancellationToken);
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        first.Headers.Contains("Idempotent-Replayed").Should().BeFalse(
            "the first call is a fresh handler invocation, not a replay");
        var firstTodo = await first.Content.ReadAsAsyncWithAssertion<TodoResponse>();
        var firstLocation = first.Headers.Location;

        var second = await client.PostJsonWithKeyAsync(BaseUrl, body, key, TestContext.Current.CancellationToken);

        second.StatusCode.Should().Be(HttpStatusCode.Created);
        second.Headers.Contains("Idempotent-Replayed").Should().BeTrue(
            "the second call must carry the replay marker so the client can distinguish a cached snapshot from a fresh invocation");
        second.Headers.Location?.OriginalString.Should().Be(firstLocation?.OriginalString,
            "a replayed snapshot must reproduce the original Location header byte-for-byte");
        var secondTodo = await second.Content.ReadAsAsyncWithAssertion<TodoResponse>();
        secondTodo.Id.Should().Be(firstTodo.Id,
            "the duplicate POST must NOT create a second todo — replaying preserves the original id");
    }

    [Fact]
    public async Task Create_valid_todo_returns_201_with_location()
    {
        var client = CreateClient("user-1", "todos:create", "todos:read");
        var dueDate = DateTime.UtcNow.AddDays(7);
        var body = new { title = "Buy groceries", dueDate, tag = "shopping" };

        var response = await client.PostJsonIdempotentAsync(BaseUrl, body, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        // The Location header must round-trip the requested api-version so the follow-up GET
        // dereferences correctly. CreatedAtVersionedRoute (Trellis.Asp.ApiVersioning) injects
        // this automatically — without it the URL would 404 under query-string versioning.
        response.Headers.Location!.OriginalString.Should().Contain("api-version=2026-03-26");

        var todo = await response.Content.ReadAsAsyncWithAssertion<TodoResponse>();
        todo.Title.Should().Be("Buy groceries");
        todo.Status.Should().Be("Active");
        todo.Tag.Should().Be("shopping");
        todo.CreatedByActorId.Should().Be("user-1");
    }

    [Fact]
    public async Task Create_todo_without_tag_returns_201()
    {
        var client = CreateClient("user-1", "todos:create");
        var body = new { title = "No tag todo", dueDate = DateTime.UtcNow.AddDays(3) };

        var response = await client.PostJsonIdempotentAsync(BaseUrl, body, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var todo = await response.Content.ReadAsAsyncWithAssertion<TodoResponse>();
        todo.Tag.Should().BeNull();
    }

    [Fact]
    public async Task Create_todo_with_invalid_title_returns_422()
    {
        var client = CreateClient("user-1", "todos:create");
        var body = new { title = "", dueDate = DateTime.UtcNow.AddDays(3) };

        var response = await client.PostJsonIdempotentAsync(BaseUrl, body, TestContext.Current.CancellationToken);

        // 422 (not 400) — the empty title fails Title's TryCreate value-object validation at the
        // binder seam, which Trellis maps to 422 Unprocessable Content (Trellis.Asp PR #477,
        // 3.0.0-alpha.252+). 400 is reserved for framework-level errors (missing api-version,
        // malformed JSON, unbound route parameter), not business validation.
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetById_existing_todo_returns_200()
    {
        var client = CreateClient("user-1", "todos:create", "todos:read");
        var dueDate = DateTime.UtcNow.AddDays(5);
        var createResponse = await client.PostJsonIdempotentAsync(BaseUrl, new { title = "Fetch me", dueDate }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();

        var response = await client.GetAsync($"api/Todos/{created.Id}?{VersionParam}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var todo = await response.Content.ReadAsAsyncWithAssertion<TodoResponse>();
        todo.Id.Should().Be(created.Id);
        todo.Title.Should().Be("Fetch me");
    }

    [Fact]
    public async Task GetById_nonexistent_returns_404()
    {
        var client = CreateClient("user-1", "todos:read");
        var fakeId = Guid.NewGuid();

        var response = await client.GetAsync($"api/Todos/{fakeId}?{VersionParam}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(404);
    }

    [Fact]
    public async Task Complete_own_todo_returns_200()
    {
        var client = CreateClient("owner-1", "todos:create", "todos:complete");
        var dueDate = DateTime.UtcNow.AddDays(2);
        var createResponse = await client.PostJsonIdempotentAsync(BaseUrl, new { title = "Complete me", dueDate }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();

        var response = await client.PostAsync($"api/Todos/{created.Id}/complete?{VersionParam}", null, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var completed = await response.Content.ReadAsAsyncWithAssertion<TodoResponse>();
        completed.Status.Should().Be("Completed");
        completed.CompletedAt.Should().NotBeNull();
    }

    // Body-less state-transition POSTs (like /{id}/complete) intentionally do NOT require
    // If-Match. The state machine guard on TodoItem.Complete rejects a duplicate transition
    // (already-completed todo) with 422 Unprocessable Content — there is no body to overwrite,
    // so a precondition header would be ceremony without benefit. See the template's
    // "Require If-Match on body-overwriting mutations" rule.
    [Fact]
    public async Task Complete_already_completed_todo_returns_422_UnprocessableContent()
    {
        var client = CreateClient("owner-1", "todos:create", "todos:complete");
        var dueDate = DateTime.UtcNow.AddDays(2);
        var createResponse = await client.PostJsonIdempotentAsync(BaseUrl, new { title = "Already done", dueDate }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();

        var first = await client.PostAsync($"api/Todos/{created.Id}/complete?{VersionParam}", null, TestContext.Current.CancellationToken);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsync($"api/Todos/{created.Id}/complete?{VersionParam}", null, TestContext.Current.CancellationToken);

        second.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Complete_other_users_todo_returns_403()
    {
        // Create todo as owner
        var ownerClient = CreateClient("owner-1", "todos:create", "todos:complete");
        var dueDate = DateTime.UtcNow.AddDays(2);
        var createResponse = await ownerClient.PostJsonIdempotentAsync(BaseUrl, new { title = "Not yours", dueDate }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();

        // Try to complete as different user. Resource-authorization runs before the handler, so the
        // 403 wins without us needing to present a valid If-Match.
        var otherClient = CreateClient("other-user", "todos:complete");
        var response = await otherClient.PostAsync($"api/Todos/{created.Id}/complete?{VersionParam}", null, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_todo_returns_204()
    {
        var client = CreateClient("user-1", "todos:create", "todos:delete");
        var dueDate = DateTime.UtcNow.AddDays(1);
        var createResponse = await client.PostJsonIdempotentAsync(BaseUrl, new { title = "Delete me", dueDate }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();
        var etag = createResponse.Headers.ETag!;

        using var delete = new HttpRequestMessage(HttpMethod.Delete, $"api/Todos/{created.Id}?{VersionParam}");
        delete.Headers.IfMatch.Add(etag);
        var response = await client.SendAsync(delete, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_without_If_Match_returns_428_PreconditionRequired()
    {
        var client = CreateClient("user-1", "todos:create", "todos:delete");
        var dueDate = DateTime.UtcNow.AddDays(1);
        var createResponse = await client.PostJsonIdempotentAsync(BaseUrl, new { title = "Needs If-Match to delete", dueDate }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();

        var response = await client.DeleteAsync($"api/Todos/{created.Id}?{VersionParam}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.PreconditionRequired);
    }

    [Fact]
    public async Task Delete_with_stale_If_Match_returns_412_PreconditionFailed()
    {
        var client = CreateClient("user-1", "todos:create", "todos:delete");
        var dueDate = DateTime.UtcNow.AddDays(1);
        var createResponse = await client.PostJsonIdempotentAsync(BaseUrl, new { title = "Stale delete", dueDate }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();

        using var delete = new HttpRequestMessage(HttpMethod.Delete, $"api/Todos/{created.Id}?{VersionParam}");
        delete.Headers.TryAddWithoutValidation("If-Match", "\"this-tag-does-not-match\"");
        var response = await client.SendAsync(delete, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task Update_todo_with_If_Match_returns_200()
    {
        var client = CreateClient("user-1", "todos:create", "todos:read", "todos:update");
        var dueDate = DateTime.UtcNow.AddDays(5);
        var createResponse = await client.PostJsonIdempotentAsync(BaseUrl, new { title = "Original", dueDate }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();
        var etag = createResponse.Headers.ETag;
        etag.Should().NotBeNull();

        var newDueDate = DateTime.UtcNow.AddDays(14);
        using var put = new HttpRequestMessage(HttpMethod.Put, $"api/Todos/{created.Id}?{VersionParam}")
        {
            Content = JsonContent.Create(new { title = "Updated", dueDate = newDueDate, tag = "changed" }),
        };
        put.Headers.IfMatch.Add(etag!);
        var response = await client.SendAsync(put, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadAsAsyncWithAssertion<TodoResponse>();
        updated.Title.Should().Be("Updated");
        updated.Tag.Should().Be("changed");
    }

    [Fact]
    public async Task Update_without_If_Match_returns_428_PreconditionRequired()
    {
        var client = CreateClient("user-1", "todos:create", "todos:read", "todos:update");
        var dueDate = DateTime.UtcNow.AddDays(5);
        var createResponse = await client.PostJsonIdempotentAsync(BaseUrl, new { title = "Needs If-Match", dueDate }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();

        var newDueDate = DateTime.UtcNow.AddDays(14);
        var response = await client.PutAsJsonAsync(
            $"api/Todos/{created.Id}?{VersionParam}",
            new { title = "Updated", dueDate = newDueDate },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.PreconditionRequired);
    }

    [Fact]
    public async Task Update_with_stale_If_Match_returns_412_PreconditionFailed()
    {
        var client = CreateClient("user-1", "todos:create", "todos:read", "todos:update");
        var dueDate = DateTime.UtcNow.AddDays(5);
        var createResponse = await client.PostJsonIdempotentAsync(BaseUrl, new { title = "Stale ETag", dueDate }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();

        var newDueDate = DateTime.UtcNow.AddDays(14);
        using var put = new HttpRequestMessage(HttpMethod.Put, $"api/Todos/{created.Id}?{VersionParam}")
        {
            Content = JsonContent.Create(new { title = "Updated", dueDate = newDueDate }),
        };
        put.Headers.TryAddWithoutValidation("If-Match", "\"this-tag-does-not-match\"");
        var response = await client.SendAsync(put, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task Update_with_Prefer_return_minimal_returns_204_NoContent()
    {
        var client = CreateClient("user-1", "todos:create", "todos:read", "todos:update");
        var dueDate = DateTime.UtcNow.AddDays(5);
        var createResponse = await client.PostJsonIdempotentAsync(BaseUrl, new { title = "Minimal pref", dueDate }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();
        var etag = createResponse.Headers.ETag!;

        var newDueDate = DateTime.UtcNow.AddDays(14);
        using var put = new HttpRequestMessage(HttpMethod.Put, $"api/Todos/{created.Id}?{VersionParam}")
        {
            Content = JsonContent.Create(new { title = "Minimal", dueDate = newDueDate }),
        };
        put.Headers.IfMatch.Add(etag);
        put.Headers.Add("Prefer", "return=minimal");
        var response = await client.SendAsync(put, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        // RFC 7240: server SHOULD echo the honored preference and Vary: Prefer.
        response.Headers.Vary.Should().Contain("Prefer");
        response.Headers.GetValues("Preference-Applied").Should().Contain(v => v.Contains("return=minimal"));
    }

    [Fact]
    public async Task Update_with_past_due_date_returns_422()
    {
        var client = CreateClient("user-1", "todos:create", "todos:read", "todos:update");
        var dueDate = DateTime.UtcNow.AddDays(5);
        var createResponse = await client.PostJsonIdempotentAsync(BaseUrl, new { title = "Will fail update", dueDate }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();

        var pastDate = DateTime.UtcNow.AddDays(-1);
        var response = await client.PutAsJsonAsync($"api/Todos/{created.Id}?{VersionParam}", new { title = "Updated", dueDate = pastDate }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Full_lifecycle_create_get_complete_delete()
    {
        var client = CreateClient("lifecycle-user", "todos:create", "todos:read", "todos:complete", "todos:delete");
        var dueDate = DateTime.UtcNow.AddDays(10);

        // Create
        var createResponse = await client.PostJsonIdempotentAsync(BaseUrl, new { title = "Lifecycle test", dueDate, tag = "e2e" }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();
        created.Status.Should().Be("Active");

        // Get
        var getResponse = await client.GetAsync($"api/Todos/{created.Id}?{VersionParam}", TestContext.Current.CancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Complete — body-less state-transition POST, no If-Match required
        var completeResponse = await client.PostAsync($"api/Todos/{created.Id}/complete?{VersionParam}", null, TestContext.Current.CancellationToken);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var completed = await completeResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();
        completed.Status.Should().Be("Completed");

        // Delete (use the post-complete ETag — completing mutated the aggregate)
        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"api/Todos/{created.Id}?{VersionParam}");
        deleteRequest.Headers.IfMatch.Add(completeResponse.Headers.ETag!);
        var deleteResponse = await client.SendAsync(deleteRequest, TestContext.Current.CancellationToken);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deleted
        var getDeletedResponse = await client.GetAsync($"api/Todos/{created.Id}?{VersionParam}", TestContext.Current.CancellationToken);
        getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #region Overdue endpoint (paginated — RFC 8288 Link header)

    [Fact]
    public async Task GetOverdue_returns_paged_response()
    {
        var client = CreateClient("user-1", "todos:create", "todos:read");
        // Create a todo with a past due date — it will be Active and overdue
        var pastDue = DateTime.UtcNow.AddDays(-1);
        var createResponse = await client.PostJsonIdempotentAsync(BaseUrl, new { title = "Overdue todo", dueDate = pastDue }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await client.GetAsync($"api/Todos/overdue?limit=10&{VersionParam}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await response.Content.ReadFromJsonAsync<PagedTodoResponse>(TestContext.Current.CancellationToken);
        paged.Should().NotBeNull();
        paged!.Items.Should().NotBeNull();
        paged.AppliedLimit.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetOverdue_emits_Link_header_when_more_pages_exist()
    {
        var client = CreateClient("link-user", "todos:create", "todos:read");
        // Seed three overdue items so a limit of 1 produces a `next` link.
        for (var i = 0; i < 3; i++)
        {
            var seed = await client.PostJsonIdempotentAsync(BaseUrl,
                new { title = $"Overdue {i}", dueDate = DateTime.UtcNow.AddDays(-1 - i) },
                TestContext.Current.CancellationToken);
            seed.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var response = await client.GetAsync($"api/Todos/overdue?limit=1&{VersionParam}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Link", out var linkValues).Should().BeTrue();
        var link = string.Join(", ", linkValues!);
        link.Should().Contain("rel=\"next\"");
    }

    [Fact]
    public async Task GetOverdue_following_next_cursor_returns_distinct_items()
    {
        var client = CreateClient("paginate-user", "todos:create", "todos:read");
        for (var i = 0; i < 3; i++)
        {
            var seed = await client.PostJsonIdempotentAsync(BaseUrl,
                new { title = $"Page {i}", dueDate = DateTime.UtcNow.AddDays(-1 - i) },
                TestContext.Current.CancellationToken);
            seed.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var firstPage = await client.GetFromJsonAsync<PagedTodoResponse>(
            $"api/Todos/overdue?limit=1&{VersionParam}", TestContext.Current.CancellationToken);
        firstPage.Should().NotBeNull();
        firstPage!.Items.Should().HaveCount(1);
        firstPage.Next.Should().NotBeNull();
        firstPage.Next!.Cursor.Should().NotBeNullOrEmpty();
        firstPage.Next.Href.Should().Contain("cursor=");

        var secondPage = await client.GetFromJsonAsync<PagedTodoResponse>(
            $"api/Todos/overdue?cursor={firstPage.Next.Cursor}&limit=1&{VersionParam}",
            TestContext.Current.CancellationToken);
        secondPage.Should().NotBeNull();
        secondPage!.Items.Should().HaveCount(1);
        secondPage.Items[0].Id.Should().NotBe(firstPage.Items[0].Id);
    }

    [Fact]
    public async Task GetOverdue_with_malformed_cursor_returns_422()
    {
        var client = CreateClient("user-1", "todos:read");

        var response = await client.GetAsync($"api/Todos/overdue?cursor=not-a-guid&{VersionParam}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // The all-zero GUID is a *syntactically* valid 32-hex-char cursor that the keyset parser
    // would accept, but TodoId's ValidateAdditional rejects Guid.Empty as a domain value.
    // The handler must surface this as 422 (malformed cursor), NOT bubble the validation
    // failure into a 500. Regression: see PR #43 Copilot review comment on
    // GetOverdueTodosQuery.cs:53.
    [Fact]
    public async Task GetOverdue_with_all_zero_cursor_returns_422_not_500()
    {
        var client = CreateClient("user-1", "todos:read");

        var response = await client.GetAsync($"api/Todos/overdue?cursor=00000000000000000000000000000000&{VersionParam}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    #endregion

    #region Conditional requests (RFC 9110)

    [Fact]
    public async Task GetById_emits_ETag_and_LastModified_headers()
    {
        var client = CreateClient("user-1", "todos:create", "todos:read");
        var dueDate = DateTime.UtcNow.AddDays(2);
        var createResponse = await client.PostJsonIdempotentAsync(BaseUrl, new { title = "Headers", dueDate }, TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();

        var response = await client.GetAsync($"api/Todos/{created.Id}?{VersionParam}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag.Should().NotBeNull();
        response.Content.Headers.LastModified.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_with_matching_If_None_Match_returns_304_NotModified()
    {
        var client = CreateClient("user-1", "todos:create", "todos:read");
        var dueDate = DateTime.UtcNow.AddDays(2);
        var createResponse = await client.PostJsonIdempotentAsync(BaseUrl, new { title = "304 me", dueDate }, TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();
        var etag = createResponse.Headers.ETag!;

        using var get = new HttpRequestMessage(HttpMethod.Get, $"api/Todos/{created.Id}?{VersionParam}");
        get.Headers.IfNoneMatch.Add(etag);
        var response = await client.SendAsync(get, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task GetById_with_If_Modified_Since_in_future_returns_304_NotModified()
    {
        var client = CreateClient("user-1", "todos:create", "todos:read");
        var dueDate = DateTime.UtcNow.AddDays(2);
        var createResponse = await client.PostJsonIdempotentAsync(BaseUrl, new { title = "ims-304", dueDate }, TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();

        using var get = new HttpRequestMessage(HttpMethod.Get, $"api/Todos/{created.Id}?{VersionParam}");
        // Any date after the resource's Last-Modified should produce 304.
        get.Headers.IfModifiedSince = DateTimeOffset.UtcNow.AddDays(1);
        var response = await client.SendAsync(get, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task GetById_with_If_Unmodified_Since_in_past_returns_412_PreconditionFailed()
    {
        var client = CreateClient("user-1", "todos:create", "todos:read");
        var dueDate = DateTime.UtcNow.AddDays(2);
        var createResponse = await client.PostJsonIdempotentAsync(BaseUrl, new { title = "ius-412", dueDate }, TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();

        using var get = new HttpRequestMessage(HttpMethod.Get, $"api/Todos/{created.Id}?{VersionParam}");
        // Any date strictly before the resource's Last-Modified should produce 412.
        get.Headers.IfUnmodifiedSince = DateTimeOffset.UtcNow.AddDays(-7);
        var response = await client.SendAsync(get, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    #endregion

    private sealed record PagedTodoResponse(
        IReadOnlyList<TodoResponse> Items,
        PageLinkDto? Next,
        PageLinkDto? Previous,
        int RequestedLimit,
        int AppliedLimit,
        int DeliveredCount,
        bool WasCapped);

    private sealed record PageLinkDto(string Cursor, string Href);

    #region Permission denied tests (403)

    [Fact]
    public async Task Create_without_permission_returns_403()
    {
        var client = CreateClient("user-1", "todos:read"); // no todos:create
        var body = new { title = "Denied", dueDate = DateTime.UtcNow.AddDays(5) };

        var response = await client.PostJsonIdempotentAsync(BaseUrl, body, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetById_without_permission_returns_403()
    {
        var client = CreateClient("user-1"); // no permissions at all
        var fakeId = Guid.NewGuid();

        var response = await client.GetAsync($"api/Todos/{fakeId}?{VersionParam}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOverdue_without_permission_returns_403()
    {
        var client = CreateClient("user-1"); // no todos:read

        var response = await client.GetAsync($"api/Todos/overdue?{VersionParam}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_without_permission_returns_403()
    {
        // Create a todo first with a permissioned client
        var creator = CreateClient("user-1", "todos:create");
        var createResponse = await creator.PostJsonIdempotentAsync(BaseUrl, new { title = "To update", dueDate = DateTime.UtcNow.AddDays(5) }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();

        // Try to update without todos:update
        var client = CreateClient("user-1", "todos:read");
        var response = await client.PutAsJsonAsync($"api/Todos/{created.Id}?{VersionParam}", new { title = "Updated", dueDate = DateTime.UtcNow.AddDays(10) }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Complete_without_permission_returns_403()
    {
        var creator = CreateClient("owner-1", "todos:create");
        var createResponse = await creator.PostJsonIdempotentAsync(BaseUrl, new { title = "To complete", dueDate = DateTime.UtcNow.AddDays(5) }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();

        // Try to complete without todos:complete
        var client = CreateClient("owner-1", "todos:read");
        var response = await client.PostAsync($"api/Todos/{created.Id}/complete?{VersionParam}", null, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_without_permission_returns_403()
    {
        var creator = CreateClient("user-1", "todos:create");
        var createResponse = await creator.PostJsonIdempotentAsync(BaseUrl, new { title = "To delete", dueDate = DateTime.UtcNow.AddDays(5) }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();

        // Try to delete without todos:delete
        var client = CreateClient("user-1", "todos:read");
        var response = await client.DeleteAsync($"api/Todos/{created.Id}?{VersionParam}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Cross-version isolation (v1 ↔ v2)

    [Fact]
    public async Task Create_under_v1_response_body_does_not_include_IsOverdue_field()
    {
        // Namespace-versioning guarantees v1 and v2 are isolated by controller class.
        // v2 (2026-12-01) added an `isOverdue` field to TodoResponse; v1 callers must
        // continue to receive the original shape. This test asserts the raw JSON body
        // does not contain the new field when the request hits api-version=2026-03-26.
        var client = CreateClient("user-1", "todos:create");
        var body = new { title = "v1-shape", dueDate = DateTime.UtcNow.AddDays(2) };

        var response = await client.PostJsonIdempotentAsync(BaseUrl, body, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        json.Should().NotContain("isOverdue", "v1 response must keep its original shape — IsOverdue is a v2 addition");
    }

    #endregion
}

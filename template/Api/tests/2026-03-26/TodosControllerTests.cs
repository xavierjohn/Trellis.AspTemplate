namespace Api.Tests._2026_03_26;

using System.Net;
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

    [Fact]
    public async Task Create_valid_todo_returns_201_with_location()
    {
        var client = CreateClient("user-1", "todos:create", "todos:read");
        var dueDate = DateTime.UtcNow.AddDays(7);
        var body = new { title = "Buy groceries", dueDate, tag = "shopping" };

        var response = await client.PostAsJsonAsync(BaseUrl, body, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

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

        var response = await client.PostAsJsonAsync(BaseUrl, body, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var todo = await response.Content.ReadAsAsyncWithAssertion<TodoResponse>();
        todo.Tag.Should().BeNull();
    }

    [Fact]
    public async Task Create_todo_with_invalid_title_returns_400()
    {
        var client = CreateClient("user-1", "todos:create");
        var body = new { title = "", dueDate = DateTime.UtcNow.AddDays(3) };

        var response = await client.PostAsJsonAsync(BaseUrl, body, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_existing_todo_returns_200()
    {
        var client = CreateClient("user-1", "todos:create", "todos:read");
        var dueDate = DateTime.UtcNow.AddDays(5);
        var createResponse = await client.PostAsJsonAsync(BaseUrl, new { title = "Fetch me", dueDate }, TestContext.Current.CancellationToken);
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
        var createResponse = await client.PostAsJsonAsync(BaseUrl, new { title = "Complete me", dueDate }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();

        var response = await client.PostAsync($"api/Todos/{created.Id}/complete?{VersionParam}", null, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var completed = await response.Content.ReadAsAsyncWithAssertion<TodoResponse>();
        completed.Status.Should().Be("Completed");
        completed.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Complete_other_users_todo_returns_403()
    {
        // Create todo as owner
        var ownerClient = CreateClient("owner-1", "todos:create", "todos:complete");
        var dueDate = DateTime.UtcNow.AddDays(2);
        var createResponse = await ownerClient.PostAsJsonAsync(BaseUrl, new { title = "Not yours", dueDate }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();

        // Try to complete as different user
        var otherClient = CreateClient("other-user", "todos:complete");
        var response = await otherClient.PostAsync($"api/Todos/{created.Id}/complete?{VersionParam}", null, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_todo_returns_204()
    {
        var client = CreateClient("user-1", "todos:create", "todos:delete");
        var dueDate = DateTime.UtcNow.AddDays(1);
        var createResponse = await client.PostAsJsonAsync(BaseUrl, new { title = "Delete me", dueDate }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();

        var response = await client.DeleteAsync($"api/Todos/{created.Id}?{VersionParam}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_todo_returns_200()
    {
        var client = CreateClient("user-1", "todos:create", "todos:read", "todos:update");
        var dueDate = DateTime.UtcNow.AddDays(5);
        var createResponse = await client.PostAsJsonAsync(BaseUrl, new { title = "Original", dueDate }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();

        var newDueDate = DateTime.UtcNow.AddDays(14);
        var response = await client.PutAsJsonAsync($"api/Todos/{created.Id}?{VersionParam}", new { title = "Updated", dueDate = newDueDate, tag = "changed" }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadAsAsyncWithAssertion<TodoResponse>();
        updated.Title.Should().Be("Updated");
        updated.Tag.Should().Be("changed");
    }

    [Fact]
    public async Task Update_with_past_due_date_returns_400()
    {
        var client = CreateClient("user-1", "todos:create", "todos:read", "todos:update");
        var dueDate = DateTime.UtcNow.AddDays(5);
        var createResponse = await client.PostAsJsonAsync(BaseUrl, new { title = "Will fail update", dueDate }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();

        var pastDate = DateTime.UtcNow.AddDays(-1);
        var response = await client.PutAsJsonAsync($"api/Todos/{created.Id}?{VersionParam}", new { title = "Updated", dueDate = pastDate }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Full_lifecycle_create_get_complete_delete()
    {
        var client = CreateClient("lifecycle-user", "todos:create", "todos:read", "todos:complete", "todos:delete");
        var dueDate = DateTime.UtcNow.AddDays(10);

        // Create
        var createResponse = await client.PostAsJsonAsync(BaseUrl, new { title = "Lifecycle test", dueDate, tag = "e2e" }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();
        created.Status.Should().Be("Active");

        // Get
        var getResponse = await client.GetAsync($"api/Todos/{created.Id}?{VersionParam}", TestContext.Current.CancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Complete
        var completeResponse = await client.PostAsync($"api/Todos/{created.Id}/complete?{VersionParam}", null, TestContext.Current.CancellationToken);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var completed = await completeResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();
        completed.Status.Should().Be("Completed");

        // Delete
        var deleteResponse = await client.DeleteAsync($"api/Todos/{created.Id}?{VersionParam}", TestContext.Current.CancellationToken);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deleted
        var getDeletedResponse = await client.GetAsync($"api/Todos/{created.Id}?{VersionParam}", TestContext.Current.CancellationToken);
        getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #region Overdue endpoint

    [Fact]
    public async Task GetOverdue_returns_200()
    {
        var client = CreateClient("user-1", "todos:create", "todos:read");
        // Create a todo with a past due date — it will be Active and overdue
        var pastDue = DateTime.UtcNow.AddDays(-1);
        var createResponse = await client.PostAsJsonAsync(BaseUrl, new { title = "Overdue todo", dueDate = pastDue }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await client.GetAsync($"api/Todos/overdue?{VersionParam}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Permission denied tests (403)

    [Fact]
    public async Task Create_without_permission_returns_403()
    {
        var client = CreateClient("user-1", "todos:read"); // no todos:create
        var body = new { title = "Denied", dueDate = DateTime.UtcNow.AddDays(5) };

        var response = await client.PostAsJsonAsync(BaseUrl, body, TestContext.Current.CancellationToken);

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
        var createResponse = await creator.PostAsJsonAsync(BaseUrl, new { title = "To update", dueDate = DateTime.UtcNow.AddDays(5) }, TestContext.Current.CancellationToken);
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
        var createResponse = await creator.PostAsJsonAsync(BaseUrl, new { title = "To complete", dueDate = DateTime.UtcNow.AddDays(5) }, TestContext.Current.CancellationToken);
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
        var createResponse = await creator.PostAsJsonAsync(BaseUrl, new { title = "To delete", dueDate = DateTime.UtcNow.AddDays(5) }, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadAsAsyncWithAssertion<TodoResponse>();

        // Try to delete without todos:delete
        var client = CreateClient("user-1", "todos:read");
        var response = await client.DeleteAsync($"api/Todos/{created.Id}?{VersionParam}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion
}

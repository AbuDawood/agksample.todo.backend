using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using TodoApi.Application.Todos;
using Xunit;

namespace TodoApi.Tests;

public sealed class TodoApiHttpApiTests
{
    [Fact]
    public async Task Health_is_successful_and_machine_readable()
    {
        await using var app = CreateApplication();
        using var client = app.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", json.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Post_creates_a_todo_with_location()
    {
        await using var app = CreateApplication();
        using var client = app.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/todos",
            new CreateTodoRequest("Ship API", "Complete the foundation"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var todo = await ReadTodoAsync(response);
        Assert.NotEqual(Guid.Empty, todo.Id);
        Assert.Equal("Ship API", todo.Title);
        Assert.Equal("Complete the foundation", todo.Description);
        Assert.False(todo.IsCompleted);
        Assert.Equal(todo.CreatedAtUtc, todo.UpdatedAtUtc);
        Assert.Equal($"/api/todos/{todo.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_lists_created_todos()
    {
        await using var app = CreateApplication();
        using var client = app.CreateClient();
        await CreateTodoAsync(client, "First");
        await CreateTodoAsync(client, "Second");

        var todos = await client.GetFromJsonAsync<TodoDto[]>("/api/todos");

        Assert.NotNull(todos);
        Assert.Collection(
            todos,
            todo => Assert.Equal("First", todo.Title),
            todo => Assert.Equal("Second", todo.Title));
    }

    [Fact]
    public async Task Get_returns_todo_detail_and_problem_for_unknown_id()
    {
        await using var app = CreateApplication();
        using var client = app.CreateClient();
        var created = await CreateTodoAsync(client, "Detail");

        var todo = await client.GetFromJsonAsync<TodoDto>($"/api/todos/{created.Id}");
        using var missingResponse = await client.GetAsync($"/api/todos/{Guid.NewGuid()}");

        Assert.NotNull(todo);
        Assert.Equal(created, todo);
        await AssertProblemAsync(missingResponse, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_updates_an_existing_todo_and_is_idempotent()
    {
        await using var app = CreateApplication();
        using var client = app.CreateClient();
        var created = await CreateTodoAsync(client, "Before");
        var request = new UpsertTodoRequest("After", "Updated");

        using var firstResponse = await client.PutAsJsonAsync($"/api/todos/{created.Id}", request);
        var first = await ReadTodoAsync(firstResponse);
        using var secondResponse = await client.PutAsJsonAsync($"/api/todos/{created.Id}", request);
        var second = await ReadTodoAsync(secondResponse);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(created.Id, first.Id);
        Assert.Equal(created.CreatedAtUtc, first.CreatedAtUtc);
        Assert.Equal("After", first.Title);
        Assert.Equal("Updated", first.Description);
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Put_upserts_an_absent_id_with_created_status_and_location()
    {
        await using var app = CreateApplication();
        using var client = app.CreateClient();
        var id = Guid.NewGuid();

        using var response = await client.PutAsJsonAsync(
            $"/api/todos/{id}",
            new UpsertTodoRequest("Upserted", null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var todo = await ReadTodoAsync(response);
        Assert.Equal(id, todo.Id);
        Assert.Equal($"/api/todos/{id}", response.Headers.Location?.OriginalString);
        Assert.Equal(todo, await client.GetFromJsonAsync<TodoDto>($"/api/todos/{id}"));
    }

    [Fact]
    public async Task Patch_sets_the_explicit_completion_state()
    {
        await using var app = CreateApplication();
        using var client = app.CreateClient();
        var created = await CreateTodoAsync(client, "Complete me");

        using var completeResponse = await PatchAsJsonAsync(
            client,
            $"/api/todos/{created.Id}/completion",
            new SetTodoCompletionRequest(true));
        var completed = await ReadTodoAsync(completeResponse);
        using var reopenResponse = await PatchAsJsonAsync(
            client,
            $"/api/todos/{created.Id}/completion",
            new SetTodoCompletionRequest(false));
        var reopened = await ReadTodoAsync(reopenResponse);

        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        Assert.True(completed.IsCompleted);
        Assert.Equal(HttpStatusCode.OK, reopenResponse.StatusCode);
        Assert.False(reopened.IsCompleted);
        Assert.True(reopened.UpdatedAtUtc > completed.UpdatedAtUtc);
    }

    [Fact]
    public async Task Missing_or_blank_title_returns_problem_without_mutation()
    {
        await using var app = CreateApplication();
        using var client = app.CreateClient();

        using var missingResponse = await client.PostAsJsonAsync(
            "/api/todos",
            new { description = "No title" });
        using var blankResponse = await client.PostAsJsonAsync(
            "/api/todos",
            new CreateTodoRequest("   ", null));
        var todos = await client.GetFromJsonAsync<TodoDto[]>("/api/todos");

        await AssertProblemAsync(missingResponse, HttpStatusCode.BadRequest);
        await AssertProblemAsync(blankResponse, HttpStatusCode.BadRequest);
        Assert.Empty(todos!);
    }

    [Fact]
    public async Task Invalid_update_does_not_mutate_the_existing_todo()
    {
        await using var app = CreateApplication();
        using var client = app.CreateClient();
        var created = await CreateTodoAsync(client, "Keep me");

        using var response = await client.PutAsJsonAsync(
            $"/api/todos/{created.Id}",
            new UpsertTodoRequest("", "Must not be stored"));
        var persisted = await client.GetFromJsonAsync<TodoDto>($"/api/todos/{created.Id}");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(created, persisted);
    }

    [Fact]
    public async Task Delete_returns_no_content_and_removes_the_todo()
    {
        await using var app = CreateApplication();
        using var client = app.CreateClient();
        var created = await CreateTodoAsync(client, "Delete me");

        using var response = await client.DeleteAsync($"/api/todos/{created.Id}");
        var list = await client.GetFromJsonAsync<TodoDto[]>("/api/todos");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        Assert.Empty(list!);
    }

    [Fact]
    public async Task Deleted_todo_lookup_returns_not_found_problem()
    {
        await using var app = CreateApplication();
        using var client = app.CreateClient();
        var created = await CreateTodoAsync(client, "Gone");
        using var deleteResponse = await client.DeleteAsync($"/api/todos/{created.Id}");

        using var lookupResponse = await client.GetAsync($"/api/todos/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        await AssertProblemAsync(lookupResponse, HttpStatusCode.NotFound);
    }

    private static WebApplicationFactory<Program> CreateApplication() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(_ => { });

    private static async Task<TodoDto> CreateTodoAsync(HttpClient client, string title)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/todos",
            new CreateTodoRequest(title, null));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadTodoAsync(response);
    }

    private static async Task<HttpResponseMessage> PatchAsJsonAsync<T>(
        HttpClient client,
        string uri,
        T value)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, uri)
        {
            Content = JsonContent.Create(value)
        };

        return await client.SendAsync(request);
    }

    private static async Task<TodoDto> ReadTodoAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<TodoDto>()
        ?? throw new InvalidOperationException("The response did not contain a Todo.");

    private static async Task AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)expectedStatus, problem.Status);
    }
}

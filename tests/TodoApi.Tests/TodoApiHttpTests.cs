using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TodoApi.Tests;

public sealed class TodoApiHttpTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client;

    public TodoApiHttpTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_returns_machine_readable_success()
    {
        using var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using var json = await ReadJsonAsync(response);
        Assert.Equal("Healthy", json.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Create_returns_resource_and_location()
    {
        var request = new { title = UniqueTitle("create"), description = "Created in an HTTP test" };

        using var response = await _client.PostAsJsonAsync("/api/todos", request);
        var todo = await ReadTodoAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/todos/{todo.Id:D}", response.Headers.Location?.OriginalString);
        Assert.Equal(request.title, todo.Title);
        Assert.Equal(request.description, todo.Description);
        Assert.False(todo.IsCompleted);
        Assert.NotEqual(default, todo.CreatedAtUtc);
        Assert.Equal(todo.CreatedAtUtc, todo.UpdatedAtUtc);
    }

    [Fact]
    public async Task List_includes_created_item()
    {
        var created = await CreateTodoAsync(UniqueTitle("list"), "Listed");

        using var response = await _client.GetAsync("/api/todos");
        var todos = await response.Content.ReadFromJsonAsync<TodoResponse[]>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(todos);
        Assert.Contains(todos, todo => todo.Id == created.Id);
    }

    [Fact]
    public async Task Detail_returns_created_item()
    {
        var created = await CreateTodoAsync(UniqueTitle("detail"), null);

        using var response = await _client.GetAsync($"/api/todos/{created.Id:D}");
        var detail = await ReadTodoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created, detail);
    }

    [Fact]
    public async Task Put_updates_existing_item()
    {
        var created = await CreateTodoAsync(UniqueTitle("before-update"), "Old description");
        var update = new { title = UniqueTitle("after-update"), description = "New description" };

        using var response = await _client.PutAsJsonAsync($"/api/todos/{created.Id:D}", update);
        var updated = await ReadTodoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal(update.title, updated.Title);
        Assert.Equal(update.description, updated.Description);
        Assert.Equal(created.CreatedAtUtc, updated.CreatedAtUtc);
        Assert.True(updated.UpdatedAtUtc >= created.UpdatedAtUtc);
    }

    [Fact]
    public async Task Put_absent_id_creates_once_and_is_idempotent()
    {
        var id = Guid.NewGuid();
        var request = new { title = UniqueTitle("upsert"), description = "Upserted" };

        using var firstResponse = await _client.PutAsJsonAsync($"/api/todos/{id:D}", request);
        var first = await ReadTodoAsync(firstResponse);
        using var secondResponse = await _client.PutAsJsonAsync($"/api/todos/{id:D}", request);
        var second = await ReadTodoAsync(secondResponse);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal($"/api/todos/{id:D}", firstResponse.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(id, first.Id);
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Completion_patch_applies_explicit_value()
    {
        var created = await CreateTodoAsync(UniqueTitle("completion"), null);

        using var response = await _client.PatchAsJsonAsync(
            $"/api/todos/{created.Id:D}/completion",
            new { completed = true });
        var completed = await ReadTodoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(completed.IsCompleted);
        Assert.Equal(created.Id, completed.Id);
    }

    [Fact]
    public async Task Missing_or_blank_title_returns_problem_without_mutation()
    {
        var countBefore = await GetTodoCountAsync();

        using var missingResponse = await _client.PostAsJsonAsync(
            "/api/todos",
            new { description = "No title" });
        using var blankResponse = await _client.PostAsJsonAsync(
            "/api/todos",
            new { title = "  ", description = "Blank title" });

        await AssertProblemAsync(missingResponse, HttpStatusCode.BadRequest);
        await AssertProblemAsync(blankResponse, HttpStatusCode.BadRequest);
        Assert.Equal(countBefore, await GetTodoCountAsync());
    }

    [Fact]
    public async Task Delete_returns_no_content()
    {
        var created = await CreateTodoAsync(UniqueTitle("delete"), null);

        using var response = await _client.DeleteAsync($"/api/todos/{created.Id:D}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, response.Content.Headers.ContentLength);
    }

    [Fact]
    public async Task Deleted_item_lookup_returns_not_found_problem()
    {
        var created = await CreateTodoAsync(UniqueTitle("deleted-lookup"), null);
        using var deleteResponse = await _client.DeleteAsync($"/api/todos/{created.Id:D}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var response = await _client.GetAsync($"/api/todos/{created.Id:D}");

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    private async Task<TodoResponse> CreateTodoAsync(string title, string? description)
    {
        using var response = await _client.PostAsJsonAsync("/api/todos", new { title, description });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadTodoAsync(response);
    }

    private async Task<int> GetTodoCountAsync()
    {
        var todos = await _client.GetFromJsonAsync<TodoResponse[]>("/api/todos", JsonOptions);
        Assert.NotNull(todos);
        return todos.Length;
    }

    private static async Task<TodoResponse> ReadTodoAsync(HttpResponseMessage response)
    {
        var todo = await response.Content.ReadFromJsonAsync<TodoResponse>(JsonOptions);
        Assert.NotNull(todo);
        return todo;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    private static async Task AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var json = await ReadJsonAsync(response);
        Assert.Equal((int)expectedStatus, json.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("title").GetString()));
    }

    private static string UniqueTitle(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private sealed record TodoResponse(
        Guid Id,
        string Title,
        string? Description,
        bool IsCompleted,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TodoApi.Tests.Features.Filtering;

public sealed class TodoFilteringHttpTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client;

    public TodoFilteringHttpTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Completed_filter_returns_only_completed_todos()
    {
        var incomplete = await CreateTodoAsync("completed-filter-incomplete");
        var completed = await CreateCompletedTodoAsync("completed-filter-complete");

        var todos = await GetTodosAsync("/api/todos?isCompleted=true");

        Assert.Contains(todos, todo => todo.Id == completed.Id);
        Assert.DoesNotContain(todos, todo => todo.Id == incomplete.Id);
        Assert.All(todos, todo => Assert.True(todo.IsCompleted));
    }

    [Fact]
    public async Task Incomplete_filter_returns_only_incomplete_todos()
    {
        var incomplete = await CreateTodoAsync("incomplete-filter-incomplete");
        var completed = await CreateCompletedTodoAsync("incomplete-filter-complete");

        var todos = await GetTodosAsync("/api/todos?isCompleted=false");

        Assert.Contains(todos, todo => todo.Id == incomplete.Id);
        Assert.DoesNotContain(todos, todo => todo.Id == completed.Id);
        Assert.All(todos, todo => Assert.False(todo.IsCompleted));
    }

    [Fact]
    public async Task Missing_filter_returns_completed_and_incomplete_todos()
    {
        var incomplete = await CreateTodoAsync("unfiltered-incomplete");
        var completed = await CreateCompletedTodoAsync("unfiltered-complete");

        var todos = await GetTodosAsync("/api/todos");

        Assert.Contains(todos, todo => todo.Id == incomplete.Id);
        Assert.Contains(todos, todo => todo.Id == completed.Id);
    }

    private async Task<TodoResponse> CreateCompletedTodoAsync(string titlePrefix)
    {
        var todo = await CreateTodoAsync(titlePrefix);
        using var response = await _client.PatchAsJsonAsync(
            $"/api/todos/{todo.Id:D}/completion",
            new { completed = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var completed = await response.Content.ReadFromJsonAsync<TodoResponse>(JsonOptions);
        Assert.NotNull(completed);
        return completed;
    }

    private async Task<TodoResponse> CreateTodoAsync(string titlePrefix)
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/todos",
            new { title = $"{titlePrefix}-{Guid.NewGuid():N}" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var todo = await response.Content.ReadFromJsonAsync<TodoResponse>(JsonOptions);
        Assert.NotNull(todo);
        return todo;
    }

    private async Task<TodoResponse[]> GetTodosAsync(string requestUri)
    {
        using var response = await _client.GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var todos = await response.Content.ReadFromJsonAsync<TodoResponse[]>(JsonOptions);
        Assert.NotNull(todos);
        return todos;
    }

    private sealed record TodoResponse(Guid Id, bool IsCompleted);
}

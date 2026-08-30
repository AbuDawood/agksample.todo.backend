using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TodoApi.Tests.Summary;

public sealed class TodoSummaryHttpTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly WebApplicationFactory<Program> _factory = new();
    private readonly HttpClient _client;

    public TodoSummaryHttpTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Summary_for_empty_collection_returns_zero_counts()
    {
        var summary = await GetSummaryAsync();

        Assert.Equal(0, summary.TotalCount);
        Assert.Equal(0, summary.CompletedCount);
    }

    [Fact]
    public async Task Summary_for_mixed_collection_returns_total_and_completed_counts()
    {
        await CreateTodoAsync("remaining-one");
        var firstCompleted = await CreateTodoAsync("completed-one");
        var secondCompleted = await CreateTodoAsync("completed-two");
        await SetCompletionAsync(firstCompleted);
        await SetCompletionAsync(secondCompleted);

        var summary = await GetSummaryAsync();

        Assert.Equal(3, summary.TotalCount);
        Assert.Equal(2, summary.CompletedCount);
    }

    [Fact]
    public async Task Summary_for_completed_collection_returns_matching_counts()
    {
        var first = await CreateTodoAsync("completed-one");
        var second = await CreateTodoAsync("completed-two");
        await SetCompletionAsync(first);
        await SetCompletionAsync(second);

        var summary = await GetSummaryAsync();

        Assert.Equal(2, summary.TotalCount);
        Assert.Equal(2, summary.CompletedCount);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<Guid> CreateTodoAsync(string title)
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/todos",
            new { title = $"{title}-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var todo = await response.Content.ReadFromJsonAsync<TodoResponse>(JsonOptions);
        Assert.NotNull(todo);
        return todo.Id;
    }

    private async Task SetCompletionAsync(Guid id)
    {
        using var response = await _client.PatchAsJsonAsync(
            $"/api/todos/{id:D}/completion",
            new { completed = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<TodoSummaryResponse> GetSummaryAsync()
    {
        using var response = await _client.GetAsync("/api/todos/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var summary = await response.Content.ReadFromJsonAsync<TodoSummaryResponse>(JsonOptions);
        Assert.NotNull(summary);
        return summary;
    }

    private sealed record TodoResponse(Guid Id);

    private sealed record TodoSummaryResponse(int TotalCount, int CompletedCount);
}

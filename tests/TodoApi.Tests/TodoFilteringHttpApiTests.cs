using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TodoApi.Application;
using Xunit;

namespace TodoApi.Tests;

public sealed class TodoFilteringHttpApiTests
{
    [Fact]
    public async Task Completed_filter_returns_only_completed_todos()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var incomplete = await CreateTodoAsync(client, "Still in progress", isCompleted: false);
        var completed = await CreateTodoAsync(client, "Already done", isCompleted: true);

        var items = await client.GetFromJsonAsync<TodoDto[]>("/api/todos?isCompleted=true");

        var item = Assert.Single(Assert.IsType<TodoDto[]>(items));
        Assert.Equal(completed.Id, item.Id);
        Assert.True(item.IsCompleted);
        Assert.DoesNotContain(items, candidate => candidate.Id == incomplete.Id);
    }

    [Fact]
    public async Task Incomplete_filter_returns_only_incomplete_todos()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var incomplete = await CreateTodoAsync(client, "Still in progress", isCompleted: false);
        var completed = await CreateTodoAsync(client, "Already done", isCompleted: true);

        var items = await client.GetFromJsonAsync<TodoDto[]>("/api/todos?isCompleted=false");

        var item = Assert.Single(Assert.IsType<TodoDto[]>(items));
        Assert.Equal(incomplete.Id, item.Id);
        Assert.False(item.IsCompleted);
        Assert.DoesNotContain(items, candidate => candidate.Id == completed.Id);
    }

    [Fact]
    public async Task Omitted_filter_returns_all_todos()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var incomplete = await CreateTodoAsync(client, "Still in progress", isCompleted: false);
        var completed = await CreateTodoAsync(client, "Already done", isCompleted: true);

        var items = await client.GetFromJsonAsync<TodoDto[]>("/api/todos");

        Assert.NotNull(items);
        Assert.Equal(2, items.Length);
        Assert.Equal([incomplete.Id, completed.Id], items.Select(item => item.Id));
    }

    private static async Task<TodoDto> CreateTodoAsync(
        HttpClient client,
        string title,
        bool isCompleted)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/todos",
            new { title, isCompleted });

        response.EnsureSuccessStatusCode();
        var item = await response.Content.ReadFromJsonAsync<TodoDto>();
        return Assert.IsType<TodoDto>(item);
    }
}

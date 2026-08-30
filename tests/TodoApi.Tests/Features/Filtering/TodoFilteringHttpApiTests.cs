using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TodoApi.Application.Todos;
using Xunit;

namespace TodoApi.Tests.Features.Filtering;

public sealed class TodoFilteringHttpApiTests
{
    [Fact]
    public async Task Completed_filter_returns_only_completed_todos()
    {
        await using var app = CreateApplication();
        using var client = app.CreateClient();
        var completed = await CreateTodoAsync(client, "Completed");
        await CreateTodoAsync(client, "Incomplete");
        await SetCompletionAsync(client, completed.Id, true);

        var todos = await client.GetFromJsonAsync<TodoDto[]>("/api/todos?isCompleted=true");

        var todo = Assert.Single(todos!);
        Assert.Equal(completed.Id, todo.Id);
        Assert.True(todo.IsCompleted);
    }

    [Fact]
    public async Task Incomplete_filter_returns_only_incomplete_todos()
    {
        await using var app = CreateApplication();
        using var client = app.CreateClient();
        var completed = await CreateTodoAsync(client, "Completed");
        var incomplete = await CreateTodoAsync(client, "Incomplete");
        await SetCompletionAsync(client, completed.Id, true);

        var todos = await client.GetFromJsonAsync<TodoDto[]>("/api/todos?isCompleted=false");

        var todo = Assert.Single(todos!);
        Assert.Equal(incomplete.Id, todo.Id);
        Assert.False(todo.IsCompleted);
    }

    [Fact]
    public async Task Omitted_filter_preserves_the_unfiltered_list()
    {
        await using var app = CreateApplication();
        using var client = app.CreateClient();
        var completed = await CreateTodoAsync(client, "Completed");
        var incomplete = await CreateTodoAsync(client, "Incomplete");
        await SetCompletionAsync(client, completed.Id, true);

        var todos = await client.GetFromJsonAsync<TodoDto[]>("/api/todos");

        Assert.NotNull(todos);
        Assert.Collection(
            todos,
            todo =>
            {
                Assert.Equal(completed.Id, todo.Id);
                Assert.True(todo.IsCompleted);
            },
            todo =>
            {
                Assert.Equal(incomplete.Id, todo.Id);
                Assert.False(todo.IsCompleted);
            });
    }

    private static WebApplicationFactory<Program> CreateApplication() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(_ => { });

    private static async Task<TodoDto> CreateTodoAsync(HttpClient client, string title)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/todos",
            new CreateTodoRequest(title, null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<TodoDto>()
            ?? throw new InvalidOperationException("The response did not contain a Todo.");
    }

    private static async Task SetCompletionAsync(HttpClient client, Guid id, bool completed)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/todos/{id}/completion")
        {
            Content = JsonContent.Create(new SetTodoCompletionRequest(completed))
        };
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

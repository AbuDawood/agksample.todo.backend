using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TodoApi.Application.Todos;
using TodoApi.Features.Summary;
using Xunit;

namespace TodoApi.Tests;

public sealed class TodoSummaryHttpApiTests
{
    [Fact]
    public async Task Summary_reports_zero_counts_for_an_empty_collection()
    {
        await using var app = CreateApplication();
        using var client = app.CreateClient();

        using var response = await client.GetAsync("/api/todos/summary");
        var summary = await ReadSummaryAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(new TodoSummary(0, 0), summary);
    }

    [Fact]
    public async Task Summary_reports_total_and_completed_counts_for_a_mixed_collection()
    {
        await using var app = CreateApplication();
        using var client = app.CreateClient();
        var first = await CreateTodoAsync(client, "Completed");
        await CreateTodoAsync(client, "Open one");
        await CreateTodoAsync(client, "Open two");
        using var completionResponse = await SetCompletionAsync(client, first.Id, completed: true);

        var summary = await client.GetFromJsonAsync<TodoSummary>("/api/todos/summary");

        Assert.Equal(HttpStatusCode.OK, completionResponse.StatusCode);
        Assert.Equal(new TodoSummary(3, 1), summary);
    }

    [Fact]
    public async Task Summary_reports_every_todo_when_the_collection_is_completed()
    {
        await using var app = CreateApplication();
        using var client = app.CreateClient();
        var first = await CreateTodoAsync(client, "First");
        var second = await CreateTodoAsync(client, "Second");
        using var firstCompletionResponse = await SetCompletionAsync(client, first.Id, completed: true);
        using var secondCompletionResponse = await SetCompletionAsync(client, second.Id, completed: true);

        var summary = await client.GetFromJsonAsync<TodoSummary>("/api/todos/summary");

        Assert.Equal(HttpStatusCode.OK, firstCompletionResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondCompletionResponse.StatusCode);
        Assert.Equal(new TodoSummary(2, 2), summary);
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

    private static Task<HttpResponseMessage> SetCompletionAsync(
        HttpClient client,
        Guid id,
        bool completed)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/todos/{id}/completion")
        {
            Content = JsonContent.Create(new SetTodoCompletionRequest(completed))
        };

        return client.SendAsync(request);
    }

    private static async Task<TodoSummary> ReadSummaryAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<TodoSummary>()
        ?? throw new InvalidOperationException("The response did not contain a Todo summary.");
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TodoApi.Tests.Features.Summary;

public sealed class TodoSummaryEndpointTests
{
    [Fact]
    public async Task Empty_collection_returns_zero_counts()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/todos/summary");
        var summary = await ReadSummaryAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, summary.GetProperty("totalCount").GetInt32());
        Assert.Equal(0, summary.GetProperty("completedCount").GetInt32());
    }

    [Fact]
    public async Task Mixed_collection_returns_total_and_completed_counts()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        await CreateTodoAsync(client, "Not started", isCompleted: false);
        await CreateTodoAsync(client, "Finished one", isCompleted: true);
        await CreateTodoAsync(client, "Finished two", isCompleted: true);

        using var response = await client.GetAsync("/api/todos/summary");
        var summary = await ReadSummaryAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, summary.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, summary.GetProperty("completedCount").GetInt32());
    }

    [Fact]
    public async Task Completed_collection_returns_matching_counts()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        await CreateTodoAsync(client, "Finished one", isCompleted: true);
        await CreateTodoAsync(client, "Finished two", isCompleted: true);

        using var response = await client.GetAsync("/api/todos/summary");
        var summary = await ReadSummaryAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, summary.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, summary.GetProperty("completedCount").GetInt32());
    }

    private static async Task CreateTodoAsync(HttpClient client, string title, bool isCompleted)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/todos",
            new { title, isCompleted });

        response.EnsureSuccessStatusCode();
    }

    private static async Task<JsonElement> ReadSummaryAsync(HttpResponseMessage response)
    {
        var summary = await response.Content.ReadFromJsonAsync<JsonElement>();
        return summary;
    }
}

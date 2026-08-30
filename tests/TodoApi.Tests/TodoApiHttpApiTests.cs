using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using TodoApi.Application;
using Xunit;

namespace TodoApi.Tests;

public sealed class TodoApiHttpApiTests
{
    [Fact]
    public async Task Health_is_machine_readable_and_successful()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", payload.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Create_returns_resource_and_location()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/todos",
            new { title = "Ship API", description = "Run acceptance" });
        var item = await ReadTodoAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/todos/{item.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal("Ship API", item.Title);
        Assert.Equal("Run acceptance", item.Description);
        Assert.False(item.IsCompleted);
        Assert.NotEqual(default, item.CreatedAtUtc);
        Assert.Equal(item.CreatedAtUtc, item.UpdatedAtUtc);
    }

    [Fact]
    public async Task List_returns_created_items()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await CreateTodoAsync(client, "First");
        await CreateTodoAsync(client, "Second");

        var items = await client.GetFromJsonAsync<TodoDto[]>("/api/todos");

        Assert.NotNull(items);
        Assert.Equal(2, items.Length);
        Assert.Equal(["First", "Second"], items.Select(item => item.Title));
    }

    [Fact]
    public async Task Detail_returns_the_requested_item()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var created = await CreateTodoAsync(client, "Detail");

        var item = await client.GetFromJsonAsync<TodoDto>($"/api/todos/{created.Id}");

        Assert.NotNull(item);
        Assert.Equal(created, item);
    }

    [Fact]
    public async Task Put_updates_an_existing_item()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var created = await CreateTodoAsync(client, "Before");

        using var response = await client.PutAsJsonAsync(
            $"/api/todos/{created.Id}",
            new { title = "After", description = "Updated", isCompleted = true });
        var updated = await ReadTodoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("After", updated.Title);
        Assert.Equal("Updated", updated.Description);
        Assert.True(updated.IsCompleted);
        Assert.Equal(created.CreatedAtUtc, updated.CreatedAtUtc);
    }

    [Fact]
    public async Task Put_upserts_an_absent_id_and_is_idempotent()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var id = Guid.NewGuid();
        var request = new { title = "Client id", description = "Upsert", isCompleted = false };

        using var firstResponse = await client.PutAsJsonAsync($"/api/todos/{id}", request);
        var first = await ReadTodoAsync(firstResponse);
        using var secondResponse = await client.PutAsJsonAsync($"/api/todos/{id}", request);
        var second = await ReadTodoAsync(secondResponse);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal($"/api/todos/{id}", firstResponse.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(id, first.Id);
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Completion_accepts_an_explicit_boolean()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var created = await CreateTodoAsync(client, "Complete me");

        using var response = await client.PatchAsJsonAsync(
            $"/api/todos/{created.Id}/completion",
            new { completed = true });
        var completed = await ReadTodoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(completed.IsCompleted);
    }

    [Fact]
    public async Task Missing_or_blank_title_returns_problem_details_without_mutation()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var missingResponse = await client.PostAsJsonAsync("/api/todos", new { description = "No title" });
        using var blankResponse = await client.PostAsJsonAsync("/api/todos", new { title = "   " });
        var missingProblem = await missingResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        var blankProblem = await blankResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        var items = await client.GetFromJsonAsync<TodoDto[]>("/api/todos");

        Assert.Equal(HttpStatusCode.BadRequest, missingResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, blankResponse.StatusCode);
        Assert.Equal("application/problem+json", missingResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal("application/problem+json", blankResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(400, missingProblem?.Status);
        Assert.Equal(400, blankProblem?.Status);
        Assert.Empty(items!);
    }

    [Fact]
    public async Task Delete_returns_no_content()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var created = await CreateTodoAsync(client, "Delete me");

        using var response = await client.DeleteAsync($"/api/todos/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, response.Content.Headers.ContentLength);
    }

    [Fact]
    public async Task Deleted_item_lookup_returns_problem_details()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var created = await CreateTodoAsync(client, "Gone");
        using var deleteResponse = await client.DeleteAsync($"/api/todos/{created.Id}");

        using var response = await client.GetAsync($"/api/todos/{created.Id}");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(404, problem?.Status);
    }

    [Fact]
    public async Task Unknown_item_lookup_returns_problem_details()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/todos/{Guid.NewGuid()}");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(404, problem?.Status);
    }

    private static WebApplicationFactory<Program> CreateFactory() => new();

    private static async Task<TodoDto> CreateTodoAsync(HttpClient client, string title)
    {
        using var response = await client.PostAsJsonAsync("/api/todos", new { title });
        response.EnsureSuccessStatusCode();
        return await ReadTodoAsync(response);
    }

    private static async Task<TodoDto> ReadTodoAsync(HttpResponseMessage response)
    {
        var item = await response.Content.ReadFromJsonAsync<TodoDto>();
        return Assert.IsType<TodoDto>(item);
    }
}

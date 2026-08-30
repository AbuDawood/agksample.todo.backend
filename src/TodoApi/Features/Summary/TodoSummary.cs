using TodoApi.Domain;

namespace TodoApi.Features.Summary;

public static class TodoSummaryEndpointModule
{
    public static IEndpointRouteBuilder MapTodoSummaryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/todos/summary", GetAsync)
            .WithName("GetTodoSummary")
            .WithTags("Todos")
            .Produces<TodoSummary>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        ITodoRepository repository,
        CancellationToken cancellationToken)
    {
        var todos = await repository.GetListAsync(cancellationToken);
        var summary = new TodoSummary(
            todos.Count,
            todos.Count(todo => todo.IsCompleted));

        return Results.Ok(summary);
    }
}

public sealed record TodoSummary(int TotalCount, int CompletedCount);

using TodoApi.Domain;

namespace TodoApi.Features.Summary;

public sealed class TodoSummary : IFeatureEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/todos/summary", GetAsync)
            .WithName("GetTodoSummary")
            .WithTags("Todos")
            .Produces<TodoSummaryResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetAsync(
        ITodoRepository repository,
        CancellationToken cancellationToken)
    {
        var todos = await repository.ListAsync(cancellationToken);
        var summary = new TodoSummaryResponse(
            todos.Count,
            todos.Count(todo => todo.IsCompleted));

        return Results.Ok(summary);
    }
}

public sealed record TodoSummaryResponse(int TotalCount, int CompletedCount);

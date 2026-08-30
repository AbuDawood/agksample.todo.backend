using TodoApi.Domain.Todos;

namespace TodoApi.Features.Summary;

public sealed record TodoSummary(int TotalCount, int CompletedCount);

public sealed class TodoSummaryEndpointModule : IFeatureEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/todos/summary", GetSummaryAsync)
            .WithName("GetTodoSummary")
            .WithTags("Todos")
            .WithSummary("Reports total and completed Todo counts.")
            .Produces<TodoSummary>();
    }

    private static async Task<IResult> GetSummaryAsync(
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

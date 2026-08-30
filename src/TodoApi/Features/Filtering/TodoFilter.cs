using TodoApi.Application;

namespace TodoApi.Features.Filtering;

/// <summary>
/// Provides the completion-aware representation of the todo collection without
/// requiring the shared todo endpoint module to know about this feature.
/// </summary>
public sealed class TodoFilter : IFeatureEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/todos", ListAsync)
            .WithName("FilterTodosByCompletion")
            .WithTags("Todos")
            .Produces<IReadOnlyList<TodoDto>>(StatusCodes.Status200OK)
            // The shared collection route remains available, while this feature's
            // lower order lets it add optional query handling without creating an
            // ambiguous match for collection requests.
            .Add(builder => ((RouteEndpointBuilder)builder).Order = -1);
    }

    private static async Task<IResult> ListAsync(
        bool? isCompleted,
        ITodoAppService todoService,
        CancellationToken cancellationToken)
    {
        var todos = await todoService.ListAsync(cancellationToken);

        if (isCompleted is not { } completionState)
        {
            return Results.Ok(todos);
        }

        return Results.Ok(todos.Where(todo => todo.IsCompleted == completionState).ToArray());
    }
}

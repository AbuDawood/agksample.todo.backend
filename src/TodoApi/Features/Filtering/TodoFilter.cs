using TodoApi.Application.Todos;

namespace TodoApi.Features.Filtering;

/// <summary>
/// Owns completion-state filtering for the Todo collection endpoint.
/// </summary>
public sealed class TodoFilter : IFeatureEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/todos", GetListAsync)
            .WithTags("Todos")
            .WithSummary("Lists Todo items, optionally filtered by completion state.")
            .Produces<IReadOnlyList<TodoDto>>()
            // The base collection route remains available while this feature-specific
            // route takes precedence for both filtered and unfiltered requests.
            .WithOrder(-1);
    }

    private static async Task<IResult> GetListAsync(
        bool? isCompleted,
        ITodoApplicationService service,
        CancellationToken cancellationToken)
    {
        var todos = await service.GetListAsync(cancellationToken);
        if (isCompleted is null)
        {
            return Results.Ok(todos);
        }

        return Results.Ok(todos.Where(todo => todo.IsCompleted == isCompleted.Value).ToArray());
    }
}

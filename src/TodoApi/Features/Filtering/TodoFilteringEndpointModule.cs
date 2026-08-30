namespace TodoApi.Features.Filtering;

// This dedicated extension point lets the filtering feature add routes without
// changing the CRUD or summary endpoint modules.
public static class TodoFilteringEndpointModule
{
    public static IEndpointRouteBuilder MapTodoFilteringEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/todos", TodoFilter.GetAsync)
            .WithOrder(-1)
            .WithTags("Todos")
            .WithName("GetTodosByCompletionState")
            .Produces(StatusCodes.Status200OK);

        return endpoints;
    }
}

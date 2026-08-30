namespace TodoApi.Features.Filtering;

// This dedicated extension point lets the filtering feature add routes without
// changing the CRUD or summary endpoint modules.
public static class TodoFilteringEndpointModule
{
    public static IEndpointRouteBuilder MapTodoFilteringEndpoints(this IEndpointRouteBuilder endpoints) =>
        endpoints;
}


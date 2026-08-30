namespace TodoApi.Features.Summary;

// This dedicated extension point lets the summary feature add routes without
// changing the CRUD or filtering endpoint modules.
public static class TodoSummaryEndpointModule
{
    public static IEndpointRouteBuilder MapTodoSummaryEndpoints(this IEndpointRouteBuilder endpoints) =>
        endpoints;
}

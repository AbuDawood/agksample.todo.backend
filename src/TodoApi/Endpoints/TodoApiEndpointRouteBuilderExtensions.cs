using TodoApi.Features.Filtering;
using TodoApi.Features.Health;
using TodoApi.Features.Summary;
using TodoApi.Features.Todos;

namespace TodoApi.Endpoints;

public static class TodoApiEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapTodoApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthEndpoints();
        endpoints.MapTodoCrudEndpoints();
        endpoints.MapTodoFilteringEndpoints();
        endpoints.MapTodoSummaryEndpoints();
        return endpoints;
    }
}

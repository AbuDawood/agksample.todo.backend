namespace TodoApi.Features.Health;

public static class HealthEndpointModule
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () => Results.Ok(new HealthResponse("Healthy")))
            .WithName("Health")
            .Produces<HealthResponse>(StatusCodes.Status200OK);

        return endpoints;
    }

    private sealed record HealthResponse(string Status);
}


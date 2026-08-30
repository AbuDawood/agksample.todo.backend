namespace TodoApi.Features.Health;

public sealed class HealthEndpoints : IFeatureEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () => Results.Ok(new HealthResponse("Healthy")))
            .WithName("GetHealth")
            .WithTags("Health")
            .Produces<HealthResponse>(StatusCodes.Status200OK);
    }

    private sealed record HealthResponse(string Status);
}


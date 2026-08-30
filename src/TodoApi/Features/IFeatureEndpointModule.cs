namespace TodoApi.Features;

/// <summary>
/// Contract for independently discoverable HTTP feature modules.
/// A new feature only needs to add an implementation in its own source path.
/// </summary>
public interface IFeatureEndpointModule
{
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}


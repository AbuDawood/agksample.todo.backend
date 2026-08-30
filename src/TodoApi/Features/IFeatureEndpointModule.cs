namespace TodoApi.Features;

/// <summary>
/// Implemented by independently owned feature modules that contribute HTTP routes.
/// Adding a module requires only a new feature source file; startup remains unchanged.
/// </summary>
public interface IFeatureEndpointModule
{
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}

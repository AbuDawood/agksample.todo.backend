using System.Reflection;

namespace TodoApi.Features;

public static class FeatureEndpointExtensions
{
    public static IEndpointRouteBuilder MapFeatureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var moduleType = typeof(IFeatureEndpointModule);
        var modules = Assembly.GetExecutingAssembly()
            .DefinedTypes
            .Where(type =>
                type is { IsAbstract: false, IsInterface: false } &&
                moduleType.IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal);

        foreach (var module in modules)
        {
            var instance = (IFeatureEndpointModule?)Activator.CreateInstance(module.AsType())
                ?? throw new InvalidOperationException($"Unable to create feature endpoint module '{module.FullName}'.");

            instance.MapEndpoints(endpoints);
        }

        return endpoints;
    }
}

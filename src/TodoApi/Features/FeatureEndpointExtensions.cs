using System.Reflection;

namespace TodoApi.Features;

public static class FeatureEndpointExtensions
{
    public static IServiceCollection AddFeatureEndpointModules(
        this IServiceCollection services,
        Assembly assembly)
    {
        var featureModuleType = typeof(IFeatureEndpointModule);
        var moduleTypes = assembly.DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false }
                && featureModuleType.IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal);

        foreach (var moduleType in moduleTypes)
        {
            services.AddSingleton(featureModuleType, moduleType);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapFeatureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        foreach (var module in endpoints.ServiceProvider.GetServices<IFeatureEndpointModule>())
        {
            module.MapEndpoints(endpoints);
        }

        return endpoints;
    }
}


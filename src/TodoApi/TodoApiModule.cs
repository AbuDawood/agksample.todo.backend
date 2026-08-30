using TodoApi.Application;
using TodoApi.Domain;
using TodoApi.Features;
using TodoApi.Infrastructure;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace TodoApi;

[DependsOn(
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpAutofacModule))]
public sealed class TodoApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton(TimeProvider.System);
        context.Services.AddSingleton<ITodoRepository, InMemoryTodoRepository>();
        context.Services.AddTransient<ITodoAppService, TodoAppService>();
        context.Services.AddFeatureEndpointModules(typeof(TodoApiModule).Assembly);
    }
}

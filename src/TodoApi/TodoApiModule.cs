using Microsoft.Extensions.DependencyInjection;
using TodoApi.Application;
using TodoApi.Domain;
using TodoApi.Infrastructure;
using Volo.Abp.Application;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace TodoApi;

[DependsOn(
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpAutofacModule),
    typeof(AbpDddApplicationModule))]
public sealed class TodoApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton(TimeProvider.System);
        context.Services.AddSingleton<ITodoRepository, InMemoryTodoRepository>();
        context.Services.AddTransient<ITodoAppService, TodoAppService>();
    }
}

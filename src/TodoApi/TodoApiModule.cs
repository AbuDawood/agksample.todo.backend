using TodoApi.Application.Todos;
using TodoApi.Domain.Todos;
using TodoApi.Infrastructure.Todos;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Application;
using Volo.Abp.Modularity;

namespace TodoApi;

[DependsOn(
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpDddApplicationModule))]
public sealed class TodoApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<ITodoRepository, InMemoryTodoRepository>();
        context.Services.AddTransient<ITodoApplicationService, TodoApplicationService>();
    }
}

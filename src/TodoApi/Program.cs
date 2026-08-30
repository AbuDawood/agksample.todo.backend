using TodoApi;
using TodoApi.Endpoints;
using Volo.Abp;
using Volo.Abp.Autofac;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseAutofac();
await builder.AddApplicationAsync<TodoApiModule>();

var app = builder.Build();

await app.InitializeApplicationAsync();
app.MapTodoApiEndpoints();

await app.RunAsync();

public partial class Program;

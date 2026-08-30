using TodoApi;
using TodoApi.Features;
using Volo.Abp;
using Volo.Abp.Autofac;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseAutofac();
await builder.AddApplicationAsync<TodoApiModule>();

var app = builder.Build();

await app.InitializeApplicationAsync();
app.MapFeatureEndpoints();

await app.RunAsync();

public partial class Program;


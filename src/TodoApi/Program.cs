using TodoApi;
using TodoApi.Features;
using Volo.Abp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
await builder.AddApplicationAsync<TodoApiModule>();

var app = builder.Build();

await app.InitializeApplicationAsync();

app.UseExceptionHandler();

app.MapGet("/health", () => Results.Ok(new HealthResponse("Healthy")))
    .WithName("Health")
    .WithSummary("Reports whether the Todo API process is healthy.")
    .Produces<HealthResponse>();

app.MapFeatureEndpoints();

await app.RunAsync();

public sealed record HealthResponse(string Status);

public partial class Program;

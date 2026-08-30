using TodoApi.Application;

namespace TodoApi.Features.Todos;

public static class TodoCrudEndpointModule
{
    public static IEndpointRouteBuilder MapTodoCrudEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var todos = endpoints.MapGroup("/api/todos").WithTags("Todos");

        todos.MapPost("/", CreateAsync)
            .WithName("CreateTodo")
            .Produces<TodoDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        todos.MapGet("/", GetListAsync)
            .WithName("GetTodos")
            .Produces<IReadOnlyList<TodoDto>>(StatusCodes.Status200OK);

        todos.MapGet("/{id:guid}", GetAsync)
            .WithName("GetTodo")
            .Produces<TodoDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        todos.MapPut("/{id:guid}", UpsertAsync)
            .WithName("UpsertTodo")
            .Produces<TodoDto>(StatusCodes.Status200OK)
            .Produces<TodoDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        todos.MapPatch("/{id:guid}/completion", SetCompletionAsync)
            .WithName("SetTodoCompletion")
            .Produces<TodoDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        todos.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteTodo")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateTodoRequest request,
        ITodoAppService service,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return ValidationProblem("Title is required.");
        }

        var item = await service.CreateAsync(request, cancellationToken);
        return Results.Created($"/api/todos/{item.Id}", item);
    }

    private static async Task<IResult> GetListAsync(
        ITodoAppService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.GetListAsync(cancellationToken));

    private static async Task<IResult> GetAsync(
        Guid id,
        ITodoAppService service,
        CancellationToken cancellationToken)
    {
        var item = await service.GetAsync(id, cancellationToken);
        return item is null ? NotFound(id) : Results.Ok(item);
    }

    private static async Task<IResult> UpsertAsync(
        Guid id,
        UpsertTodoRequest request,
        ITodoAppService service,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return ValidationProblem("Title is required.");
        }

        var result = await service.UpsertAsync(id, request, cancellationToken);
        return result.WasCreated
            ? Results.Created($"/api/todos/{id}", result.Item)
            : Results.Ok(result.Item);
    }

    private static async Task<IResult> SetCompletionAsync(
        Guid id,
        UpdateCompletionRequest request,
        ITodoAppService service,
        CancellationToken cancellationToken)
    {
        if (request.Completed is null)
        {
            return ValidationProblem("Completed must be provided.");
        }

        var item = await service.SetCompletionAsync(id, request.Completed.Value, cancellationToken);
        return item is null ? NotFound(id) : Results.Ok(item);
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        ITodoAppService service,
        CancellationToken cancellationToken) =>
        await service.DeleteAsync(id, cancellationToken)
            ? Results.NoContent()
            : NotFound(id);

    private static IResult ValidationProblem(string detail) =>
        Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Validation failed",
            detail: detail);

    private static IResult NotFound(Guid id) =>
        Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Todo not found",
            detail: $"No Todo with id '{id}' exists.");
}


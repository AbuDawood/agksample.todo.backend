using TodoApi.Application.Todos;

namespace TodoApi.Features.Todos;

public sealed class TodoEndpointModule : IFeatureEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapTodoEndpoints();
}

public static class TodoEndpointExtensions
{
    public static RouteGroupBuilder MapTodoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/todos")
            .WithTags("Todos");

        group.MapPost("", CreateAsync)
            .WithName("CreateTodo")
            .WithSummary("Creates a Todo item.")
            .Produces<TodoDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("", GetListAsync)
            .WithName("ListTodos")
            .WithSummary("Lists all Todo items.")
            .Produces<IReadOnlyList<TodoDto>>();

        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetTodo")
            .WithSummary("Gets a Todo item by id.")
            .Produces<TodoDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpsertAsync)
            .WithName("UpsertTodo")
            .WithSummary("Creates an absent Todo or replaces an existing Todo.")
            .Produces<TodoDto>()
            .Produces<TodoDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPatch("/{id:guid}/completion", SetCompletionAsync)
            .WithName("SetTodoCompletion")
            .WithSummary("Sets the explicit completion state of a Todo item.")
            .Produces<TodoDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteTodo")
            .WithSummary("Deletes a Todo item.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> CreateAsync(
        CreateTodoRequest? request,
        ITodoApplicationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(
            request ?? new CreateTodoRequest(null, null),
            cancellationToken);

        if (!result.IsValid)
        {
            return Results.ValidationProblem(result.Errors);
        }

        var todo = result.Todo!;
        return Results.Created($"/api/todos/{todo.Id}", todo);
    }

    private static async Task<IResult> GetListAsync(
        ITodoApplicationService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.GetListAsync(cancellationToken));

    private static async Task<IResult> GetAsync(
        Guid id,
        ITodoApplicationService service,
        CancellationToken cancellationToken)
    {
        var todo = await service.FindAsync(id, cancellationToken);
        return todo is null ? NotFound(id) : Results.Ok(todo);
    }

    private static async Task<IResult> UpsertAsync(
        Guid id,
        UpsertTodoRequest? request,
        ITodoApplicationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpsertAsync(
            id,
            request ?? new UpsertTodoRequest(null, null),
            cancellationToken);

        if (!result.IsValid)
        {
            return Results.ValidationProblem(result.Errors);
        }

        var todo = result.Todo!;
        return result.Created
            ? Results.Created($"/api/todos/{todo.Id}", todo)
            : Results.Ok(todo);
    }

    private static async Task<IResult> SetCompletionAsync(
        Guid id,
        SetTodoCompletionRequest? request,
        ITodoApplicationService service,
        CancellationToken cancellationToken)
    {
        if (request?.Completed is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["completed"] = ["The completed field is required."]
            });
        }

        var todo = await service.SetCompletionAsync(
            id,
            request.Completed.Value,
            cancellationToken);

        return todo is null ? NotFound(id) : Results.Ok(todo);
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        ITodoApplicationService service,
        CancellationToken cancellationToken) =>
        await service.DeleteAsync(id, cancellationToken)
            ? Results.NoContent()
            : NotFound(id);

    private static IResult NotFound(Guid id) =>
        Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Todo not found",
            detail: $"No Todo with id '{id}' exists.");
}

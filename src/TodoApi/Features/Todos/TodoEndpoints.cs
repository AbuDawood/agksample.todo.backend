using TodoApi.Application;

namespace TodoApi.Features.Todos;

public sealed class TodoEndpoints : IFeatureEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var todos = endpoints.MapGroup("/api/todos")
            .WithTags("Todos");

        todos.MapPost("", CreateAsync)
            .WithName("CreateTodo")
            .Produces<TodoDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        todos.MapGet("", ListAsync)
            .WithName("ListTodos")
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
            .Produces(StatusCodes.Status204NoContent);
    }

    private static async Task<IResult> CreateAsync(
        CreateTodoRequest request,
        ITodoAppService todoService,
        CancellationToken cancellationToken)
    {
        if (HasInvalidTitle(request.Title))
        {
            return InvalidTitle();
        }

        var todo = await todoService.CreateAsync(request, cancellationToken);
        return Results.Created(TodoLocation(todo.Id), todo);
    }

    private static async Task<IResult> ListAsync(
        ITodoAppService todoService,
        CancellationToken cancellationToken)
    {
        var todos = await todoService.ListAsync(cancellationToken);
        return Results.Ok(todos);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ITodoAppService todoService,
        CancellationToken cancellationToken)
    {
        var todo = await todoService.GetAsync(id, cancellationToken);
        return todo is null ? TodoNotFound(id) : Results.Ok(todo);
    }

    private static async Task<IResult> UpsertAsync(
        Guid id,
        UpsertTodoRequest request,
        ITodoAppService todoService,
        CancellationToken cancellationToken)
    {
        if (HasInvalidTitle(request.Title))
        {
            return InvalidTitle();
        }

        var result = await todoService.UpsertAsync(id, request, cancellationToken);
        return result.WasCreated
            ? Results.Created(TodoLocation(id), result.Todo)
            : Results.Ok(result.Todo);
    }

    private static async Task<IResult> SetCompletionAsync(
        Guid id,
        SetTodoCompletionRequest request,
        ITodoAppService todoService,
        CancellationToken cancellationToken)
    {
        if (request.Completed is null)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["completed"] = ["The completed field is required."]
                },
                statusCode: StatusCodes.Status400BadRequest,
                title: "One or more validation errors occurred.");
        }

        var todo = await todoService.SetCompletionAsync(
            id,
            request.Completed.Value,
            cancellationToken);

        return todo is null ? TodoNotFound(id) : Results.Ok(todo);
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        ITodoAppService todoService,
        CancellationToken cancellationToken)
    {
        await todoService.DeleteAsync(id, cancellationToken);
        return Results.NoContent();
    }

    private static bool HasInvalidTitle(string? title) => string.IsNullOrWhiteSpace(title);

    private static IResult InvalidTitle() => Results.ValidationProblem(
        new Dictionary<string, string[]>
        {
            ["title"] = ["The title field is required and cannot be blank."]
        },
        statusCode: StatusCodes.Status400BadRequest,
        title: "One or more validation errors occurred.");

    private static IResult TodoNotFound(Guid id) => Results.Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Todo not found",
        detail: $"No todo with id '{id:D}' exists.");

    private static string TodoLocation(Guid id) => $"/api/todos/{id:D}";
}


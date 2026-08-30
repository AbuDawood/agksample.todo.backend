namespace TodoApi.Application.Todos;

public sealed record TodoDto(
    Guid Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateTodoRequest(string? Title, string? Description);

public sealed record UpsertTodoRequest(string? Title, string? Description);

public sealed record SetTodoCompletionRequest(bool? Completed);

public sealed record TodoMutationResult(
    TodoDto? Todo,
    bool Created,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public bool IsValid => Errors.Count == 0;

    public static TodoMutationResult ValidationFailure(string field, string message) =>
        new(null, false, new Dictionary<string, string[]>
        {
            [field] = [message]
        });

    public static TodoMutationResult Success(TodoDto todo, bool created) =>
        new(todo, created, new Dictionary<string, string[]>());
}

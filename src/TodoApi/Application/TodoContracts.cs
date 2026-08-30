namespace TodoApi.Application;

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

public sealed record TodoUpsertResult(TodoDto Todo, bool WasCreated);


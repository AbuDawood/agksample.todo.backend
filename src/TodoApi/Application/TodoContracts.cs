namespace TodoApi.Application;

public sealed record TodoDto(
    Guid Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed class CreateTodoRequest
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public bool IsCompleted { get; init; }
}

public sealed class UpsertTodoRequest
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public bool IsCompleted { get; init; }
}

public sealed class UpdateCompletionRequest
{
    public bool? Completed { get; init; }
}

public sealed record TodoUpsertDto(TodoDto Item, bool WasCreated);


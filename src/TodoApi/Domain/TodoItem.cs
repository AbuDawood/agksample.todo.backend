namespace TodoApi.Domain;

public sealed record TodoItem(
    Guid Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);


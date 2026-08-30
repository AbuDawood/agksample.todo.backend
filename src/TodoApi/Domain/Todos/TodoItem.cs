namespace TodoApi.Domain.Todos;

/// <summary>
/// The immutable domain representation of a Todo item.
/// </summary>
public sealed record TodoItem(
    Guid Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static TodoItem Create(
        Guid id,
        string title,
        string? description,
        DateTimeOffset createdAtUtc) =>
        new(id, title, description, false, createdAtUtc, createdAtUtc);

    public TodoItem UpdateDetails(string title, string? description, DateTimeOffset changedAtUtc)
    {
        if (Title == title && Description == description)
        {
            return this;
        }

        return this with
        {
            Title = title,
            Description = description,
            UpdatedAtUtc = EnsureLaterTimestamp(changedAtUtc)
        };
    }

    public TodoItem SetCompletion(bool completed, DateTimeOffset changedAtUtc)
    {
        if (IsCompleted == completed)
        {
            return this;
        }

        return this with
        {
            IsCompleted = completed,
            UpdatedAtUtc = EnsureLaterTimestamp(changedAtUtc)
        };
    }

    private DateTimeOffset EnsureLaterTimestamp(DateTimeOffset timestamp) =>
        timestamp > UpdatedAtUtc ? timestamp : UpdatedAtUtc.AddTicks(1);
}

namespace TodoApi.Domain;

public sealed class TodoItem
{
    public Guid Id { get; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public TodoItem(
        Guid id,
        string title,
        string? description,
        bool isCompleted,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        Title = title;
        Description = description;
        IsCompleted = isCompleted;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    internal bool Replace(string title, string? description, bool isCompleted, DateTimeOffset changedAtUtc)
    {
        if (Title == title && Description == description && IsCompleted == isCompleted)
        {
            return false;
        }

        Title = title;
        Description = description;
        IsCompleted = isCompleted;
        UpdatedAtUtc = changedAtUtc;
        return true;
    }

    internal bool SetCompletion(bool completed, DateTimeOffset changedAtUtc)
    {
        if (IsCompleted == completed)
        {
            return false;
        }

        IsCompleted = completed;
        UpdatedAtUtc = changedAtUtc;
        return true;
    }

    internal TodoItem Snapshot() =>
        new(Id, Title, Description, IsCompleted, CreatedAtUtc, UpdatedAtUtc);
}


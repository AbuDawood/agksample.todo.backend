namespace TodoApi.Domain;

public interface ITodoRepository
{
    Task<TodoItem> InsertAsync(
        Guid id,
        string title,
        string? description,
        bool isCompleted,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TodoItem>> GetListAsync(CancellationToken cancellationToken = default);

    Task<TodoItem?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TodoUpsertResult> UpsertAsync(
        Guid id,
        string title,
        string? description,
        bool isCompleted,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<TodoItem?> SetCompletionAsync(
        Guid id,
        bool completed,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record TodoUpsertResult(TodoItem Item, bool WasCreated);


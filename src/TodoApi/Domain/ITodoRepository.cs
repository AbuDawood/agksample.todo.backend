namespace TodoApi.Domain;

public interface ITodoRepository
{
    ValueTask<bool> TryInsertAsync(TodoItem item, CancellationToken cancellationToken = default);

    ValueTask<TodoItem?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<TodoItem>> ListAsync(CancellationToken cancellationToken = default);

    ValueTask<TodoRepositoryUpsertResult> UpsertAsync(
        Guid id,
        Func<TodoItem> create,
        Func<TodoItem, TodoItem> update,
        CancellationToken cancellationToken = default);

    ValueTask<TodoItem?> UpdateAsync(
        Guid id,
        Func<TodoItem, TodoItem> update,
        CancellationToken cancellationToken = default);

    ValueTask<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record TodoRepositoryUpsertResult(TodoItem Item, bool WasCreated);


using TodoApi.Domain;

namespace TodoApi.Infrastructure;

/// <summary>
/// A process-lifetime repository for the acceptance host. A single lock makes
/// read-modify-write operations deterministic and prevents partial upserts.
/// </summary>
public sealed class InMemoryTodoRepository : ITodoRepository
{
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, TodoItem> _items = [];

    public ValueTask<bool> TryInsertAsync(TodoItem item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return ValueTask.FromResult(_items.TryAdd(item.Id, item));
        }
    }

    public ValueTask<TodoItem?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return ValueTask.FromResult(_items.GetValueOrDefault(id));
        }
    }

    public ValueTask<IReadOnlyList<TodoItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            IReadOnlyList<TodoItem> snapshot = _items.Values
                .OrderBy(item => item.CreatedAtUtc)
                .ThenBy(item => item.Id)
                .ToArray();

            return ValueTask.FromResult(snapshot);
        }
    }

    public ValueTask<TodoRepositoryUpsertResult> UpsertAsync(
        Guid id,
        Func<TodoItem> create,
        Func<TodoItem, TodoItem> update,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_items.TryGetValue(id, out var current))
            {
                var updated = update(current);
                _items[id] = updated;
                return ValueTask.FromResult(new TodoRepositoryUpsertResult(updated, WasCreated: false));
            }

            var created = create();
            if (created.Id != id)
            {
                throw new InvalidOperationException("The upsert factory returned an item with a different id.");
            }

            _items.Add(id, created);
            return ValueTask.FromResult(new TodoRepositoryUpsertResult(created, WasCreated: true));
        }
    }

    public ValueTask<TodoItem?> UpdateAsync(
        Guid id,
        Func<TodoItem, TodoItem> update,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_items.TryGetValue(id, out var current))
            {
                return ValueTask.FromResult<TodoItem?>(null);
            }

            var updated = update(current);
            _items[id] = updated;
            return ValueTask.FromResult<TodoItem?>(updated);
        }
    }

    public ValueTask<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return ValueTask.FromResult(_items.Remove(id));
        }
    }
}


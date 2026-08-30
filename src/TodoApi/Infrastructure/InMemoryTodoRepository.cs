using TodoApi.Domain;

namespace TodoApi.Infrastructure;

public sealed class InMemoryTodoRepository : ITodoRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, TodoItem> _items = [];

    public Task<TodoItem> InsertAsync(
        Guid id,
        string title,
        string? description,
        bool isCompleted,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_items.ContainsKey(id))
            {
                throw new InvalidOperationException($"A Todo with id '{id}' already exists.");
            }

            var item = new TodoItem(id, title, description, isCompleted, now, now);
            _items.Add(id, item);
            return Task.FromResult(item.Snapshot());
        }
    }

    public Task<IReadOnlyList<TodoItem>> GetListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            IReadOnlyList<TodoItem> items = _items.Values
                .OrderBy(item => item.CreatedAtUtc)
                .ThenBy(item => item.Id)
                .Select(item => item.Snapshot())
                .ToArray();

            return Task.FromResult(items);
        }
    }

    public Task<TodoItem?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return Task.FromResult(_items.TryGetValue(id, out var item) ? item.Snapshot() : null);
        }
    }

    public Task<TodoUpsertResult> UpsertAsync(
        Guid id,
        string title,
        string? description,
        bool isCompleted,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_items.TryGetValue(id, out var existing))
            {
                existing.Replace(title, description, isCompleted, now);
                return Task.FromResult(new TodoUpsertResult(existing.Snapshot(), false));
            }

            var created = new TodoItem(id, title, description, isCompleted, now, now);
            _items.Add(id, created);
            return Task.FromResult(new TodoUpsertResult(created.Snapshot(), true));
        }
    }

    public Task<TodoItem?> SetCompletionAsync(
        Guid id,
        bool completed,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_items.TryGetValue(id, out var item))
            {
                return Task.FromResult<TodoItem?>(null);
            }

            item.SetCompletion(completed, now);
            return Task.FromResult<TodoItem?>(item.Snapshot());
        }
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return Task.FromResult(_items.Remove(id));
        }
    }
}


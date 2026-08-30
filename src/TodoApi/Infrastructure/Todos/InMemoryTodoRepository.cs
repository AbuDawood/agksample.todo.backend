using TodoApi.Domain.Todos;

namespace TodoApi.Infrastructure.Todos;

/// <summary>
/// A thread-safe process-lifetime repository used by this self-contained host.
/// </summary>
public sealed class InMemoryTodoRepository : ITodoRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, TodoItem> _todos = new();

    public Task<TodoItem?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return Task.FromResult(_todos.GetValueOrDefault(id));
        }
    }

    public Task<IReadOnlyList<TodoItem>> GetListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            IReadOnlyList<TodoItem> snapshot = _todos.Values
                .OrderBy(todo => todo.CreatedAtUtc)
                .ThenBy(todo => todo.Id)
                .ToArray();

            return Task.FromResult(snapshot);
        }
    }

    public Task AddAsync(TodoItem todo, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_todos.TryAdd(todo.Id, todo))
            {
                throw new InvalidOperationException($"A Todo with id '{todo.Id}' already exists.");
            }
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(TodoItem todo, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_todos.ContainsKey(todo.Id))
            {
                throw new KeyNotFoundException($"A Todo with id '{todo.Id}' does not exist.");
            }

            _todos[todo.Id] = todo;
        }

        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return Task.FromResult(_todos.Remove(id));
        }
    }
}

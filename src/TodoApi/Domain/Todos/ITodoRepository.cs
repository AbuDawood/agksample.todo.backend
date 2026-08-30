namespace TodoApi.Domain.Todos;

/// <summary>
/// Persistence boundary for Todo aggregate operations.
/// </summary>
public interface ITodoRepository
{
    Task<TodoItem?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TodoItem>> GetListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(TodoItem todo, CancellationToken cancellationToken = default);

    Task UpdateAsync(TodoItem todo, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

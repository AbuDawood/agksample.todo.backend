namespace TodoApi.Application.Todos;

/// <summary>
/// Application boundary used by the HTTP feature module.
/// </summary>
public interface ITodoApplicationService
{
    Task<TodoMutationResult> CreateAsync(
        CreateTodoRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TodoDto>> GetListAsync(CancellationToken cancellationToken = default);

    Task<TodoDto?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TodoMutationResult> UpsertAsync(
        Guid id,
        UpsertTodoRequest request,
        CancellationToken cancellationToken = default);

    Task<TodoDto?> SetCompletionAsync(
        Guid id,
        bool completed,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

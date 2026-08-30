namespace TodoApi.Application;

public interface ITodoAppService
{
    Task<TodoDto> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TodoDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<TodoDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TodoUpsertResult> UpsertAsync(
        Guid id,
        UpsertTodoRequest request,
        CancellationToken cancellationToken = default);

    Task<TodoDto?> SetCompletionAsync(
        Guid id,
        bool completed,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}


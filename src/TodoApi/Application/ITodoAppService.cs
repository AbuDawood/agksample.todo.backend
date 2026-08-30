namespace TodoApi.Application;

public interface ITodoAppService
{
    Task<TodoDto> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TodoDto>> GetListAsync(CancellationToken cancellationToken = default);

    Task<TodoDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TodoUpsertDto> UpsertAsync(
        Guid id,
        UpsertTodoRequest request,
        CancellationToken cancellationToken = default);

    Task<TodoDto?> SetCompletionAsync(
        Guid id,
        bool completed,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}


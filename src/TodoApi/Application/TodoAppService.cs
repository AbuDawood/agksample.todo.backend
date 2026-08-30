using TodoApi.Domain;
using Volo.Abp.Application.Services;

namespace TodoApi.Application;

public class TodoAppService : ApplicationService, ITodoAppService
{
    private readonly ITodoRepository _repository;
    private readonly TimeProvider _timeProvider;

    public TodoAppService(ITodoRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<TodoDto> CreateAsync(
        CreateTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        var (title, description) = Normalize(request.Title, request.Description);

        while (true)
        {
            try
            {
                var item = await _repository.InsertAsync(
                    Guid.NewGuid(),
                    title,
                    description,
                    request.IsCompleted,
                    _timeProvider.GetUtcNow(),
                    cancellationToken);

                return Map(item);
            }
            catch (InvalidOperationException)
            {
                // A GUID collision is extraordinarily unlikely, but retrying keeps create atomic.
            }
        }
    }

    public async Task<IReadOnlyList<TodoDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetListAsync(cancellationToken);
        return items.Select(Map).ToArray();
    }

    public async Task<TodoDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _repository.FindAsync(id, cancellationToken);
        return item is null ? null : Map(item);
    }

    public async Task<TodoUpsertDto> UpsertAsync(
        Guid id,
        UpsertTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        var (title, description) = Normalize(request.Title, request.Description);
        var result = await _repository.UpsertAsync(
            id,
            title,
            description,
            request.IsCompleted,
            _timeProvider.GetUtcNow(),
            cancellationToken);

        return new TodoUpsertDto(Map(result.Item), result.WasCreated);
    }

    public async Task<TodoDto?> SetCompletionAsync(
        Guid id,
        bool completed,
        CancellationToken cancellationToken = default)
    {
        var item = await _repository.SetCompletionAsync(
            id,
            completed,
            _timeProvider.GetUtcNow(),
            cancellationToken);

        return item is null ? null : Map(item);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        _repository.DeleteAsync(id, cancellationToken);

    private static (string Title, string? Description) Normalize(string? title, string? description)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        return (title.Trim(), normalizedDescription);
    }

    private static TodoDto Map(TodoItem item) =>
        new(
            item.Id,
            item.Title,
            item.Description,
            item.IsCompleted,
            item.CreatedAtUtc,
            item.UpdatedAtUtc);
}

using TodoApi.Domain;
using Volo.Abp.Application.Services;

namespace TodoApi.Application;

public class TodoAppService(
    ITodoRepository repository,
    TimeProvider timeProvider) : ApplicationService, ITodoAppService
{
    public virtual async Task<TodoDto> CreateAsync(
        CreateTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        var title = NormalizeRequiredTitle(request.Title);

        while (true)
        {
            var now = GetUtcNow();
            var item = new TodoItem(
                Guid.NewGuid(),
                title,
                request.Description,
                IsCompleted: false,
                now,
                now);

            if (await repository.TryInsertAsync(item, cancellationToken))
            {
                return ToDto(item);
            }
        }
    }

    public virtual async Task<IReadOnlyList<TodoDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var items = await repository.ListAsync(cancellationToken);
        return items.Select(ToDto).ToArray();
    }

    public virtual async Task<TodoDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await repository.FindAsync(id, cancellationToken);
        return item is null ? null : ToDto(item);
    }

    public virtual async Task<TodoUpsertResult> UpsertAsync(
        Guid id,
        UpsertTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        var title = NormalizeRequiredTitle(request.Title);
        var now = GetUtcNow();

        var result = await repository.UpsertAsync(
            id,
            create: () => new TodoItem(
                id,
                title,
                request.Description,
                IsCompleted: false,
                now,
                now),
            update: current => UpdateContent(current, title, request.Description, now),
            cancellationToken);

        return new TodoUpsertResult(ToDto(result.Item), result.WasCreated);
    }

    public virtual async Task<TodoDto?> SetCompletionAsync(
        Guid id,
        bool completed,
        CancellationToken cancellationToken = default)
    {
        var now = GetUtcNow();
        var item = await repository.UpdateAsync(
            id,
            current => current.IsCompleted == completed
                ? current
                : current with { IsCompleted = completed, UpdatedAtUtc = now },
            cancellationToken);

        return item is null ? null : ToDto(item);
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await repository.DeleteAsync(id, cancellationToken);
    }

    private DateTimeOffset GetUtcNow() => timeProvider.GetUtcNow();

    private static TodoItem UpdateContent(
        TodoItem current,
        string title,
        string? description,
        DateTimeOffset updatedAtUtc)
    {
        if (current.Title == title && current.Description == description)
        {
            return current;
        }

        return current with
        {
            Title = title,
            Description = description,
            UpdatedAtUtc = updatedAtUtc
        };
    }

    private static string NormalizeRequiredTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("A todo title is required.", nameof(title));
        }

        return title.Trim();
    }

    private static TodoDto ToDto(TodoItem item) => new(
        item.Id,
        item.Title,
        item.Description,
        item.IsCompleted,
        item.CreatedAtUtc,
        item.UpdatedAtUtc);
}

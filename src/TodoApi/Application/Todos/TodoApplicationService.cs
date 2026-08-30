using TodoApi.Domain.Todos;
using Volo.Abp.Application.Services;

namespace TodoApi.Application.Todos;

public sealed class TodoApplicationService(ITodoRepository repository)
    : ApplicationService, ITodoApplicationService
{
    public async Task<TodoMutationResult> CreateAsync(
        CreateTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationFailure = ValidateTitle(request.Title);
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var todo = TodoItem.Create(
            Guid.NewGuid(),
            request.Title!,
            request.Description,
            DateTimeOffset.UtcNow);

        await repository.AddAsync(todo, cancellationToken);
        return TodoMutationResult.Success(Map(todo), created: true);
    }

    public async Task<IReadOnlyList<TodoDto>> GetListAsync(
        CancellationToken cancellationToken = default)
    {
        var todos = await repository.GetListAsync(cancellationToken);
        return todos.Select(Map).ToArray();
    }

    public async Task<TodoDto?> FindAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var todo = await repository.FindAsync(id, cancellationToken);
        return todo is null ? null : Map(todo);
    }

    public async Task<TodoMutationResult> UpsertAsync(
        Guid id,
        UpsertTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationFailure = ValidateTitle(request.Title);
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var existing = await repository.FindAsync(id, cancellationToken);
        if (existing is null)
        {
            var created = TodoItem.Create(
                id,
                request.Title!,
                request.Description,
                DateTimeOffset.UtcNow);

            await repository.AddAsync(created, cancellationToken);
            return TodoMutationResult.Success(Map(created), created: true);
        }

        var updated = existing.UpdateDetails(
            request.Title!,
            request.Description,
            DateTimeOffset.UtcNow);

        if (!ReferenceEquals(updated, existing))
        {
            await repository.UpdateAsync(updated, cancellationToken);
        }

        return TodoMutationResult.Success(Map(updated), created: false);
    }

    public async Task<TodoDto?> SetCompletionAsync(
        Guid id,
        bool completed,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.FindAsync(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var updated = existing.SetCompletion(completed, DateTimeOffset.UtcNow);
        if (!ReferenceEquals(updated, existing))
        {
            await repository.UpdateAsync(updated, cancellationToken);
        }

        return Map(updated);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(id, cancellationToken);

    private static TodoMutationResult? ValidateTitle(string? title) =>
        string.IsNullOrWhiteSpace(title)
            ? TodoMutationResult.ValidationFailure("title", "The title field is required.")
            : null;

    private static TodoDto Map(TodoItem todo) =>
        new(
            todo.Id,
            todo.Title,
            todo.Description,
            todo.IsCompleted,
            todo.CreatedAtUtc,
            todo.UpdatedAtUtc);
}

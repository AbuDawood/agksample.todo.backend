using TodoApi.Application;

namespace TodoApi.Features.Filtering;

public static class TodoFilter
{
    public static async Task<IResult> GetAsync(
        bool? isCompleted,
        ITodoAppService service,
        CancellationToken cancellationToken)
    {
        var items = await service.GetListAsync(cancellationToken);

        if (isCompleted is null)
        {
            return Results.Ok(items);
        }

        var filteredItems = items
            .Where(item => item.IsCompleted == isCompleted.Value)
            .ToArray();

        return Results.Ok(filteredItems);
    }
}

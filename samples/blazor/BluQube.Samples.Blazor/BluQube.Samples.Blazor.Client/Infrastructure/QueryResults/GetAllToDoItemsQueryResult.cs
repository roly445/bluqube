using BluQube.Queries;

namespace BluQube.Samples.Blazor.Client.Infrastructure.QueryResults;

public record GetAllToDoItemsQueryResult(IReadOnlyList<GetAllToDoItemsQueryResult.ToDoItem> ToDoItems) : IQueryResult
{
    public record ToDoItem(Guid Id, string Title, bool IsCompleted);
};
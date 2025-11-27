using BluQube.Queries;

namespace BluQube.Samples.Blazor.Client.Infrastructure.QueryResults;

public record GetAllToDoItemsQueryAnswer(IReadOnlyList<GetAllToDoItemsQueryAnswer.ToDoItem> ToDoItems) : IQueryResult
{
    public record ToDoItem(Guid Id, string Title, bool IsCompleted);
}
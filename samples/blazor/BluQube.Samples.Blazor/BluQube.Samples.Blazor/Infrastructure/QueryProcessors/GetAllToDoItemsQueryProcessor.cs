using BluQube.Queries;
using BluQube.Samples.Blazor.Client.Infrastructure.Queries;
using BluQube.Samples.Blazor.Client.Infrastructure.QueryResults;
using BluQube.Samples.Blazor.Infrastructure.Data;

namespace BluQube.Samples.Blazor.Infrastructure.QueryProcessors;

public class GetAllToDoItemsQueryProcessor(ITodoService todoService) : IQueryProcessor<GetAllToDoItemsQuery, GetAllToDoItemsQueryAnswer>
{
    public Task<QueryResult<GetAllToDoItemsQueryAnswer>> Handle(GetAllToDoItemsQuery request, CancellationToken cancellationToken)
    {
        var result = new GetAllToDoItemsQueryAnswer(
            todoService.Todos.Select(x => new GetAllToDoItemsQueryAnswer.ToDoItem(x.Id, x.Title, x.IsCompleted))
                .ToList());

        return Task.FromResult(QueryResult<GetAllToDoItemsQueryAnswer>.Succeeded(result));
    }
}
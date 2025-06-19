using BluQube.Queries;
using BluQube.Samples.Blazor.Client.Infrastructure.Queries;
using BluQube.Samples.Blazor.Client.Infrastructure.QueryResults;
using BluQube.Samples.Blazor.Infrastructure.Data;

namespace BluQube.Samples.Blazor.Infrastructure.QueryProcessors;

public class GetAllToDoItemsQueryProcessor(ITodoService todoService) : IQueryProcessor<GetAllToDoItemsQuery, GetAllToDoItemsQueryResult>
{
    public Task<QueryResult<GetAllToDoItemsQueryResult>> Handle(GetAllToDoItemsQuery request, CancellationToken cancellationToken)
    {
        var result = new GetAllToDoItemsQueryResult(
            todoService.Todos.Select(x => new GetAllToDoItemsQueryResult.ToDoItem(x.Id, x.Title, x.IsCompleted))
                .ToList());

        return Task.FromResult(QueryResult<GetAllToDoItemsQueryResult>.Succeeded(result));
    }
}
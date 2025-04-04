using BluQube.Queries;
using BluQube.Samples.Blazor.Client.Infrastructure.Queries;
using BluQube.Samples.Blazor.Client.Infrastructure.QueryResults;
using BluQube.Samples.Blazor.Infrastructure.Data;

namespace BluQube.Samples.Blazor.Infrastructure.QueryProcessors;

public class ToDoStatsQueryProcessor(ITodoService todoService) : IQueryProcessor<ToDoStatsQuery, ToDoStatsQueryResult>
{
    public Task<QueryResult<ToDoStatsQueryResult>> Handle(ToDoStatsQuery request, CancellationToken cancellationToken)
    {
        var result = new ToDoStatsQueryResult(
            todoService.Todos.Count(),
            todoService.Todos.Count(t => t.IsCompleted));
        
        return Task.FromResult(QueryResult<ToDoStatsQueryResult>.Succeeded(result));
    }
}
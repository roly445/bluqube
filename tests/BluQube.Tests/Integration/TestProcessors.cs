using BluQube.Queries;

namespace BluQube.Tests.Integration;

public class GetItemQueryProcessor : IQueryProcessor<GetItemQuery, ItemResult>
{
    public Task<QueryResult<ItemResult>> Handle(GetItemQuery request, CancellationToken cancellationToken)
    {
        // Verify we received both path param (Id) and querystring param (Filter)
        if (request.Id == Guid.Empty)
        {
            return Task.FromResult(QueryResult<ItemResult>.Failed());
        }

        var result = new ItemResult(request.Id, request.Filter ?? "no-filter");
        return Task.FromResult(QueryResult<ItemResult>.Succeeded(result));
    }
}

public class ListTodosQueryProcessor : IQueryProcessor<ListTodosQuery, TodoListResult>
{
    public Task<QueryResult<TodoListResult>> Handle(ListTodosQuery request, CancellationToken cancellationToken)
    {
        // Verify nullable querystring parameter handling
        var items = new List<string>();
        if (request.Status != null)
        {
            items.Add($"status:{request.Status}");
        }
        else
        {
            items.Add("status:null");
        }

        var result = new TodoListResult(items);
        return Task.FromResult(QueryResult<TodoListResult>.Succeeded(result));
    }
}

public class SearchQueryProcessor : IQueryProcessor<SearchQuery, SearchResult>
{
    public Task<QueryResult<SearchResult>> Handle(SearchQuery request, CancellationToken cancellationToken)
    {
        // Verify path param (Category) and body param (ComplexFilter) both present
        if (string.IsNullOrEmpty(request.Category))
        {
            return Task.FromResult(QueryResult<SearchResult>.Failed());
        }

        var results = new List<string>
        {
            $"category:{request.Category}",
            $"keyword:{request.Filter.KeywordFilter ?? "null"}",
            $"minscore:{request.Filter.MinScore?.ToString() ?? "null"}",
        };

        var result = new SearchResult(results);
        return Task.FromResult(QueryResult<SearchResult>.Succeeded(result));
    }
}
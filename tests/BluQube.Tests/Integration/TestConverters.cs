using BluQube.Queries;

namespace BluQube.Tests.Integration;

// Concrete converters for test query results
public class ItemResultConverter : QueryResultConverter<ItemResult>
{
}

public class TodoListResultConverter : QueryResultConverter<TodoListResult>
{
}

public class SearchResultConverter : QueryResultConverter<SearchResult>
{
}

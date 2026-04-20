using BluQube.Attributes;
using BluQube.Queries;

namespace BluQube.Tests.Integration;

[BluQubeQuery(Path = "test/item/{id}", Method = "GET")]
public record GetItemQuery(Guid Id, string? Filter) : IQuery<ItemResult>;

[BluQubeQuery(Path = "test/todos", Method = "GET")]
public record ListTodosQuery(string? Status) : IQuery<TodoListResult>;

[BluQubeQuery(Path = "test/search/{category}")]
public record SearchQuery(string Category, ComplexFilter Filter) : IQuery<SearchResult>;

public record ItemResult(Guid Id, string Name) : IQueryResult;

public record TodoListResult(List<string> Items) : IQueryResult;

public record SearchResult(List<string> Results) : IQueryResult;

public record ComplexFilter(string? KeywordFilter, int? MinScore);

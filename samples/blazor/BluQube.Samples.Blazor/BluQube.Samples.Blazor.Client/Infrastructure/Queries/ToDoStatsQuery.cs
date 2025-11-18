using BluQube.Attributes;
using BluQube.Constants;
using BluQube.Queries;
using BluQube.Samples.Blazor.Client.Infrastructure.QueryResults;

namespace BluQube.Samples.Blazor.Client.Infrastructure.Queries;

[BluQubeQuery(Path = "queries/todo/get-stats", HttpMethod = HttpRequestMethod.Post)]
public record ToDoStatsQuery(string Query) : IQuery<ToDoStatsQueryResult>;
using BluQube.Queries;

namespace BluQube.Samples.Blazor.Client.Infrastructure.QueryResults;

public record ToDoStatsQueryResult(int TotalCount, int CompletedCount) : IQueryResult;
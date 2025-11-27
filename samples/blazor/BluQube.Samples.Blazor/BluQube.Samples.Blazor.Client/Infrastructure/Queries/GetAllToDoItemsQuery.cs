using BluQube.Attributes;
using BluQube.Queries;
using BluQube.Samples.Blazor.Client.Infrastructure.QueryResults;

namespace BluQube.Samples.Blazor.Client.Infrastructure.Queries;

[BluQubeQuery(Path = "queries/todo/get-all")]
public record GetAllToDoItemsQuery : IQuery<GetAllToDoItemsQueryAnswer>;
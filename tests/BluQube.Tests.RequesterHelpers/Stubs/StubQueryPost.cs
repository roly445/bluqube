using BluQube.Attributes;
using BluQube.Queries;

namespace BluQube.Tests.RequesterHelpers.Stubs;

/// <summary>
/// Query explicitly configured to use POST HTTP method (overriding default).
/// </summary>
[BluQubeQuery(Path = "query-post")]
public record StubQueryPost(string Query) : IQuery<StubPostQueryResult>;
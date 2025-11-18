using BluQube.Attributes;
using BluQube.Constants;
using BluQube.Queries;

namespace BluQube.Tests.RequesterHelpers.Stubs;

/// <summary>
/// Query explicitly configured to use POST HTTP method (overriding default).
/// </summary>
[BluQubeQuery(Path = "query-post", HttpMethod = HttpRequestMethod.Post)]
public record StubQueryPost(string Query) : IQuery<StubPostQueryResult>;
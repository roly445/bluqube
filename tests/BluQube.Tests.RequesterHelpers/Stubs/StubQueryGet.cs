using BluQube.Attributes;
using BluQube.Constants;
using BluQube.Queries;

namespace BluQube.Tests.RequesterHelpers.Stubs;

/// <summary>
/// Query explicitly configured to use GET HTTP method.
/// </summary>
[BluQubeQuery(Path = "query-get", HttpMethod = HttpRequestMethod.Get)]
public record StubQueryGet(string Query) : IQuery<StubGetQueryResult>;
using BluQube.Attributes;
using BluQube.Constants;
using BluQube.Queries;

namespace BluQube.Tests.RequesterHelpers.Stubs;

[BluQubeQuery(Path = "query", HttpMethod = HttpRequestMethod.Post)]
public record StubQuery(string Query) : IQuery<StubQueryResult>;
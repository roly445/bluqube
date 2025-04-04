using BluQube.Attributes;
using BluQube.Queries;

namespace BluQube.Tests.TestHelpers.Stubs;

[BluQubeQuery(Path = "query")]
public record StubQuery(string Query) : IQuery<StubQueryResult>;
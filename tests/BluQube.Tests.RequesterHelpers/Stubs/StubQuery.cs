using BluQube.Attributes;
using BluQube.Queries;

namespace BluQube.Tests.RequesterHelpers.Stubs;

[BluQubeQuery(Path = "query")]
public record StubQuery(string Query) : IQuery<StubQueryResult>;
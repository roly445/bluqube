using BluQube.Queries;

namespace BluQube.Tests.RequesterHelpers.Stubs;

public record StubNoAttrQuery(string Query) : IQuery<StubQueryResult>;
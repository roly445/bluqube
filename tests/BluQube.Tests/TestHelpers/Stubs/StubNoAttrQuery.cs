using BluQube.Queries;

namespace BluQube.Tests.TestHelpers.Stubs;

public record StubNoAttrQuery(string Query) : IQuery<StubQueryResult>;
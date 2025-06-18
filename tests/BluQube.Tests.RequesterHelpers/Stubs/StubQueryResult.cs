using BluQube.Queries;

namespace BluQube.Tests.RequesterHelpers.Stubs;

public record StubQueryResult(string Result) : IQueryResult;
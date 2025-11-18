using BluQube.Queries;

namespace BluQube.Tests.RequesterHelpers.Stubs;

public record StubPostQueryResult(string Message) : IQueryResult;
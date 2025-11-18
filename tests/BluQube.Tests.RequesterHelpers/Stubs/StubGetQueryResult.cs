using BluQube.Queries;

namespace BluQube.Tests.RequesterHelpers.Stubs;

public record StubGetQueryResult(string Message) : IQueryResult;
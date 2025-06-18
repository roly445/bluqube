using BluQube.Commands;

namespace BluQube.Tests.RequesterHelpers.Stubs;

public record StubWithResultCommandResult(string Result) : ICommandResult;
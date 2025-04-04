using BluQube.Commands;

namespace BluQube.Tests.TestHelpers.Stubs;

public record StubWithResultCommandResult(string Result) : ICommandResult;
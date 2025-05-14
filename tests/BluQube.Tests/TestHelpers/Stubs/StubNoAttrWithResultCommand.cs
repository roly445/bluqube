using BluQube.Commands;

namespace BluQube.Tests.TestHelpers.Stubs;

public record StubNoAttrWithResultCommand(string Data) : ICommand<StubWithResultCommandResult>;
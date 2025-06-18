using BluQube.Commands;

namespace BluQube.Tests.RequesterHelpers.Stubs;

public record StubNoAttrWithResultCommand(string Data) : ICommand<StubWithResultCommandResult>;
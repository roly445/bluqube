using BluQube.Commands;

namespace BluQube.Tests.TestHelpers.Stubs;

public record StubNoAttrCommand(string Data) : ICommand;
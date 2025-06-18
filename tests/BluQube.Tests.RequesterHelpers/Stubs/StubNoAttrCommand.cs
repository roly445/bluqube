using BluQube.Commands;

namespace BluQube.Tests.RequesterHelpers.Stubs;

public record StubNoAttrCommand(string Data) : ICommand;
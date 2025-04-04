using BluQube.Attributes;
using BluQube.Commands;

namespace BluQube.Tests.TestHelpers.Stubs;

[BluQubeCommand(Path = "command/stub")]
public record StubCommand(string Data) : ICommand;
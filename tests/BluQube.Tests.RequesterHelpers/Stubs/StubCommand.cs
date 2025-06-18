using BluQube.Attributes;
using BluQube.Commands;

namespace BluQube.Tests.RequesterHelpers.Stubs;

[BluQubeCommand(Path = "command/stub")]
public record StubCommand(string Data) : ICommand;
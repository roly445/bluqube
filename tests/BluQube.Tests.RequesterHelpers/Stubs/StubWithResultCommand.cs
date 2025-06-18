using BluQube.Attributes;
using BluQube.Commands;

namespace BluQube.Tests.RequesterHelpers.Stubs;

[BluQubeCommand(Path = "command/stub-with-result")]
public record StubWithResultCommand(string Data) : ICommand<StubWithResultCommandResult>;
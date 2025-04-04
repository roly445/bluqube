using BluQube.Attributes;
using BluQube.Commands;

namespace BluQube.Tests.TestHelpers.Stubs;

public record StubCommandWithResultResult(string Result) : ICommandResult;

[BluQubeCommand(Path = "command/stub-with-result")]
public record StubCommandWithResult(string Data) : ICommand<StubCommandWithResultResult>;


[BluQubeCommand(Path = "command/stub")]
public record StubCommand(string Data) : ICommand;
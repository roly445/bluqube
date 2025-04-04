using BluQube.Attributes;
using BluQube.Commands;
using BluQube.Samples.Blazor.Client.Infrastructure.CommandResults;

namespace BluQube.Samples.Blazor.Client.Infrastructure.Commands;

[BluQubeCommand(Path = "commands/todo/add")]
public record AddTodoCommand(string Title) : ICommand<AddTodoCommandResult>;
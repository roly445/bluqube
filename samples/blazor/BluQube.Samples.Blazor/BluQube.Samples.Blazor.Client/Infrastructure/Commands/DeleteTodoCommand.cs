using BluQube.Attributes;
using BluQube.Commands;

namespace BluQube.Samples.Blazor.Client.Infrastructure.Commands;

[BluQubeCommand(Path = "commands/todo/delete")]
public record DeleteTodoCommand(Guid TodoId) : ICommand;
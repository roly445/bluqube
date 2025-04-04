using BluQube.Attributes;
using BluQube.Commands;

namespace BluQube.Samples.Blazor.Client.Infrastructure.Commands;

[BluQubeCommand(Path = "commands/todo/mark-as-completed")]
public record MarkTodoAsCompletedCommand(Guid TodoId) : ICommand;
using BluQube.Attributes;
using BluQube.Commands;

namespace BluQube.Samples.Blazor.Client.Infrastructure.Commands;

[BluQubeCommand(Path = "commands/todo/update-title")]
public record UpdateToDoTitleCommand(Guid ToDoId, string Title) : ICommand;
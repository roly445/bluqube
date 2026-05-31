using Mediator;

namespace BluQube.Commands;

/// <summary>
/// Represents a command that performs an action but does not return data.
/// </summary>
/// <remarks>
/// Implement this interface on record types to define commands in the BluQube CQRS pattern.
/// Commands represent write operations (create, update, delete) and return <see cref="CommandResult"/> indicating success, failure, validation errors, or authorization issues.
/// <para>
/// Apply the <see cref="Attributes.BluQubeCommandAttribute"/> to command records to enable source generation of HTTP requesters.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [BluQubeCommand(Path = "commands/create-item")]
/// public record CreateTodoCommand(string Title, string Description) : ICommand;
/// </code>
/// </example>
public interface ICommand : IRequest<CommandResult>;
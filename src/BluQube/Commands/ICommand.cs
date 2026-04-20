using MediatR;

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
/// <para>
/// To enforce authorization, override the <see cref="PolicyName"/> property to return the name of the authorization policy required to execute this command.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [BluQubeCommand(Path = "commands/create-item")]
/// public record CreateTodoCommand(string Title, string Description) : ICommand;
///
/// // With authorization:
/// [BluQubeCommand(Path = "commands/delete-item")]
/// public record DeleteTodoCommand(Guid Id) : ICommand
/// {
///     public string PolicyName => "AdminOnly";
/// }
/// </code>
/// </example>
public interface ICommand : IRequest<CommandResult>
{
    /// <summary>
    /// Gets the authorization policy name required to execute this command. Returns an empty string by default (no authorization required).
    /// </summary>
    /// <value>The name of the authorization policy, or an empty string if no authorization is required.</value>
    string PolicyName => string.Empty;
}
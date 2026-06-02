using System.Diagnostics.CodeAnalysis;

namespace BluQube.Commands;

/// <summary>
/// Represents a command that performs an action and returns typed result data.
/// </summary>
/// <typeparam name="TResult">The type of data returned on successful command execution. Must implement <see cref="ICommandResult"/>.</typeparam>
/// <remarks>
/// Implement this interface on record types to define commands that return data in the BluQube CQRS pattern.
/// Commands return <see cref="CommandResult{TResult}"/> containing either the result data or error information (validation failures, authorization issues, general errors).
/// <para>
/// Apply the <see cref="Attributes.BluQubeCommandAttribute"/> to command records to enable source generation of HTTP requesters.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [BluQubeCommand(Path = "commands/create-item")]
/// public record CreateTodoCommand(string Title) : ICommand&lt;CreateTodoResult&gt;;
///
/// public record CreateTodoResult(Guid Id) : ICommandResult;
/// </code>
/// </example>
[SuppressMessage(
    "Major Code Smell",
    "S2326:Unused type parameters should be removed",
    Justification = "The generic argument binds commands to their result type for mediator dispatch and handler registration.")]
public interface ICommand<TResult>
    where TResult : ICommandResult;
namespace BluQube.Commands;

/// <summary>
/// Marker interface for command result data types.
/// </summary>
/// <remarks>
/// Implement this interface on record types that are returned by <see cref="ICommand{TResult}"/> commands.
/// This interface has no members; it serves as a type constraint to ensure type safety in the command pipeline.
/// </remarks>
/// <example>
/// <code>
/// public record CreateTodoResult(Guid Id) : ICommandResult;
/// </code>
/// </example>
public interface ICommandResult;
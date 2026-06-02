using BluQube.Commands;
using BluQube.Queries;

namespace BluQube.Mediation;

/// <summary>
/// Dispatches BluQube commands and queries through registered handlers and pipeline behaviors.
/// </summary>
public interface IBluQubeMediator
{
    /// <summary>
    /// Sends a command that does not return data.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="request">The command to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The command result.</returns>
    ValueTask<CommandResult> Send<TCommand>(TCommand request, CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    /// <summary>
    /// Sends a command that returns typed data.
    /// </summary>
    /// <typeparam name="TResult">The command result data type.</typeparam>
    /// <param name="request">The command to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The command result.</returns>
    ValueTask<CommandResult<TResult>> Send<TResult>(
        ICommand<TResult> request,
        CancellationToken cancellationToken = default)
        where TResult : ICommandResult;

    /// <summary>
    /// Sends a query that returns typed data.
    /// </summary>
    /// <typeparam name="TResult">The query result data type.</typeparam>
    /// <param name="request">The query to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The query result.</returns>
    ValueTask<QueryResult<TResult>> Send<TResult>(
        IQuery<TResult> request,
        CancellationToken cancellationToken = default)
        where TResult : IQueryResult;
}

using System.Reflection;
using BluQube.Commands;
using BluQube.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace BluQube.Mediation;

/// <summary>
/// Default BluQube mediator implementation backed by dependency injection.
/// </summary>
public sealed class BluQubeMediator(IServiceProvider serviceProvider) : IBluQubeMediator
{
    /// <inheritdoc/>
    public ValueTask<CommandResult> Send<TCommand>(TCommand request, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        return this.Dispatch<TCommand, CommandResult>(
            request,
            handler => ((ICommandHandler<TCommand>)handler).Handle(request, cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask<CommandResult<TResult>> Send<TResult>(
        ICommand<TResult> request,
        CancellationToken cancellationToken = default)
        where TResult : ICommandResult
    {
        return this.SendRuntimeCommand<TResult>(request, cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask<QueryResult<TResult>> Send<TResult>(
        IQuery<TResult> request,
        CancellationToken cancellationToken = default)
        where TResult : IQueryResult
    {
        return this.SendRuntimeQuery<TResult>(request, cancellationToken);
    }

    private ValueTask<CommandResult<TResult>> SendRuntimeCommand<TResult>(
        ICommand<TResult> request,
        CancellationToken cancellationToken)
        where TResult : ICommandResult
    {
        var method = typeof(BluQubeMediator)
            .GetMethod(nameof(this.SendCommandCore), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(request.GetType(), typeof(TResult));

        return (ValueTask<CommandResult<TResult>>)method.Invoke(this, [request, cancellationToken])!;
    }

    private ValueTask<QueryResult<TResult>> SendRuntimeQuery<TResult>(
        IQuery<TResult> request,
        CancellationToken cancellationToken)
        where TResult : IQueryResult
    {
        var method = typeof(BluQubeMediator)
            .GetMethod(nameof(this.SendQueryCore), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(request.GetType(), typeof(TResult));

        return (ValueTask<QueryResult<TResult>>)method.Invoke(this, [request, cancellationToken])!;
    }

    private ValueTask<CommandResult<TResult>> SendCommandCore<TCommand, TResult>(
        TCommand request,
        CancellationToken cancellationToken)
        where TCommand : ICommand<TResult>
        where TResult : ICommandResult
    {
        return this.Dispatch<TCommand, CommandResult<TResult>>(
            request,
            handler => ((ICommandHandler<TCommand, TResult>)handler).Handle(request, cancellationToken),
            cancellationToken);
    }

    private ValueTask<QueryResult<TResult>> SendQueryCore<TQuery, TResult>(
        TQuery request,
        CancellationToken cancellationToken)
        where TQuery : IQuery<TResult>
        where TResult : IQueryResult
    {
        return this.Dispatch<TQuery, QueryResult<TResult>>(
            request,
            handler => ((IQueryProcessor<TQuery, TResult>)handler).Handle(request, cancellationToken),
            cancellationToken);
    }

    private ValueTask<TResponse> Dispatch<TRequest, TResponse>(
        TRequest request,
        Func<object, ValueTask<TResponse>> handlerInvoker,
        CancellationToken cancellationToken)
    {
        var handler = this.GetHandler<TRequest, TResponse>();
        BluQubeRequestHandlerDelegate<TRequest, TResponse> next =
            (_, _) => handlerInvoker(handler);

        var behaviors = serviceProvider
            .GetServices<IBluQubePipelineBehavior<TRequest, TResponse>>()
            .Reverse();

        foreach (var behavior in behaviors)
        {
            var current = next;
            next = (message, token) => behavior.Handle(message, current, token);
        }

        return next(request, cancellationToken);
    }

    private object GetHandler<TRequest, TResponse>()
    {
        var requestType = typeof(TRequest);
        var responseType = typeof(TResponse);
        Type handlerType;

        if (responseType == typeof(CommandResult))
        {
            handlerType = typeof(ICommandHandler<>).MakeGenericType(requestType);
        }
        else if (responseType.IsGenericType &&
                 responseType.GetGenericTypeDefinition() == typeof(CommandResult<>))
        {
            handlerType = typeof(ICommandHandler<,>).MakeGenericType(requestType, responseType.GetGenericArguments()[0]);
        }
        else if (responseType.IsGenericType &&
                 responseType.GetGenericTypeDefinition() == typeof(QueryResult<>))
        {
            handlerType = typeof(IQueryProcessor<,>).MakeGenericType(requestType, responseType.GetGenericArguments()[0]);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported BluQube response type '{responseType}'.");
        }

        return serviceProvider.GetRequiredService(handlerType);
    }
}

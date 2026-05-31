using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BluQube.Authorization;

/// <summary>
/// Mediator pipeline behavior that enforces BluQube authorization before handler execution.
/// Runs when an <see cref="IBluQubeAuthorizer{TRequest}"/> is registered for the message.
/// </summary>
/// <typeparam name="TMessage">The request (command or query) type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// Registration order matters. This behavior must be registered before the handler in the pipeline.
/// Use <see cref="ServiceCollectionExtensions.AddBluQubeAuthorization"/> to register authorizers and
/// then add this behavior to the Mediator pipeline:
/// <code>
/// builder.Services.AddMediator();
/// builder.Services.AddBluQubeAuthorization(typeof(App).Assembly);
/// builder.Services.AddSingleton(typeof(IPipelineBehavior&lt;,&gt;), typeof(BluQubeAuthorizationBehavior&lt;,&gt;));
/// </code>
/// </remarks>
public sealed class BluQubeAuthorizationBehavior<TMessage, TResponse>(
    IServiceProvider rootServiceProvider,
    IHttpContextAccessor? httpContextAccessor = null)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    /// <inheritdoc/>
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        // Prefer request-scoped services when available (HTTP request context)
        var sp = httpContextAccessor?.HttpContext?.RequestServices ?? rootServiceProvider;

        // A registered authorizer is an explicit opt-in to request authorization.
        var authorizer = sp.GetService<IBluQubeAuthorizer<TMessage>>();
        if (authorizer == null)
        {
            return await next(message, cancellationToken);
        }

        var result = await authorizer.Authorize(message, cancellationToken);
        if (!result.IsAuthorized)
        {
            throw new UnauthorizedException(result.FailureMessage ?? "Authorization failed.");
        }

        return await next(message, cancellationToken);
    }
}
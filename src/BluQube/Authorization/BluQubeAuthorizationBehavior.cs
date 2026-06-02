using BluQube.Mediation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BluQube.Authorization;

/// <summary>
/// BluQube pipeline behavior that enforces authorization before handler execution.
/// Runs when an <see cref="IBluQubeAuthorizer{TRequest}"/> is registered for the message.
/// </summary>
/// <typeparam name="TMessage">The request (command or query) type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// Registration order matters. This behavior must be registered before the handler in the pipeline.
/// Use the BluQube authorization service collection extensions to register authorizers and add this behavior
/// to the BluQube pipeline:
/// <code>
/// builder.Services.AddBluQube(typeof(App).Assembly);
/// builder.Services.AddBluQubeAuthorization(typeof(App).Assembly);
/// builder.Services.AddSingleton(typeof(IBluQubePipelineBehavior&lt;,&gt;), typeof(BluQubeAuthorizationBehavior&lt;,&gt;));
/// </code>
/// </remarks>
public sealed class BluQubeAuthorizationBehavior<TMessage, TResponse>(
    IServiceProvider rootServiceProvider,
    IHttpContextAccessor? httpContextAccessor = null,
    IOptions<BluQubeAuthorizationOptions>? options = null)
    : IBluQubePipelineBehavior<TMessage, TResponse>
{
    /// <inheritdoc/>
    public async ValueTask<TResponse> Handle(
        TMessage request,
        BluQubeRequestHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        // Prefer request-scoped services when available (HTTP request context)
        var sp = httpContextAccessor?.HttpContext?.RequestServices ?? rootServiceProvider;

        // A registered authorizer is an explicit opt-in to request authorization.
        var authorizer = sp.GetService<IBluQubeAuthorizer<TMessage>>();
        if (authorizer == null)
        {
            if (request is IAllowAnonymousBluQubeRequest)
            {
                return await next(request, cancellationToken);
            }

            if (options?.Value.RequireAuthorizationByDefault == true)
            {
                throw new UnauthorizedException(
                    $"No authorizer is registered for request type '{typeof(TMessage).Name}'.");
            }

            return await next(request, cancellationToken);
        }

        var result = await authorizer.Authorize(request, cancellationToken);
        if (!result.IsAuthorized)
        {
            throw new UnauthorizedException(result.FailureMessage ?? "Authorization failed.");
        }

        return await next(request, cancellationToken);
    }
}
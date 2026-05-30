using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BluQube.Authorization;

/// <summary>
/// Mediator pipeline behavior that enforces BluQube authorization before handler execution.
/// Runs when the resolved handler type is decorated with <see cref="AuthorizeAttribute"/>.
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

        // Resolve the concrete handler to check for [Authorize] attribute.
        // Use runtime type to bypass the IRequest<TResponse> constraint on IRequestHandler<,>.
        var handlerServiceType = typeof(IRequestHandler<,>).MakeGenericType(typeof(TMessage), typeof(TResponse));
        var handlerType = sp.GetService(handlerServiceType)?.GetType();

        if (handlerType == null)
        {
            return await next(message, cancellationToken);
        }

        var authorizeAttr = (AuthorizeAttribute?)Attribute.GetCustomAttribute(
            handlerType, typeof(AuthorizeAttribute), inherit: true);

        if (authorizeAttr == null)
        {
            return await next(message, cancellationToken);
        }

        // Try custom authorizer first
        var authorizer = sp.GetService<IBluQubeAuthorizer<TMessage>>();
        if (authorizer != null)
        {
            var result = await authorizer.Authorize(message, cancellationToken);
            if (!result.IsAuthorized)
            {
                throw new UnauthorizedException(result.FailureMessage ?? "Authorization failed.");
            }

            return await next(message, cancellationToken);
        }

        // Fall back to ASP.NET Core policy / IsAuthenticated
        if (!string.IsNullOrEmpty(authorizeAttr.PolicyName))
        {
            var authorizationService = sp.GetService<IAuthorizationService>();
            var httpContext = httpContextAccessor?.HttpContext;
            if (authorizationService != null && httpContext != null)
            {
                var policyResult = await authorizationService.AuthorizeAsync(
                    httpContext.User, resource: null, authorizeAttr.PolicyName);

                if (!policyResult.Succeeded)
                {
                    throw new UnauthorizedException($"Policy '{authorizeAttr.PolicyName}' was not satisfied.");
                }

                return await next(message, cancellationToken);
            }
        }

        // Plain [Authorize] with no policy and no custom authorizer — check IsAuthenticated
        var user = httpContextAccessor?.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedException("User is not authenticated.");
        }

        return await next(message, cancellationToken);
    }
}
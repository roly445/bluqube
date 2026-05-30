namespace BluQube.Authorization;

/// <summary>
/// Defines a custom authorizer for a specific request type.
/// Implement this interface to provide request-level authorization logic for a command or query.
/// </summary>
/// <typeparam name="TRequest">The command or query type this authorizer handles.</typeparam>
/// <remarks>
/// Register implementations in DI using <c>AddBluQubeAuthorization(Assembly)</c> or manually with
/// <c>builder.Services.AddScoped&lt;IBluQubeAuthorizer&lt;TRequest&gt;, TAuthorizer&gt;()</c>.
/// The <see cref="BluQubeAuthorizationBehavior{TMessage,TResponse}"/> resolves and invokes this
/// authorizer when the handler is decorated with <see cref="AuthorizeAttribute"/>.
/// </remarks>
/// <example>
/// <code>
/// public class AddTodoCommandAuthorizer : IBluQubeAuthorizer&lt;AddTodoCommand&gt;
/// {
///     private readonly IHttpContextAccessor _httpContextAccessor;
///
///     public AddTodoCommandAuthorizer(IHttpContextAccessor httpContextAccessor)
///         => _httpContextAccessor = httpContextAccessor;
///
///     public Task&lt;AuthorizationResult&gt; Authorize(AddTodoCommand request, CancellationToken cancellationToken)
///     {
///         var isAuthenticated = _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
///         return Task.FromResult(isAuthenticated ? AuthorizationResult.Succeed() : AuthorizationResult.Fail());
///     }
/// }
/// </code>
/// </example>
public interface IBluQubeAuthorizer<in TRequest>
{
    /// <summary>
    /// Evaluates whether the given request is authorized.
    /// </summary>
    /// <param name="request">The command or query being authorized.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="AuthorizationResult"/> indicating whether the request is allowed.
    /// Return <see cref="AuthorizationResult.Succeed()"/> to allow execution or
    /// <see cref="AuthorizationResult.Fail(string?)"/> to deny it.
    /// </returns>
    Task<AuthorizationResult> Authorize(TRequest request, CancellationToken cancellationToken);
}
namespace BluQube.Authorization;

/// <summary>
/// Marks a command or query handler as requiring authorization before execution.
/// Place this attribute on the handler class (not the command/query record).
/// </summary>
/// <remarks>
/// When applied to a handler, the <see cref="BluQubeAuthorizationBehavior{TMessage,TResponse}"/> pipeline behavior
/// runs before the handler executes. If no <see cref="IBluQubeAuthorizer{TRequest}"/> is registered for the message
/// type, the behavior falls back to checking <c>IHttpContextAccessor.HttpContext.User.Identity.IsAuthenticated</c>.
/// <para>
/// To use a named policy, specify the <see cref="PolicyName"/> parameter. The named policy must be configured
/// using <c>builder.Services.AddAuthorization()</c> in your <c>Program.cs</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Require any authenticated user:
/// [Authorize]
/// public class AddTodoCommandHandler : CommandHandler&lt;AddTodoCommand&gt; { ... }
///
/// // Require a specific ASP.NET Core policy:
/// [Authorize("AdminOnly")]
/// public class DeleteTodoCommandHandler : CommandHandler&lt;DeleteTodoCommand&gt; { ... }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class AuthorizeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizeAttribute"/> class that requires any authenticated user.
    /// </summary>
    public AuthorizeAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizeAttribute"/> class that requires the specified ASP.NET Core policy.
    /// </summary>
    /// <param name="policyName">The name of the ASP.NET Core authorization policy to evaluate.</param>
    public AuthorizeAttribute(string policyName)
    {
        this.PolicyName = policyName;
    }

    /// <summary>
    /// Gets the optional ASP.NET Core authorization policy name. When null or empty, any authenticated user is allowed.
    /// </summary>
    public string? PolicyName { get; }
}
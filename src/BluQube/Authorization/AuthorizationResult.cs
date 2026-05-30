namespace BluQube.Authorization;

/// <summary>
/// Represents the result of an authorization check performed by an <see cref="IBluQubeAuthorizer{TRequest}"/>.
/// </summary>
public sealed class AuthorizationResult
{
    private AuthorizationResult(bool isAuthorized, string? failureMessage)
    {
        this.IsAuthorized = isAuthorized;
        this.FailureMessage = failureMessage;
    }

    /// <summary>Gets a value indicating whether the authorization check passed.</summary>
    public bool IsAuthorized { get; }

    /// <summary>Gets the failure message when authorization is denied. <see langword="null"/> when authorized.</summary>
    public string? FailureMessage { get; }

    /// <summary>Returns a successful authorization result.</summary>
    public static AuthorizationResult Succeed() => new(true, null);

    /// <summary>Returns a failed authorization result with an optional message.</summary>
    public static AuthorizationResult Fail(string? message = null) => new(false, message);
}

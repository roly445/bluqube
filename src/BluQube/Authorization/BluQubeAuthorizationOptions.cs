namespace BluQube.Authorization;

/// <summary>
/// Options that control BluQube authorization pipeline behavior.
/// </summary>
public sealed class BluQubeAuthorizationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether commands and queries without a registered authorizer should be rejected.
    /// </summary>
    public bool RequireAuthorizationByDefault { get; set; }
}
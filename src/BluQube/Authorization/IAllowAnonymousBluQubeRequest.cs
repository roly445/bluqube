namespace BluQube.Authorization;

/// <summary>
/// Marks a command or query as intentionally public when authorization is required by default.
/// </summary>
/// <remarks>
/// This marker only bypasses the default missing-authorizer rejection. If an
/// <see cref="IBluQubeAuthorizer{TRequest}"/> is registered for the request type, the authorizer still runs.
/// </remarks>
public interface IAllowAnonymousBluQubeRequest;
namespace BluQube.Constants;

/// <summary>
/// Standard error codes used in <see cref="Commands.BluQubeErrorData"/>.
/// </summary>
/// <remarks>
/// These constants provide machine-readable error identifiers for common failure scenarios in the BluQube framework.
/// Applications can define their own error codes as needed; these are provided for framework-level errors.
/// </remarks>
public static class BluQubeErrorCodes
{
    /// <summary>
    /// Error code indicating the operation was rejected due to authorization failure.
    /// </summary>
    /// <value>"NotAuthorized"</value>
    public const string NotAuthorized = "NotAuthorized";

    /// <summary>
    /// Error code indicating a communication failure between client and server (HTTP error, network issue, or JSON deserialization failure).
    /// </summary>
    /// <value>"CommunicationError"</value>
    public const string CommunicationError = "CommunicationError";
}
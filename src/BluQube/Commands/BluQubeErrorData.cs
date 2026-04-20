using System.Text.Json.Serialization;

namespace BluQube.Commands;

/// <summary>
/// Represents a command error with an error code and optional descriptive message.
/// </summary>
/// <remarks>
/// This class is used to transport error information when a command fails. It's serialized as JSON when command results cross HTTP boundaries.
/// The <see cref="Code"/> property provides a machine-readable error identifier; <see cref="Message"/> provides a human-readable description.
/// <para>
/// Standard error codes are defined in <see cref="Constants.BluQubeErrorCodes"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // With message:
/// var error = new BluQubeErrorData("DUPLICATE_ENTITY", "A todo with this title already exists");
/// 
/// // Code-only (message defaults to code):
/// var error = new BluQubeErrorData(BluQubeErrorCodes.NotAuthorized);
/// </code>
/// </example>
[method: JsonConstructor]
public sealed class BluQubeErrorData(string code, string message)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BluQubeErrorData"/> class with an error code. The message defaults to the code value.
    /// </summary>
    /// <param name="code">The error code.</param>
    public BluQubeErrorData(string code)
        : this(code, code)
    {
    }

    /// <summary>
    /// Gets the machine-readable error code.
    /// </summary>
    /// <value>An error code string such as "NotAuthorized" or "CommunicationError". See <see cref="Constants.BluQubeErrorCodes"/> for standard codes.</value>
    public string Code { get; } = code;

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    /// <value>A descriptive error message.</value>
    public string Message { get; } = message;
}
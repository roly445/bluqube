namespace BluQube.Authorization;

/// <summary>
/// Exception thrown by the BluQube authorization pipeline when a request is not authorized.
/// This exception is caught by <see cref="BluQube.Commands.CommandRunner"/> and
/// <see cref="BluQube.Queries.QueryRunner"/> and converted to an unauthorized result,
/// so it should not propagate to application code.
/// </summary>
public sealed class UnauthorizedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedException"/> class.
    /// </summary>
    public UnauthorizedException()
        : base("The request was not authorized.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with a message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public UnauthorizedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public UnauthorizedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
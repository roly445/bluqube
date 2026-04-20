namespace BluQube.Commands;

/// <summary>
/// Represents a single validation failure for a command property.
/// </summary>
/// <remarks>
/// Instances of this record are created by the validation pipeline when FluentValidation detects rule violations.
/// Each failure captures the error message, the property that failed validation, and the value that was attempted.
/// </remarks>
/// <param name="ErrorMessage">A human-readable description of the validation failure.</param>
/// <param name="PropertyName">The name of the property that failed validation, or null if the failure applies to the entire command.</param>
/// <param name="AttemptedValue">The value that failed validation, or null if no value was provided.</param>
/// <example>
/// <code>
/// var failure = new CommandValidationFailure(
///     "Title must not be empty",
///     "Title",
///     null);
/// </code>
/// </example>
public record CommandValidationFailure(string ErrorMessage, string? PropertyName = null, object? AttemptedValue = null);
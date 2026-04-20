namespace BluQube.Constants;

/// <summary>
/// Defines the possible outcomes of a command execution.
/// </summary>
/// <remarks>
/// These values are serialized as integers in JSON. Explicit integer assignments ensure stable serialization across versions.
/// Used by <see cref="Commands.CommandResult"/> and <see cref="Commands.CommandResult{T}"/> to indicate the outcome of command execution.
/// </remarks>
public enum CommandResultStatus
{
    /// <summary>
    /// Status is unknown or unrecognized. Should not occur in normal operation; indicates a deserialization issue.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Command failed FluentValidation. Access <see cref="Commands.CommandResult.ValidationResult"/> for details.
    /// </summary>
    Invalid = 1,

    /// <summary>
    /// Command execution failed with an error. Access <see cref="Commands.CommandResult.ErrorData"/> for details.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Command executed successfully. For <see cref="Commands.CommandResult{T}"/>, access <see cref="Commands.CommandResult{T}.Data"/> for result data.
    /// </summary>
    Succeeded = 3,
}
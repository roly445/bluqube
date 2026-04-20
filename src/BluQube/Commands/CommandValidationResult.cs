namespace BluQube.Commands;

/// <summary>
/// Contains the validation failures for a command that failed FluentValidation rules.
/// </summary>
/// <remarks>
/// This record is returned as part of <see cref="CommandResult.Invalid(CommandValidationResult)"/> when FluentValidation detects rule violations.
/// The <see cref="Failures"/> property contains a list of all validation errors, each describing which property failed and why.
/// <para>
/// Validation occurs before the command handler executes. If validation fails, the handler is never invoked.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await commandRunner.Send(new CreateTodoCommand("", "Description"));
/// 
/// if (result.Status == CommandResultStatus.Invalid)
/// {
///     foreach (var failure in result.ValidationResult.Failures)
///     {
///         Console.WriteLine($"{failure.PropertyName}: {failure.ErrorMessage}");
///     }
/// }
/// </code>
/// </example>
public record CommandValidationResult
{
    /// <summary>
    /// Gets a value indicating whether validation succeeded (no failures).
    /// </summary>
    /// <value><c>true</c> if there are no validation failures; otherwise, <c>false</c>.</value>
    public bool IsValid => this.Failures.Count == 0;

    /// <summary>
    /// Gets or initializes the collection of validation failures.
    /// </summary>
    /// <value>A read-only list of <see cref="CommandValidationFailure"/> instances. Empty if validation succeeded.</value>
    public IReadOnlyList<CommandValidationFailure> Failures { get; init; } = [];
}
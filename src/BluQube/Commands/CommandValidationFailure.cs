namespace BluQube.Commands;

public record CommandValidationFailure(string ErrorMessage, string? PropertyName = null, object? AttemptedValue = null);
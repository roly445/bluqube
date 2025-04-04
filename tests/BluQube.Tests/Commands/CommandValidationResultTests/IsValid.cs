using BluQube.Commands;

namespace BluQube.Tests.Commands.CommandValidationResultTests;

public class IsValid
{
    [Fact]
    public async Task FalseWhenFailuresExist()
    {
        // Arrange
        var result = new CommandValidationResult
        {
            Failures = new List<CommandValidationFailure>
            {
                new("Property", "Error"),
            },
        };

        // Act
        var isValid = result.IsValid;

        // Assert
        await Verify(isValid);
    }

    [Fact]
    public async Task TrueWhenNoFailuresExist()
    {
        // Arrange
        var result = new CommandValidationResult();

        // Act
        var isValid = result.IsValid;

        // Assert
        await Verify(isValid);
    }
}
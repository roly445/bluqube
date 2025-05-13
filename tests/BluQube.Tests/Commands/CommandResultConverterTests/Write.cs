using System.Text.Json;
using BluQube.Commands;

namespace BluQube.Tests.Commands.CommandResultConverterTests;

public class Write
{
    private readonly JsonSerializerOptions _options;

    public Write()
    {
        this._options = new JsonSerializerOptions();
        this._options.Converters.Add(new CommandResultConverter());
    }

    [Fact]
    public async Task GeneratesValidJsonWhenSucceeded()
    {
        // Arrange
        var commandResult = CommandResult.Succeeded();

        // Act
        var result = JsonSerializer.Serialize(commandResult, this._options);

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task GeneratesValidJsonWhenFailed()
    {
        // Arrange
        var commandResult = CommandResult.Failed(new BlueQubeErrorData("err-code", "err-message"));

        // Act
        var result = JsonSerializer.Serialize(commandResult, this._options);

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task GeneratesValidJsonWhenInvalid()
    {
        // Arrange
        var commandResult = CommandResult.Invalid(new CommandValidationResult
        {
            Failures =
            [
                new CommandValidationFailure("Property", "Error")
            ],
        });

        // Act
        var result = JsonSerializer.Serialize(commandResult, this._options);

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task GeneratesValidJsonWhenUnauthorized()
    {
        // Arrange
        var commandResult = CommandResult.Unauthorized();

        // Act
        var result = JsonSerializer.Serialize(commandResult, this._options);

        // Assert
        await Verify(result);
    }
}
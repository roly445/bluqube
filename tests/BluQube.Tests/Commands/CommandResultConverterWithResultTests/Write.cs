using System.Text.Json;
using BluQube.Commands;
using BluQube.Tests.TestHelpers.Stubs;

namespace BluQube.Tests.Commands.CommandResultConverterWithResultTests;

public class Write
{
    private readonly JsonSerializerOptions _options;

    public Write()
    {
        this._options = new JsonSerializerOptions();
        this._options.Converters.Add(new CommandResultConverter<StubCommandWithResultResult>());
    }

    [Fact]
    public async Task GeneratesValidJsonWhenSucceeded()
    {
        // Arrange
        var commandResult = CommandResult<StubCommandWithResultResult>.Succeeded(new StubCommandWithResultResult("result"));

        // Act
        var result = JsonSerializer.Serialize(commandResult, this._options);

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task GeneratesValidJsonWhenFailed()
    {
        // Arrange
        var commandResult = CommandResult<StubCommandWithResultResult>.Failed(new ErrorData("some-error"));

        // Act
        var result = JsonSerializer.Serialize(commandResult, this._options);

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task GeneratesValidJsonWhenInvalid()
    {
        // Arrange
        var commandResult = CommandResult<StubCommandWithResultResult>.Invalid(new CommandValidationResult
        {
            Failures = new List<CommandValidationFailure>
            {
                new("some-property", "some-error"),
            },
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
        var commandResult = CommandResult<StubCommandWithResultResult>.Unauthorized();

        // Act
        var result = JsonSerializer.Serialize(commandResult, this._options);

        // Assert
        await Verify(result);
    }
}
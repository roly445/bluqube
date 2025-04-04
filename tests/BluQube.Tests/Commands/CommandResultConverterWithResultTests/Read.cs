using System.Text.Json;
using BluQube.Commands;
using BluQube.Tests.TestHelpers.Stubs;

namespace BluQube.Tests.Commands.CommandResultConverterWithResultTests;

public class Read
{
    [Theory]
    [InlineData("{\"Status\":2,\"ErrorData\":{\"Code\":\"some-error\",\"Message\":\"some-error\"}}", "Failed")]
    [InlineData("{\"Status\":1,\"ValidationResult\":{\"IsValid\":false,\"Failures\":[{\"ErrorMessage\":\"Property\",\"PropertyName\":\"Error\",\"AttemptedValue\":null}]}}", "Invalid")]
    [InlineData("{\"Status\":3,\"Data\":{\"Result\":\"result\"}}", "Succeeded")]
    [InlineData("{\"Status\":2,\"ErrorData\":{\"Code\":\"NotAuthorized\",\"Message\":\"NotAuthorized\"}}", "NotAuthorized")]
    public async Task GeneratesAValidCommandResultWhenJsonIsValid(string json, string name)
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new CommandResultConverter<StubCommandWithResultResult>());

        // Act
        var result = JsonSerializer.Deserialize<CommandResult<StubCommandWithResultResult>>(json, options);

        // Assert
        await Verify(result).UseParameters(name);
    }
}
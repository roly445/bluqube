using BluQube.Tests.ResponderHelpers;
using Microsoft.AspNetCore.Http.Json;

namespace BluQube.Tests.Extensions.JsonOptionsExtensions;

public class AddBluQubeJsonConverters
{
    [Fact]
    public async Task AddsTheRequiredJsonConvertersForTheApi()
    {
        // Arrange
        var jsonOptions = new JsonOptions();

        // Act
        jsonOptions.AddBluQubeJsonConverters();

        // Assert
        await Verify(jsonOptions.SerializerOptions.Converters);
    }
}
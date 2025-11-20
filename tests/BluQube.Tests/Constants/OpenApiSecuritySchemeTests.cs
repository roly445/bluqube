using BluQube.Constants;

namespace BluQube.Tests.Constants;

public class OpenApiSecuritySchemeTests
{
    [Fact]
    public void BearerHasCorrectValue()
    {
        // Arrange & Act
        var result = OpenApiSecurityScheme.Bearer;

        // Assert
        Assert.Equal(0, (int)result);
    }

    [Fact]
    public void CookieHasCorrectValue()
    {
        // Arrange & Act
        var result = OpenApiSecurityScheme.Cookie;

        // Assert
        Assert.Equal(1, (int)result);
    }

    [Fact]
    public void ApiKeyHasCorrectValue()
    {
        // Arrange & Act
        var result = OpenApiSecurityScheme.ApiKey;

        // Assert
        Assert.Equal(2, (int)result);
    }

    [Theory]
    [InlineData(OpenApiSecurityScheme.Bearer, "Bearer")]
    [InlineData(OpenApiSecurityScheme.Cookie, "Cookie")]
    [InlineData(OpenApiSecurityScheme.ApiKey, "ApiKey")]
    public void EnumToStringReturnsCorrectName(OpenApiSecurityScheme scheme, string expectedName)
    {
        // Act
        var result = scheme.ToString();

        // Assert
        Assert.Equal(expectedName, result);
    }
}


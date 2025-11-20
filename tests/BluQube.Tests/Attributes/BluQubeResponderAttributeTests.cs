using BluQube.Attributes;
using BluQube.Constants;

namespace BluQube.Tests.Attributes;

public class BluQubeResponderAttributeTests
{
    [Fact]
    public void DefaultSecuritySchemeIsBearer()
    {
        // Arrange & Act
        var attribute = new BluQubeResponderAttribute();

        // Assert
        Assert.Equal(OpenApiSecurityScheme.Bearer, attribute.OpenApiSecurityScheme);
    }

    [Fact]
    public void CanSetSecuritySchemeToCookie()
    {
        // Arrange & Act
        var attribute = new BluQubeResponderAttribute { OpenApiSecurityScheme = OpenApiSecurityScheme.Cookie };

        // Assert
        Assert.Equal(OpenApiSecurityScheme.Cookie, attribute.OpenApiSecurityScheme);
    }

    [Fact]
    public void CanSetSecuritySchemeToApiKey()
    {
        // Arrange & Act
        var attribute = new BluQubeResponderAttribute { OpenApiSecurityScheme = OpenApiSecurityScheme.ApiKey };

        // Assert
        Assert.Equal(OpenApiSecurityScheme.ApiKey, attribute.OpenApiSecurityScheme);
    }

    [Fact]
    public void CanUseInitSyntax()
    {
        // Arrange & Act
        var attribute = new BluQubeResponderAttribute
        {
            OpenApiSecurityScheme = OpenApiSecurityScheme.Cookie,
        };

        // Assert
        Assert.Equal(OpenApiSecurityScheme.Cookie, attribute.OpenApiSecurityScheme);
    }
}



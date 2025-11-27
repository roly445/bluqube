using BluQube.Constants;

namespace BluQube.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class BluQubeResponderAttribute : Attribute
{
    /// <summary>
    /// Gets the OpenAPI security scheme type.
    /// If not specified, defaults to Bearer when authorization is detected.
    /// </summary>
    public OpenApiSecurityScheme OpenApiSecurityScheme { get; init; } = OpenApiSecurityScheme.Bearer;
}
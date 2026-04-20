namespace BluQube.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class BluQubeQueryAttribute : Attribute
{
    public string Path { get; init; } = string.Empty;

    public string Method { get; init; } = "POST";
}
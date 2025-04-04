namespace BluQube.Attributes;

public class BluQubeCommandAttribute : Attribute
{
    public string Path { get; init; } = string.Empty;
}
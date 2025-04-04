namespace BluQube.Attributes;

public class BluQubeQueryAttribute : Attribute
{
    public string Path { get; init; } = string.Empty;
}
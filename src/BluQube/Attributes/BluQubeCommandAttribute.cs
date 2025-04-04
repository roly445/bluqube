namespace BluQube.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class BluQubeCommandAttribute : Attribute
{
    public string Path { get; init; } = string.Empty;
}
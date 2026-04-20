# Roslyn Skill: Reading Record Parameters in Incremental Generators

**Skill Type:** Roslyn / IIncrementalGenerator  
**Created:** 2026-04-11  
**Author:** Kaylee (Framework Dev)  
**Use Case:** Extract parameter metadata (name, type, attributes) from positional C# records in source generators

---

## The Pattern

When building incremental source generators that need to inspect **positional record parameters**, Roslyn provides `RecordDeclarationSyntax.ParameterList` to access constructor parameters.

### Code Example

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// In your InputDefinitionProcessor:
protected override InputDefinition? ProcessInternal(SyntaxNode syntaxNode)
{
    if (syntaxNode is not RecordDeclarationSyntax recordDeclarationSyntax)
        return null;

    // Extract parameters from positional record
    var parameters = new List<ParameterInfo>();
    
    if (recordDeclarationSyntax.ParameterList != null)
    {
        foreach (var parameter in recordDeclarationSyntax.ParameterList.Parameters)
        {
            var paramName = parameter.Identifier.Text;
            var paramType = parameter.Type?.ToString() ?? "unknown";
            
            // Read per-parameter attributes
            var attributes = parameter.AttributeLists
                .SelectMany(al => al.Attributes)
                .Select(attr => attr.Name.ToString())
                .ToList();
            
            parameters.Add(new ParameterInfo
            {
                Name = paramName,
                Type = paramType,
                Attributes = attributes
            });
        }
    }
    
    return new InputDefinition(recordDeclarationSyntax, parameters);
}

public class ParameterInfo
{
    public string Name { get; init; }
    public string Type { get; init; }
    public List<string> Attributes { get; init; } = new();
}
```

---

## Key APIs

| API | Purpose |
|-----|---------|
| `RecordDeclarationSyntax.ParameterList` | Access to record's primary constructor parameters (null if none) |
| `ParameterSyntax.Identifier.Text` | Parameter name (e.g., `"Id"`) |
| `ParameterSyntax.Type` | Parameter type as `TypeSyntax` (e.g., `"Guid"`, `"List<string>"`) |
| `ParameterSyntax.AttributeLists` | Collection of attribute lists on the parameter |

---

## Reading Specific Attributes

To check for a specific attribute (e.g., `[FromRoute]`):

```csharp
var fromRouteAttr = parameter.AttributeLists
    .SelectMany(al => al.Attributes)
    .FirstOrDefault(attr => attr.Name.ToString() == "FromRoute");

if (fromRouteAttr != null)
{
    // This parameter has [FromRoute] attribute
    // Read attribute arguments if needed:
    var argValue = fromRouteAttr.ArgumentList?.Arguments
        .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.Text == "Name")?
        .Expression.ToString();
}
```

---

## Important Notes

1. **Positional vs Property-Based Records:**
   - Positional: `public record Foo(string Bar);` → uses `ParameterList`
   - Property: `public record Foo { public string Bar { get; init; } }` → use `TypeDeclarationSyntax.Members` instead

2. **Incremental Caching:**
   - If you include parameter metadata in your `InputDefinition`, ensure the data class implements **value equality**
   - Use `record class` for automatic value equality, or override `Equals`/`GetHashCode`

3. **Semantic Model vs Syntax:**
   - `ParameterSyntax.Type` gives you the **syntax** representation (e.g., `"Guid"`)
   - For full type information (namespace, generic args), use `SemanticModel.GetTypeInfo(parameter.Type)`

---

## Example: BluQube URL Binding Use Case

In BluQube, we need to detect which record parameters match route params in the path template:

```csharp
// Command definition:
[BluQubeCommand(Path = "commands/todo/{id}")]
public record DeleteTodoCommand(Guid Id) : ICommand;

// Generator extracts:
// - Path template: "commands/todo/{id}"
// - Parameters: [{ Name: "Id", Type: "Guid" }]
// - Match: "id" (from path) → "Id" (from record, case-insensitive)
```

Generator code:

```csharp
var path = attributeSyntax.GetPath(); // "commands/todo/{id}"
var routeParams = PathTemplateParser.ExtractRouteParameters(path); // ["id"]

var recordParams = recordDeclarationSyntax.ParameterList?.Parameters
    .Select(p => new { Name = p.Identifier.Text, Type = p.Type?.ToString() })
    .ToList() ?? new();

foreach (var routeParam in routeParams)
{
    var matchedParam = recordParams.FirstOrDefault(rp => 
        rp.Name.Equals(routeParam, StringComparison.OrdinalIgnoreCase));
    
    if (matchedParam == null)
    {
        // Emit diagnostic: route param {id} has no matching record property
    }
}
```

---

## When to Use This Pattern

✅ **Use when:**
- Building generators that need to inspect record constructors
- Detecting attributes on record parameters (e.g., `[FromRoute]`, `[JsonProperty]`)
- Matching record properties to external metadata (e.g., route templates, database columns)

❌ **Don't use when:**
- Working with class constructors → use `ConstructorDeclarationSyntax` instead
- Need runtime type information → use reflection (but avoid in WASM scenarios)

---

## Related Files in BluQube

- `src/BluQube.SourceGeneration/DefinitionProcessors/InputDefinitionProcessors/CommandInputDefinitionProcessor.cs` — Example of processing record syntax
- `src/BluQube.SourceGeneration/Extensions/AttributeSyntaxExtensions.cs` — Pattern for reading attribute arguments
- `src/BluQube.SourceGeneration/Requesting.cs` — Full incremental generator pipeline

---

## References

- [Roslyn API Docs: RecordDeclarationSyntax](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntax.recorddeclarationsyntax)
- [Incremental Generators Guide](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md)
- BluQube Custom Instruction: Section on "Generator Architecture"

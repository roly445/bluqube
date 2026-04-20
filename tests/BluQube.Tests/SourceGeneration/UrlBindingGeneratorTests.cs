// Run with --verify to accept snapshots after Kaylee's implementation
// NOTE: These tests will fail until the URL binding feature is implemented.
// They're scaffolding ready to be filled in with snapshot verifications.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace BluQube.Tests.SourceGeneration;

public class UrlBindingGeneratorTests
{
    [Fact(Skip = "Scaffold test — enable after Kaylee's URL binding implementation")]
    public void CommandWithPathParameter_GeneratesBuildPathOverride()
    {
        // Arrange
        var code = @"
using System;
using BluQube.Attributes;
using BluQube.Commands;

[BluQubeCommand(Path = ""commands/todo/{id}"")] 
public record DeleteTodoCommand(Guid Id) : ICommand { }

[BluQubeRequester]
internal class EntryPoint { }
";

        // Act
        GeneratorDriver driver = RunRequestingGenerator(code);
        var result = driver.GetRunResult();
        var generated = result.Results
            .SelectMany(r => r.GeneratedSources)
            .Where(s => s.HintName.Contains("GenericCommandHandler"))
            .ToList();

        // Assert
        // TODO: After implementation, verify generated code contains:
        // - BuildPath override method
        // - String interpolation using Uri.EscapeDataString for {id}
        // Example: protected override string BuildPath(DeleteTodoCommand request) =>
        //   $"commands/todo/{Uri.EscapeDataString(request.Id.ToString())}";
        Assert.True(generated.Count > 0, "Expected generated source for command handler");
    }

    [Fact(Skip = "Scaffold test — enable after Kaylee's URL binding implementation")]
    public void CommandWithoutPathParameter_NoGeneratedBuildPath()
    {
        // Arrange
        var code = @"
using BluQube.Attributes;
using BluQube.Commands;

[BluQubeCommand(Path = ""commands/todo/add"")] 
public record AddTodoCommand(string Title) : ICommand { }

[BluQubeRequester]
internal class EntryPoint { }
";

        // Act
        GeneratorDriver driver = RunRequestingGenerator(code);
        var result = driver.GetRunResult();
        var generated = result.Results
            .SelectMany(r => r.GeneratedSources)
            .Where(s => s.HintName.Contains("GenericCommandHandler"))
            .ToList();

        // Assert
        // TODO: After implementation, verify generated code does NOT contain BuildPath override
        // Should be identical to current generator output (no route params = no override needed)
        Assert.True(generated.Count > 0, "Expected generated source for command handler");
    }

    [Fact(Skip = "Scaffold test — enable after Kaylee's URL binding implementation")]
    public void GetQueryWithPathParameter_GeneratesBuildPathWithQuerystring()
    {
        // Arrange
        var code = @"
using System;
using BluQube.Attributes;
using BluQube.Queries;

public record TodoResult : IQueryResult { }

[BluQubeQuery(Path = ""queries/todo/{id}"", Method = ""GET"")] 
public record GetTodoQuery(Guid Id, string? Filter) : IQuery<TodoResult> { }

[BluQubeRequester]
internal class EntryPoint { }
";

        // Act
        GeneratorDriver driver = RunRequestingGenerator(code);
        var result = driver.GetRunResult();
        var generated = result.Results
            .SelectMany(r => r.GeneratedSources)
            .Where(s => s.HintName.Contains("GenericQueryProcessor"))
            .ToList();

        // Assert
        // TODO: After implementation, verify generated code contains:
        // - BuildPath override that constructs path + querystring
        // - Path interpolation: $"queries/todo/{Uri.EscapeDataString(request.Id.ToString())}"
        // - Querystring logic: ?Filter={request.Filter} (if not null)
        Assert.True(generated.Count > 0, "Expected generated source for query processor");
    }

    [Fact(Skip = "Scaffold test — enable after Kaylee's URL binding implementation")]
    public void GetQueryWithoutPathParameter_UsesQuerystringOnly()
    {
        // Arrange
        var code = @"
using BluQube.Attributes;
using BluQube.Queries;

public record ListResult : IQueryResult { }

[BluQubeQuery(Path = ""queries/todo/list"", Method = ""GET"")] 
public record ListTodosQuery(string? Status) : IQuery<ListResult> { }

[BluQubeRequester]
internal class EntryPoint { }
";

        // Act
        GeneratorDriver driver = RunRequestingGenerator(code);
        var result = driver.GetRunResult();
        var generated = result.Results
            .SelectMany(r => r.GeneratedSources)
            .Where(s => s.HintName.Contains("GenericQueryProcessor"))
            .ToList();

        // Assert
        // TODO: After implementation, verify generated code:
        // - Uses HttpMethod.Get
        // - BuildPath includes querystring for Status parameter
        Assert.True(generated.Count > 0, "Expected generated source for query processor");
    }

    [Fact(Skip = "Scaffold test — enable after Kaylee's URL binding implementation")]
    public void ServerCommandWithPathParameter_GeneratesBodyShimAndRouteBinding()
    {
        // Arrange
        var code = @"
using System;
using BluQube.Attributes;
using BluQube.Commands;

[BluQubeCommand(Path = ""commands/todo/{id}"")] 
public record DeleteTodoCommand(Guid Id) : ICommand { }

[BluQubeResponder]
internal class Program { }
";

        // Act
        GeneratorDriver driver = RunRespondingGenerator(code);
        var result = driver.GetRunResult();
        var generated = result.Results
            .SelectMany(r => r.GeneratedSources)
            .Where(s => s.HintName.Contains("AddBluQubeApi"))
            .ToList();

        // Assert
        // TODO: After implementation, verify generated code contains:
        // - Body shim record (properties excluding Id)
        // - MapPost endpoint with route template including {id}
        // - Route binding parameter in lambda: (Guid id, BodyShim body) => ...
        Assert.True(generated.Count > 0, "Expected generated source for responder");
    }

    [Fact(Skip = "Scaffold test — enable after Kaylee's URL binding implementation")]
    public void ServerGetQueryWithPathParameter_GeneratesMapGetWithQuerystringShim()
    {
        // Arrange
        var code = @"
using System;
using BluQube.Attributes;
using BluQube.Queries;

public record TodoResult : IQueryResult { }

[BluQubeQuery(Path = ""queries/todo/{id}"", Method = ""GET"")] 
public record GetTodoQuery(Guid Id, string? Filter) : IQuery<TodoResult> { }

[BluQubeResponder]
internal class Program { }
";

        // Act
        GeneratorDriver driver = RunRespondingGenerator(code);
        var result = driver.GetRunResult();
        var generated = result.Results
            .SelectMany(r => r.GeneratedSources)
            .Where(s => s.HintName.Contains("AddBluQubeApi"))
            .ToList();

        // Assert
        // TODO: After implementation, verify generated code contains:
        // - MapGet (not MapPost)
        // - Querystring shim record for Filter parameter
        // - Route binding for id parameter
        Assert.True(generated.Count > 0, "Expected generated source for responder");
    }

    [Fact(Skip = "Scaffold test — enable after Kaylee's URL binding implementation")]
    public void MultiplePathParameters_GeneratesCorrectOrdering()
    {
        // Arrange
        var code = @"
using System;
using BluQube.Attributes;
using BluQube.Commands;

[BluQubeCommand(Path = ""commands/tenant/{tenantId}/todo/{id}"")] 
public record DeleteTenantTodoCommand(Guid TenantId, Guid Id) : ICommand { }

[BluQubeRequester]
internal class EntryPoint { }
";

        // Act
        GeneratorDriver driver = RunRequestingGenerator(code);
        var result = driver.GetRunResult();
        var generated = result.Results
            .SelectMany(r => r.GeneratedSources)
            .Where(s => s.HintName.Contains("GenericCommandHandler"))
            .ToList();

        // Assert
        // TODO: After implementation, verify BuildPath interpolates both params in correct order:
        // $"commands/tenant/{Uri.EscapeDataString(request.TenantId.ToString())}/todo/{Uri.EscapeDataString(request.Id.ToString())}"
        Assert.True(generated.Count > 0, "Expected generated source for command handler");
    }

    private static GeneratorDriver RunRequestingGenerator(string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTest",
            syntaxTrees: new[] { syntaxTree },
            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new BluQube.SourceGeneration.Requesting();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);
        return driver;
    }

    private static GeneratorDriver RunRespondingGenerator(string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTest",
            syntaxTrees: new[] { syntaxTree },
            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new BluQube.SourceGeneration.Responding();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);
        return driver;
    }
}

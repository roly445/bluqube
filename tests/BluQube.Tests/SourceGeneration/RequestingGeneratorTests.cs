using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace BluQube.Tests.SourceGeneration
{
    public class RequestingGeneratorTests
    {
        private static GeneratorDriver RunGenerator(string code)
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

        [Fact]
        public void SkipsGenerationWithoutRequester()
        {
            var code = @"
using BluQube.Attributes;
using BluQube.Commands;

[BluQubeCommand(Path = ""commands/stub"")] 
public class StubCommand : ICommand { }
";
            GeneratorDriver driver = RunGenerator(code);
            var result = driver.GetRunResult();
            var generated = result.Results.SelectMany(r => r.GeneratedSources).ToList();

            Assert.True(generated.Count == 0, "Expected no generated sources without a [BluQubeRequester].");
        }

        [Fact]
        public void GeneratesHandlersWithRequesterPresent()
        {
            var code = @"
using BluQube.Attributes;
using BluQube.Commands;

[BluQubeCommand(Path = ""commands/stub"")] 
public class StubCommand : ICommand { }

[BluQubeRequester]
internal class EntryPoint { }
";
            GeneratorDriver driver = RunGenerator(code);
            var result = driver.GetRunResult();
            var generatedHints = result.Results
                .SelectMany(r => r.GeneratedSources)
                .Select(s => s.HintName)
                .ToList();

            Assert.Contains(generatedHints, h => h.Contains("GenericCommandHandler.g.cs", StringComparison.OrdinalIgnoreCase));
        }
    }
}

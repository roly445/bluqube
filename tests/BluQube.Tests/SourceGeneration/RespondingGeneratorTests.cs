using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace BluQube.Tests.SourceGeneration
{
    public class RespondingGeneratorTests
    {
        [Fact]
        public void SkipsGenerationWithoutResponder()
        {
            var code = @"
using BluQube.Attributes;
using BluQube.Commands;

namespace TestApp.Commands
{
    [BluQubeCommand(Path = ""commands/stub"")]
    public record StubCommand(string Data) : ICommand;

    public class StubCommandHandler : CommandHandler<StubCommand>
    {
        protected override System.Threading.Tasks.Task<CommandResult> HandleInternal(StubCommand request, System.Threading.CancellationToken ct)
            => System.Threading.Tasks.Task.FromResult(CommandResult.Succeeded());
    }
}
";
            var result = RunGenerator(code).GetRunResult();
            var generated = result.Results.SelectMany(r => r.GeneratedSources).ToList();

            Assert.True(generated.Count == 0, "Expected no generated sources without a [BluQubeResponder].");
        }

        [Fact]
        public void GeneratesEndpointForICommand()
        {
            var code = BuildResponderCode(@"
namespace TestApp.Commands
{
    [BluQubeCommand(Path = ""commands/stub"")]
    public record StubCommand(string Data) : ICommand;

    public class StubCommandHandler : CommandHandler<StubCommand>
    {
        protected override System.Threading.Tasks.Task<CommandResult> HandleInternal(StubCommand request, System.Threading.CancellationToken ct)
            => System.Threading.Tasks.Task.FromResult(CommandResult.Succeeded());
    }
}
");

            var result = RunGenerator(code).GetRunResult();
            var endpointSource = GetGeneratedSource(result, "EndpointRouteBuilderExtensions.g.cs");

            Assert.NotNull(endpointSource);
            Assert.Contains("MapPost(\"commands/stub\"", endpointSource);
        }

        [Fact]
        public void GeneratesEndpointForICommandWithResult()
        {
            var code = BuildResponderCode(@"
namespace TestApp.Commands
{
    [BluQubeCommand(Path = ""commands/create"")]
    public record CreateCommand(string Title) : ICommand<CreateResult>;

    public record CreateResult(int Id) : ICommandResult;

    public class CreateCommandHandler : CommandHandler<CreateCommand, CreateResult>
    {
        protected override System.Threading.Tasks.Task<CommandResult<CreateResult>> HandleInternal(CreateCommand request, System.Threading.CancellationToken ct)
            => System.Threading.Tasks.Task.FromResult(CommandResult<CreateResult>.Succeeded(new CreateResult(1)));
    }
}
");

            var result = RunGenerator(code).GetRunResult();
            var endpointSource = GetGeneratedSource(result, "EndpointRouteBuilderExtensions.g.cs");

            Assert.NotNull(endpointSource);
            Assert.Contains("MapPost(\"commands/create\"", endpointSource);
        }

        [Fact]
        public void RegistersCommandResultConverterForICommandWithResult()
        {
            var code = BuildResponderCode(@"
namespace TestApp.Commands
{
    [BluQubeCommand(Path = ""commands/create"")]
    public record CreateCommand(string Title) : ICommand<CreateResult>;

    public record CreateResult(int Id) : ICommandResult;

    public class CreateCommandHandler : CommandHandler<CreateCommand, CreateResult>
    {
        protected override System.Threading.Tasks.Task<CommandResult<CreateResult>> HandleInternal(CreateCommand request, System.Threading.CancellationToken ct)
            => System.Threading.Tasks.Task.FromResult(CommandResult<CreateResult>.Succeeded(new CreateResult(1)));
    }
}
");

            var result = RunGenerator(code).GetRunResult();
            var jsonSource = GetGeneratedSource(result, "JsonOptionsExtensions.g.cs");

            Assert.NotNull(jsonSource);

            // Should use CommandResultConverter<TResult> directly, not a named {TResult}Converter class
            Assert.Contains("CommandResultConverter<", jsonSource);
            Assert.Contains("CreateResult", jsonSource);
        }

        [Fact]
        public void DoesNotRegisterConverterForICommandWithoutResult()
        {
            var code = BuildResponderCode(@"
namespace TestApp.Commands
{
    [BluQubeCommand(Path = ""commands/stub"")]
    public record StubCommand(string Data) : ICommand;

    public class StubCommandHandler : CommandHandler<StubCommand>
    {
        protected override System.Threading.Tasks.Task<CommandResult> HandleInternal(StubCommand request, System.Threading.CancellationToken ct)
            => System.Threading.Tasks.Task.FromResult(CommandResult.Succeeded());
    }
}
");

            var result = RunGenerator(code).GetRunResult();
            var jsonSource = GetGeneratedSource(result, "JsonOptionsExtensions.g.cs");

            Assert.NotNull(jsonSource);
            Assert.DoesNotContain("CommandResultConverter<", jsonSource);
        }

        [Fact]
        public void RegistersCommandResultConverterWhenNoClientAssemblyReferencePresent()
        {
            // Regression test: previously the server generator looked for a named {TResult}Converter
            // class that is only generated in the client project. In modular server setups that don't
            // reference the client WASM assembly, no converter was registered, silently breaking
            // JSON serialization for ICommand<TResult> responses.
            var code = BuildResponderCode(@"
namespace TestApp.Equipment.Commands
{
    [BluQubeCommand(Path = ""commands/equipment/create"")]
    public record CreateEquipmentCommand(string Name) : ICommand<CreateEquipmentResult>;

    public record CreateEquipmentResult(int EquipmentId) : ICommandResult;

    public class CreateEquipmentCommandHandler : CommandHandler<CreateEquipmentCommand, CreateEquipmentResult>
    {
        protected override System.Threading.Tasks.Task<CommandResult<CreateEquipmentResult>> HandleInternal(CreateEquipmentCommand request, System.Threading.CancellationToken ct)
            => System.Threading.Tasks.Task.FromResult(CommandResult<CreateEquipmentResult>.Succeeded(new CreateEquipmentResult(42)));
    }
}
");

            // This compilation intentionally has NO reference to any assembly containing a named
            // CreateEquipmentResultConverter class, simulating a server module that doesn't reference
            // the client WASM project.
            var result = RunGenerator(code).GetRunResult();
            var jsonSource = GetGeneratedSource(result, "JsonOptionsExtensions.g.cs");

            Assert.NotNull(jsonSource);
            Assert.Contains("CommandResultConverter<", jsonSource);
            Assert.Contains("CreateEquipmentResult", jsonSource);
        }

        private static string BuildResponderCode(string body) => $@"
using BluQube.Attributes;
using BluQube.Commands;

[BluQubeResponder]
internal class EntryPoint {{ }}

{body}
";

        private static string? GetGeneratedSource(GeneratorDriverRunResult result, string hintName)
        {
            return result.Results
                .SelectMany(r => r.GeneratedSources)
                .FirstOrDefault(s => s.HintName.EndsWith(hintName, System.StringComparison.OrdinalIgnoreCase))
                .SourceText?.ToString();
        }

        private static GeneratorDriver RunGenerator(string code)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(BluQube.Attributes.BluQubeResponderAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(BluQube.Commands.CommandHandler<>).Assembly.Location),
            };

            var trustedAssemblies = System.AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
            if (trustedAssemblies != null)
            {
                foreach (var path in trustedAssemblies.Split(System.IO.Path.PathSeparator))
                {
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        references.Add(MetadataReference.CreateFromFile(path));
                    }
                }
            }

            var compilation = CSharpCompilation.Create(
                assemblyName: "RespondingGeneratorTest",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var generator = new BluQube.SourceGeneration.Responding();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
            driver = driver.RunGenerators(compilation);
            return driver;
        }
    }
}
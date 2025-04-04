using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BluQube.SourceGeneration.DefinitionProcessors.InputDefinitionProcessors;
using BluQube.SourceGeneration.DefinitionProcessors.OutputDefinitionProcessors.Responding;
using BluQube.SourceGeneration.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using CommandHandlerInputDefinition = BluQube.SourceGeneration.DefinitionProcessors.InputDefinitionProcessors.CommandHandlerInputDefinitionProcess.InputDefinition;
using QueryProcessorInputDefinition = BluQube.SourceGeneration.DefinitionProcessors.InputDefinitionProcessors.QueryProcessorInputDefinitionProcess.InputDefinition;
using ResponderInputDefinition = BluQube.SourceGeneration.DefinitionProcessors.InputDefinitionProcessors.ResponderInputDefinitionProcessor.InputDefinition;

namespace BluQube.SourceGeneration
{
    [Generator]
    public class Responding : IIncrementalGenerator
    {
        private enum InputType
        {
            None,
            Responder,
            QueryProcessor,
            CommandHandler,
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var responderInputDefinitionProcessor = new ResponderInputDefinitionProcessor();
            var queryProcessorInputDefinitionProcess = new QueryProcessorInputDefinitionProcess();
            var commandHandlerInputDefinitionProcess = new CommandHandlerInputDefinitionProcess();

            var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(
                    predicate: (syntaxNode, _) =>
                        responderInputDefinitionProcessor.CanProcess(syntaxNode) ||
                        queryProcessorInputDefinitionProcess.CanProcess(syntaxNode) ||
                        commandHandlerInputDefinitionProcess.CanProcess(syntaxNode),
                    transform: (ctx, _) => new Container(
                        ctx.SemanticModel,
                        responderInputDefinitionProcessor.Process(ctx.Node),
                        queryProcessorInputDefinitionProcess.Process(ctx.Node),
                        commandHandlerInputDefinitionProcess.Process(ctx.Node)))
                .Where(result => result.InputType != InputType.None);

            var combined = syntaxProvider.Collect().Combine(context.CompilationProvider);

            context.RegisterSourceOutput(combined, (spc, source) =>
            {
                var responderDefinition = source.Left.SingleOrDefault(x => x.InputType == InputType.Responder);
                if (responderDefinition == null)
                {
                    return;
                }

                var endpointRouteBuilderExtensionsOutputDefinitionProcessor =
                    new EndpointRouteBuilderExtensionsOutputDefinitionProcessor();
                var jsonOptionsExtensionsOutputDefinitionProcessor =
                    new JsonOptionsExtensionsOutputDefinitionProcessor();
                var queriesToProcess =
                    new List<EndpointRouteBuilderExtensionsOutputDefinitionProcessor.OutputDefinition.QueryToProcess>();
                var commandsToProcess =
                    new List<EndpointRouteBuilderExtensionsOutputDefinitionProcessor.OutputDefinition.CommandToProcess>();
                var jsonConvertersToProcess = new List<JsonOptionsExtensionsOutputDefinitionProcessor.OutputDefinition.JsonConverterToProcess>();

                foreach (var reference in source.Right.References)
                {
                    if (source.Right.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assemblySymbol)
                    {
                        continue;
                    }

                    foreach (var container in source.Left.Where(x => x.InputType == InputType.QueryProcessor))
                    {
                        var typeSymbol = assemblySymbol.GetTypeByMetadataName($"{container.QueryProcessor.QueryDeclaration.GetNamespace(container.SemanticModel)}.{container.QueryProcessor.QueryDeclaration.ToString()}");
                        if (typeSymbol == null)
                        {
                            continue;
                        }

                        var bluQubeQueryAttributeSyntax = typeSymbol.GetAttributes()
                            .Where(x => x.AttributeClass?.Name == "BluQubeQueryAttribute")
                            .Select(x => x.NamedArguments.SingleOrDefault(y => y.Key == "Path").Value.Value?.ToString())
                            .SingleOrDefault();
                        if (bluQubeQueryAttributeSyntax == null)
                        {
                            continue;
                        }

                        queriesToProcess.Add(
                            new EndpointRouteBuilderExtensionsOutputDefinitionProcessor.OutputDefinition.
                                QueryToProcess(
                                    typeSymbol.Name,
                                    typeSymbol.ContainingNamespace.ToDisplayString(),
                                    bluQubeQueryAttributeSyntax));

                        var converterName = assemblySymbol.TypeNames.SingleOrDefault(x => x.Contains($"{container.QueryProcessor.QueryDeclaration.ToString()}ResultConverter"));
                        if (string.IsNullOrWhiteSpace(converterName))
                        {
                            continue;
                        }

                        var converterType = FindTypeByName(assemblySymbol, converterName);
                        if (converterType == null)
                        {
                            continue;
                        }

                        jsonConvertersToProcess.Add(new JsonOptionsExtensionsOutputDefinitionProcessor.OutputDefinition.JsonConverterToProcess(converterType.ContainingNamespace.ToDisplayString(), converterName));
                    }

                    foreach (var container in source.Left.Where(x => x.InputType == InputType.CommandHandler))
                    {
                        var typeSymbol = assemblySymbol.GetTypeByMetadataName($"{container.CommandHandler.CommandDeclaration.GetNamespace(container.SemanticModel)}.{container.CommandHandler.CommandDeclaration.ToString()}");
                        if (typeSymbol == null)
                        {
                            continue;
                        }

                        var bluQubeQueryAttributeSyntax = typeSymbol.GetAttributes()
                            .Where(x => x.AttributeClass?.Name == "BluQubeCommandAttribute")
                            .Select(x => x.NamedArguments.SingleOrDefault(y => y.Key == "Path").Value.Value?.ToString())
                            .SingleOrDefault();
                        if (bluQubeQueryAttributeSyntax == null)
                        {
                            continue;
                        }

                        commandsToProcess.Add(
                                new EndpointRouteBuilderExtensionsOutputDefinitionProcessor.OutputDefinition.
                                    CommandToProcess(
                                        typeSymbol.Name,
                                        typeSymbol.ContainingNamespace.ToDisplayString(),
                                        bluQubeQueryAttributeSyntax));

                        var converterName = assemblySymbol.TypeNames.SingleOrDefault(x => x.Contains($"{container.CommandHandler.CommandDeclaration.ToString()}ResultConverter"));
                        if (string.IsNullOrWhiteSpace(converterName))
                        {
                            continue;
                        }

                        var converterType = FindTypeByName(assemblySymbol, converterName);
                        if (converterType == null)
                        {
                            continue;
                        }

                        jsonConvertersToProcess.Add(new JsonOptionsExtensionsOutputDefinitionProcessor.OutputDefinition.JsonConverterToProcess(converterType.ContainingNamespace.ToDisplayString(), converterName));
                    }
                }

                var src = endpointRouteBuilderExtensionsOutputDefinitionProcessor.Process(
                    new EndpointRouteBuilderExtensionsOutputDefinitionProcessor.OutputDefinition(
                        responderDefinition.Responder.TypeWithAttribute.GetNamespace(),
                        queriesToProcess,
                        commandsToProcess));
                spc.AddSource(
                    $"EndpointRouteBuilderExtensions.g.cs",
                    SourceText.From(src, Encoding.UTF8));

                src = jsonOptionsExtensionsOutputDefinitionProcessor.Process(
                    new JsonOptionsExtensionsOutputDefinitionProcessor.OutputDefinition(
                        responderDefinition.Responder.TypeWithAttribute.GetNamespace(),
                        jsonConvertersToProcess));
                spc.AddSource(
                    $"JsonOptionsExtensions.g.cs",
                    SourceText.From(src, Encoding.UTF8));
            });
        }

        private static INamedTypeSymbol? FindTypeByName(IAssemblySymbol assemblySymbol, string typeName)
        {
            foreach (var namespaceSymbol in assemblySymbol.GlobalNamespace.GetNamespaceMembers())
            {
                var typeSymbol = FindTypeInNamespace(namespaceSymbol, typeName);
                if (typeSymbol != null)
                {
                    return typeSymbol;
                }
            }

            return null;
        }

        private static INamedTypeSymbol? FindTypeInNamespace(INamespaceSymbol namespaceSymbol, string typeName)
        {
            foreach (var typeSymbol in namespaceSymbol.GetTypeMembers())
            {
                if (typeSymbol.Name == typeName)
                {
                    return typeSymbol;
                }
            }

            foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                var typeSymbol = FindTypeInNamespace(nestedNamespace, typeName);
                if (typeSymbol != null)
                {
                    return typeSymbol;
                }
            }

            return null;
        }

        private sealed class Container
        {
            private readonly ResponderInputDefinition? _responder;
            private readonly QueryProcessorInputDefinition? _queryProcessor;
            private readonly CommandHandlerInputDefinition? _commandHandler;

            public Container(
                SemanticModel semanticModel,
                ResponderInputDefinition? responderInputDefinition,
                QueryProcessorInputDefinition? queryProcessorInputDefinition,
                CommandHandlerInputDefinition? commandHandlerInputDefinition)
            {
                this.SemanticModel = semanticModel;
                if (responderInputDefinition != null)
                {
                    this.InputType = InputType.Responder;
                    this._responder = responderInputDefinition;
                }
                else if (queryProcessorInputDefinition != null)
                {
                    this.InputType = InputType.QueryProcessor;
                    this._queryProcessor = queryProcessorInputDefinition;
                }
                else if (commandHandlerInputDefinition != null)
                {
                    this.InputType = InputType.CommandHandler;
                    this._commandHandler = commandHandlerInputDefinition;
                }
                else
                {
                    this.InputType = InputType.None;
                }
            }

            public SemanticModel SemanticModel { get; }

            public InputType InputType { get; }

            public ResponderInputDefinition Responder
            {
                get
                {
                    if (this.InputType == InputType.Responder)
                    {
                        return this._responder!;
                    }

                    throw new InvalidOperationException("Invalid input type");
                }
            }

            public QueryProcessorInputDefinition QueryProcessor
            {
                get
                {
                    if (this.InputType == InputType.QueryProcessor)
                    {
                        return this._queryProcessor!;
                    }

                    throw new InvalidOperationException("Invalid input type");
                }
            }

            public CommandHandlerInputDefinition CommandHandler
            {
                get
                {
                    if (this.InputType == InputType.CommandHandler)
                    {
                        return this._commandHandler!;
                    }

                    throw new InvalidOperationException("Invalid input type");
                }
            }
        }
    }
}
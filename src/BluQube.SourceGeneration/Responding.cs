using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BluQube.SourceGeneration.DefinitionProcessors.InputDefinitionProcessors;
using BluQube.SourceGeneration.DefinitionProcessors.OutputDefinitionProcessors.Responding;
using BluQube.SourceGeneration.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using AuthorizerInputDefinition = BluQube.SourceGeneration.DefinitionProcessors.InputDefinitionProcessors.AuthorizerInputDefinitionProcessor.InputDefinition;
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
            Authorizer,
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var responderInputDefinitionProcessor = new ResponderInputDefinitionProcessor();
            var queryProcessorInputDefinitionProcess = new QueryProcessorInputDefinitionProcess();
            var commandHandlerInputDefinitionProcess = new CommandHandlerInputDefinitionProcess();
            var authorizerInputDefinitionProcessor = new AuthorizerInputDefinitionProcessor();

            var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(
                    predicate: (syntaxNode, _) =>
                        responderInputDefinitionProcessor.CanProcess(syntaxNode) ||
                        queryProcessorInputDefinitionProcess.CanProcess(syntaxNode) ||
                        commandHandlerInputDefinitionProcess.CanProcess(syntaxNode) ||
                        authorizerInputDefinitionProcessor.CanProcess(syntaxNode),
                    transform: (ctx, _) => new Container(
                        ctx.SemanticModel,
                        responderInputDefinitionProcessor.Process(ctx.Node),
                        queryProcessorInputDefinitionProcess.Process(ctx.Node),
                        commandHandlerInputDefinitionProcess.Process(ctx.Node),
                        authorizerInputDefinitionProcessor.Process(ctx.Node)))
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

                        var bluQubeQueryAttributeSymbol = typeSymbol.GetAttributes()
                            .FirstOrDefault(x => x.AttributeClass?.Name == "BluQubeQueryAttribute");
                        if (bluQubeQueryAttributeSymbol == null)
                        {
                            continue;
                        }

                        var pathValue = bluQubeQueryAttributeSymbol.NamedArguments
                            .SingleOrDefault(y => y.Key == "Path").Value.Value?.ToString();
                        if (pathValue == null)
                        {
                            continue;
                        }

                        var httpMethodValue = bluQubeQueryAttributeSymbol.NamedArguments
                            .FirstOrDefault(y => y.Key == "HttpMethod").Value.Value?.ToString() ?? "Get";

                        // Convert enum name to HTTP method string (e.g., "Get" -> "GET", "Post" -> "POST")
                        var httpMethodString = httpMethodValue.Equals("Post", System.StringComparison.OrdinalIgnoreCase) ? "POST" : "GET";

                        queriesToProcess.Add(
                            new EndpointRouteBuilderExtensionsOutputDefinitionProcessor.OutputDefinition.
                                QueryToProcess(
                                    typeSymbol.Name,
                                    typeSymbol.ContainingNamespace.ToDisplayString(),
                                    pathValue,
                                    httpMethodString));

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

                // Collect authorization requirements from authorizers
                var authorizationMap = new Dictionary<string, List<string>>();
                foreach (var container in source.Left.Where(x => x.InputType == InputType.Authorizer))
                {
                    var requestTypeNamespace = container.Authorizer.RequestDeclaration.GetNamespace(container.SemanticModel);
                    var requestTypeName = container.Authorizer.RequestDeclaration.ToString();
                    var fullTypeName = $"{requestTypeNamespace}.{requestTypeName}";

                    foreach (var reference in source.Right.References)
                    {
                        if (source.Right.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assemblySymbol)
                        {
                            continue;
                        }

                        var typeSymbol = assemblySymbol.GetTypeByMetadataName(fullTypeName);
                        if (typeSymbol != null)
                        {
                            authorizationMap[typeSymbol.Name] = container.Authorizer.Requirements;
                            break;
                        }
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

                // Generate OpenAPI specification
                var openApiOutputDefinitionProcessor = new OpenApiOutputDefinitionProcessor();
                var openApiQueries = queriesToProcess.Select(q =>
                    new OpenApiOutputDefinitionProcessor.OutputDefinition.QueryToProcess(
                        q.Query, q.QueryNamespace, q.Path, q.HttpMethod)).ToList();
                var openApiCommands = commandsToProcess.GroupBy(x => x.Path.ToLower())
                    .Select(g => g.First())
                    .Select(c => new OpenApiOutputDefinitionProcessor.OutputDefinition.CommandToProcess(
                        c.Command, c.CommandNamespace, c.Path)).ToList();

                var openApiJson = openApiOutputDefinitionProcessor.Process(
                    new OpenApiOutputDefinitionProcessor.OutputDefinition(
                        responderDefinition.Responder.TypeWithAttribute.GetNamespace(),
                        openApiQueries,
                        openApiCommands,
                        authorizationMap,
                        responderDefinition.Responder.OpenApiSecurityScheme));

                // Generate C# class with OpenAPI spec
                var openApiCsClass = new StringBuilder();
                openApiCsClass.AppendLine("using Microsoft.AspNetCore.Builder;");
                openApiCsClass.AppendLine("using Microsoft.AspNetCore.Http;");
                openApiCsClass.AppendLine();
                openApiCsClass.AppendLine($"namespace {responderDefinition.Responder.TypeWithAttribute.GetNamespace()};");
                openApiCsClass.AppendLine();
                openApiCsClass.AppendLine("internal static class OpenApiExtensions");
                openApiCsClass.AppendLine("{");
                openApiCsClass.AppendLine("    private const string OpenApiSpec = @\"");
                openApiCsClass.AppendLine(openApiJson.Replace("\"", "\"\""));
                openApiCsClass.AppendLine("\";");
                openApiCsClass.AppendLine();
                openApiCsClass.AppendLine("    internal static IEndpointRouteBuilder MapBluQubeOpenApi(this IEndpointRouteBuilder endpoints)");
                openApiCsClass.AppendLine("    {");
                openApiCsClass.AppendLine("        endpoints.MapGet(\"/openapi.json\", () => Results.Content(OpenApiSpec, \"application/json\"));");
                openApiCsClass.AppendLine("        return endpoints;");
                openApiCsClass.AppendLine("    }");
                openApiCsClass.AppendLine("}");

                spc.AddSource(
                    "OpenApiExtensions.g.cs",
                    SourceText.From(openApiCsClass.ToString(), Encoding.UTF8));
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
            private readonly AuthorizerInputDefinition? _authorizer;

            public Container(
                SemanticModel semanticModel,
                ResponderInputDefinition? responderInputDefinition,
                QueryProcessorInputDefinition? queryProcessorInputDefinition,
                CommandHandlerInputDefinition? commandHandlerInputDefinition,
                AuthorizerInputDefinition? authorizerInputDefinition)
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
                else if (authorizerInputDefinition != null)
                {
                    this.InputType = InputType.Authorizer;
                    this._authorizer = authorizerInputDefinition;
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

            public AuthorizerInputDefinition Authorizer
            {
                get
                {
                    if (this.InputType == InputType.Authorizer)
                    {
                        return this._authorizer!;
                    }

                    throw new InvalidOperationException("Invalid input type");
                }
            }
        }
    }
}
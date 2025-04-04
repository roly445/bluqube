using System.Linq;
using System.Text;
using BluQube.SourceGeneration.DefinitionProcessors.InputDefinitionProcessors;
using BluQube.SourceGeneration.DefinitionProcessors.OutputDefinitionProcessors;
using BluQube.SourceGeneration.DefinitionProcessors.OutputDefinitionProcessors.Requesting;
using BluQube.SourceGeneration.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using CommandInputDefinition = BluQube.SourceGeneration.DefinitionProcessors.InputDefinitionProcessors.CommandInputDefinitionProcessor.InputDefinition;
using CommandWithResultInputDefinition = BluQube.SourceGeneration.DefinitionProcessors.InputDefinitionProcessors.CommandWithResultInputDefinitionProcessor.InputDefinition;
using GenericCommandHandlerOutputDefinition = BluQube.SourceGeneration.DefinitionProcessors.OutputDefinitionProcessors.GenericCommandHandlerOutputDefinitionProcessor.OutputDefinition;
using GenericCommandOfTHandlerOutputDefinition = BluQube.SourceGeneration.DefinitionProcessors.OutputDefinitionProcessors.GenericCommandOfTHandlerOutputDefinitionProcessor.OutputDefinition;
using JsonConverterOutputDefinition = BluQube.SourceGeneration.DefinitionProcessors.OutputDefinitionProcessors.JsonConverterOutputDefinitionProcessor.OutputDefinition;
using QueryInputDefinition = BluQube.SourceGeneration.DefinitionProcessors.InputDefinitionProcessors.QueryInputDefinitionProcessor.InputDefinition;
using QueryProcessorOutputDefinition = BluQube.SourceGeneration.DefinitionProcessors.OutputDefinitionProcessors.QueryProcessorOutputDefinitionProcessor.OutputDefinition;
using RequesterInputDefinition = BluQube.SourceGeneration.DefinitionProcessors.InputDefinitionProcessors.RequesterInputDefinitionProcessor.InputDefinition;

namespace BluQube.SourceGeneration
{
    [Generator]
    public class Requesting : IIncrementalGenerator
    {
        private enum InputType
        {
            None,
            Requester,
            Query,
            Command,
            CommandHandlerWithResult,
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var queryInputDefinitionProcessor = new QueryInputDefinitionProcessor();
            var commandInputDefinitionProcessor = new CommandInputDefinitionProcessor();
            var commandWithResultInputDefinitionProcessor = new CommandWithResultInputDefinitionProcessor();
            var requesterInputDefinitionProcessor = new RequesterInputDefinitionProcessor();

            var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(
                    predicate: (syntaxNode, _) =>
                        queryInputDefinitionProcessor.CanProcess(syntaxNode) ||
                        commandInputDefinitionProcessor.CanProcess(syntaxNode) ||
                        commandWithResultInputDefinitionProcessor.CanProcess(syntaxNode) ||
                        requesterInputDefinitionProcessor.CanProcess(syntaxNode),
                    transform: (ctx, _) => new Container(
                        ctx.SemanticModel,
                        requesterInputDefinitionProcessor.Process(ctx.Node),
                        queryInputDefinitionProcessor.Process(ctx.Node),
                        commandInputDefinitionProcessor.Process(ctx.Node),
                        commandWithResultInputDefinitionProcessor.Process(ctx.Node)))
                .Where(result => result.InputType != InputType.None);

            var combined = syntaxProvider.Collect();

            context.RegisterSourceOutput(combined, (spc, source) =>
            {
                var queryOutputProcessor = new QueryProcessorOutputDefinitionProcessor();
                var genericCommandHandlerOutputDefinitionProcessor = new GenericCommandHandlerOutputDefinitionProcessor();
                var genericCommandOfTHandlerOutputDefinitionProcessor = new GenericCommandOfTHandlerOutputDefinitionProcessor();
                var jsonConverterOutputProcessor = new JsonConverterOutputDefinitionProcessor();
                var requesterHelperOutputDefinitionProcessor = new ServiceCollectionExtensionsOutputDefinitionProcessor();
                var requesterDefinition = source.SingleOrDefault(x => x.InputType == InputType.Requester);
                if (requesterDefinition == null)
                {
                    return;
                }

                var queryProcessorOutputDefinitions = source.Where(x => x.InputType == InputType.Query)
                    .Select(x =>
                    {
                        var queryName = x.QueryProcessor.QueryDeclaration.Identifier.Text;
                        var queryNamespace = x.QueryProcessor.QueryDeclaration.GetNamespace();
                        var queryResultNamespace = x.QueryProcessor.QueryResultDeclaration.GetNamespace(x.SemanticModel);
                        var queryResult = x.QueryProcessor.QueryResultDeclaration.ToString();

                        return new QueryProcessorOutputDefinition(
                            queryNamespace,
                            queryResultNamespace,
                            queryNamespace.Replace("Queries", "QueryProcessors"),
                            queryName,
                            queryResult,
                            x.QueryProcessor.BluQubeQueryAttributeSyntax.GetPath() ?? string.Empty);
                    }).ToList();

                var genericCommandHandlerOutputDefinitions = source.Where(x => x.InputType == InputType.Command)
                    .Select(x =>
                    {
                        var commandName = x.Command.CommandDeclaration.Identifier.Text;
                        var commandNamespace = x.Command.CommandDeclaration.GetNamespace();
                        return new GenericCommandHandlerOutputDefinition(
                            commandNamespace,
                            commandNamespace.Replace("Commands", "CommandHandlers"),
                            commandName,
                            x.Command.BluQubeCommandAttributeSyntax.GetPath() ?? string.Empty);
                    }).ToList();

                var genericCommandOfTHandlerOutputDefinitions = source.Where(x => x.InputType == InputType.CommandHandlerWithResult)
                    .Select(x =>
                    {
                        var commandName = x.CommandWithResultHandler.CommandDeclaration.Identifier.Text;
                        var commandNamespace = x.CommandWithResultHandler.CommandDeclaration.GetNamespace();
                        var commandResultNamespace = x.CommandWithResultHandler.CommandResultDeclaration.GetNamespace(x.SemanticModel);
                        var commandResult = x.CommandWithResultHandler.CommandResultDeclaration.ToString();
                        return new GenericCommandOfTHandlerOutputDefinition(
                            commandNamespace,
                            commandResultNamespace,
                            commandNamespace.Replace("Commands", "CommandHandlers"),
                            commandName,
                            commandResult,
                            x.CommandWithResultHandler.BluQubeCommandAttributeSyntax.GetPath() ?? string.Empty);
                    }).ToList();

                var jsonConverterOutputDefinitions = source.Where(x => x.InputType == InputType.Query)
                    .Select(x =>
                    {
                        var queryResultNamespace = x.QueryProcessor.QueryResultDeclaration.GetNamespace(x.SemanticModel);
                        var queryResult = x.QueryProcessor.QueryResultDeclaration.ToString();

                        return new JsonConverterOutputDefinition(
                            queryResultNamespace,
                            queryResultNamespace.Replace("Queries", "QueryProcessors"),
                            queryResult,
                            JsonConverterOutputDefinition.Type.QueryResult);
                    }).ToList();

                jsonConverterOutputDefinitions.AddRange(source.Where(x => x.InputType == InputType.CommandHandlerWithResult)
                    .Select(x =>
                    {
                        var commandResultNamespace = x.CommandWithResultHandler.CommandResultDeclaration.GetNamespace(x.SemanticModel);
                        var commandResult = x.CommandWithResultHandler.CommandResultDeclaration.ToString();

                        return new JsonConverterOutputDefinition(
                            commandResultNamespace,
                            commandResultNamespace.Replace("Commands", "CommandHandlers"),
                            commandResult,
                            JsonConverterOutputDefinition.Type.CommandResult);
                    }).ToList());

                foreach (var queryProcessorOutputDefinition in queryProcessorOutputDefinitions)
                {
                    spc.AddSource(
                             $"{queryProcessorOutputDefinition.QueryResultNamespace}_{queryProcessorOutputDefinition.QueryName}QueryProcessor.g.cs",
                             SourceText.From(queryOutputProcessor.Process(queryProcessorOutputDefinition), Encoding.UTF8));
                }

                foreach (var genericCommandHandlerOutputDefinition in genericCommandHandlerOutputDefinitions)
                {
                    spc.AddSource(
                        $"{genericCommandHandlerOutputDefinition.CommandNamespace}_{genericCommandHandlerOutputDefinition.CommandName}GenericCommandHandler.g.cs",
                        SourceText.From(genericCommandHandlerOutputDefinitionProcessor.Process(genericCommandHandlerOutputDefinition), Encoding.UTF8));
                }

                foreach (var genericCommandOfTHandlerOutputDefinition in genericCommandOfTHandlerOutputDefinitions)
                {
                    spc.AddSource(
                        $"{genericCommandOfTHandlerOutputDefinition.CommandNamespace}_{genericCommandOfTHandlerOutputDefinition.CommandName}GenericCommandHandler.g.cs",
                        SourceText.From(genericCommandOfTHandlerOutputDefinitionProcessor.Process(genericCommandOfTHandlerOutputDefinition), Encoding.UTF8));
                }

                foreach (var jsonConverterOutputDefinition in jsonConverterOutputDefinitions)
                {
                    spc.AddSource(
                             $"{jsonConverterOutputDefinition.QueryResultNamespace}_{jsonConverterOutputDefinition.QueryResult}Converter.g.cs",
                             SourceText.From(jsonConverterOutputProcessor.Process(jsonConverterOutputDefinition), Encoding.UTF8));
                }

                var src = requesterHelperOutputDefinitionProcessor.Process(
                    new ServiceCollectionExtensionsOutputDefinitionProcessor.OutputDefinition(requesterDefinition.Requester.TypeWithAttribute.GetNamespace(), jsonConverterOutputDefinitions.AsReadOnly()));
                spc.AddSource(
                    $"ServiceCollectionExtensions.g.cs",
                    SourceText.From(src, Encoding.UTF8));
            });
        }

        private sealed class Container
        {
            private readonly RequesterInputDefinition? _requester;
            private readonly QueryInputDefinition? _query;
            private readonly CommandWithResultInputDefinition? _commandWithResult;
            private readonly CommandInputDefinition? _command;

            public Container(
                SemanticModel semanticModel,
                RequesterInputDefinition? requesterInputDefinition,
                QueryInputDefinition? queryInputDefinition,
                CommandInputDefinition? commandInputDefinition,
                CommandWithResultInputDefinition? commandWithResultInputDefinition)
            {
                this.SemanticModel = semanticModel;
                if (requesterInputDefinition != null)
                {
                    this._requester = requesterInputDefinition;
                    this.InputType = InputType.Requester;
                }
                else if (queryInputDefinition != null)
                {
                    this._query = queryInputDefinition;
                    this.InputType = InputType.Query;
                }
                else if (commandInputDefinition != null)
                {
                    this._command = commandInputDefinition;
                    this.InputType = InputType.Command;
                }
                else if (commandWithResultInputDefinition != null)
                {
                    this._commandWithResult = commandWithResultInputDefinition;
                    this.InputType = InputType.CommandHandlerWithResult;
                }
                else
                {
                    this.InputType = InputType.None;
                }
            }

            public InputType InputType { get; }

            public SemanticModel SemanticModel { get; }

            public RequesterInputDefinition Requester
            {
                get
                {
                    if (this.InputType == InputType.Requester)
                    {
                        return this._requester!;
                    }

                    throw new System.InvalidOperationException("Invalid input type");
                }
            }

            public QueryInputDefinition QueryProcessor
            {
                get
                {
                    if (this.InputType == InputType.Query)
                    {
                        return this._query!;
                    }

                    throw new System.InvalidOperationException("Invalid input type");
                }
            }

            public CommandInputDefinition Command
            {
                get
                {
                    if (this.InputType == InputType.Command)
                    {
                        return this._command!;
                    }

                    throw new System.InvalidOperationException("Invalid input type");
                }
            }

            public CommandWithResultInputDefinition CommandWithResultHandler
            {
                get
                {
                    if (this.InputType == InputType.CommandHandlerWithResult)
                    {
                        return this._commandWithResult!;
                    }

                    throw new System.InvalidOperationException("Invalid input type");
                }
            }
        }
    }
}
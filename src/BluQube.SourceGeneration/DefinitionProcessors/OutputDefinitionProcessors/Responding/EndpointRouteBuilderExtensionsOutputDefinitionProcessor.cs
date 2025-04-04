using System.Collections.Generic;
using System.Linq;
using System.Text;
using BluQube.CodeGenerators.Contracts;

namespace BluQube.SourceGeneration.DefinitionProcessors.OutputDefinitionProcessors.Responding
{
    internal class EndpointRouteBuilderExtensionsOutputDefinitionProcessor : IOutputDefinitionProcessor<EndpointRouteBuilderExtensionsOutputDefinitionProcessor.OutputDefinition>
    {
        public string Process(OutputDefinition data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using BluQube.Queries;");
            sb.AppendLine("using BluQube.Commands;");
            foreach (var queryToProcess in data.QueriesToProcess.Select(x => x.QueryNamespace).Distinct())
            {
                sb.AppendLine($"using {queryToProcess};");
            }

            foreach (var commandToProcess in data.CommandsToProcess.Select(x => x.CommandNamespace).Distinct())
            {
                sb.AppendLine($"using {commandToProcess};");
            }

            sb.AppendLine($@"namespace {data.Namespace};
");

            sb.AppendLine($@"internal static class EndpointRouteBuilderExtensions
{{
     internal static IEndpointRouteBuilder AddBluQubeApi(this IEndpointRouteBuilder endpointRouteBuilder)
     {{");
            foreach (var queryToProcess in data.QueriesToProcess)
            {
                sb.AppendLine($"        endpointRouteBuilder.MapPost(\"{queryToProcess.Path}\", async (IQuerier querier, {queryToProcess.Query} query) => {{");
                sb.AppendLine("             var data = await querier.Send(query);");
                sb.AppendLine("             return Results.Json(data);");
                sb.AppendLine("         });");
            }

            foreach (var commandToProcess in data.CommandsToProcess)
            {
                sb.AppendLine($"        endpointRouteBuilder.MapPost(\"{commandToProcess.Path}\", async (ICommander commander, {commandToProcess.Command} command) => {{");
                sb.AppendLine("             var data = await commander.Send(command);");
                sb.AppendLine("             return Results.Json(data);");
                sb.AppendLine("         });");
            }

            sb.AppendLine(@"        return endpointRouteBuilder;
     }
}");
            return sb.ToString();
        }

        internal class OutputDefinition : IOutputDefinition
        {
            internal OutputDefinition(string ns, IReadOnlyList<QueryToProcess> queriesToProcess, IReadOnlyList<CommandToProcess> commandsToProcess)
            {
                this.Namespace = ns;
                this.QueriesToProcess = queriesToProcess;
                this.CommandsToProcess = commandsToProcess;
            }

            internal string Namespace { get; }

            internal IReadOnlyList<QueryToProcess> QueriesToProcess { get; }

            internal IReadOnlyList<CommandToProcess> CommandsToProcess { get; }

            internal class QueryToProcess
            {
                public QueryToProcess(string query, string queryNamespace, string path)
                {
                    this.Query = query;
                    this.QueryNamespace = queryNamespace;
                    this.Path = path;
                }

                internal string Query { get; }

                internal string QueryNamespace { get; }

                internal string Path { get; }
            }

            internal class CommandToProcess
            {
                public CommandToProcess(string command, string commandNamespace, string path)
                {
                    this.Command = command;
                    this.CommandNamespace = commandNamespace;
                    this.Path = path;
                }

                internal string Command { get; }

                internal string CommandNamespace { get; }

                internal string Path { get; }
            }
        }
    }
}
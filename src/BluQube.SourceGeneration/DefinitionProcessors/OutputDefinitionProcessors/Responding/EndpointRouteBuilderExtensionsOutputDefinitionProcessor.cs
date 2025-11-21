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
            sb.AppendLine("using Microsoft.AspNetCore.Builder;");
            sb.AppendLine("using Microsoft.AspNetCore.Http;");
            sb.AppendLine("using Microsoft.AspNetCore.Routing;");

            sb.AppendLine($@"namespace {data.Namespace};
");

            sb.AppendLine($@"internal static class EndpointRouteBuilderExtensions
{{
     internal static IEndpointRouteBuilder AddBluQubeApi(this IEndpointRouteBuilder endpointRouteBuilder)
     {{");
            foreach (var queryToProcess in data.QueriesToProcess)
            {
                var isGet = queryToProcess.HttpMethod.Equals("GET", System.StringComparison.OrdinalIgnoreCase);
                if (isGet)
                {
                    // For GET we accept the HttpRequest and build the query object from query parameters
                    sb.AppendLine($"        endpointRouteBuilder.MapGet(\"{queryToProcess.Path}\", async (IQuerier querier, HttpRequest req) => {{");
                    sb.AppendLine("             var dict = req.Query.ToDictionary(k => k.Key, v => (object)v.Value.ToString());");
                    sb.AppendLine($"             var json = System.Text.Json.JsonSerializer.Serialize(dict);");
                    sb.AppendLine($"             var query = System.Text.Json.JsonSerializer.Deserialize<{queryToProcess.QueryNamespace}.{queryToProcess.Query}>(json, new System.Text.Json.JsonSerializerOptions {{ PropertyNameCaseInsensitive = true }});");
                    sb.AppendLine("             var data = await querier.Send(query!);");
                    sb.AppendLine("             return Results.Json(data);");
                    sb.AppendLine("         });");
                }
                else
                {
                    sb.AppendLine($"        endpointRouteBuilder.MapPost(\"{queryToProcess.Path}\", async (IQuerier querier, {queryToProcess.QueryNamespace}.{queryToProcess.Query} query) => {{");
                    sb.AppendLine("             var data = await querier.Send(query);");
                    sb.AppendLine("             return Results.Json(data);");
                    sb.AppendLine("         });");
                }
            }

            foreach (var commandToProcess in data.CommandsToProcess.GroupBy(x => x.Path.ToLower())
                         .Select(g => g.First())
                         .ToList())
            {
                sb.AppendLine($"        endpointRouteBuilder.MapPost(\"{commandToProcess.Path}\", async (ICommander commander, {commandToProcess.CommandNamespace}.{commandToProcess.Command} command) => {{");
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
                public QueryToProcess(string query, string queryNamespace, string path, string httpMethod)
                {
                    this.Query = query;
                    this.QueryNamespace = queryNamespace;
                    this.Path = path;
                    this.HttpMethod = httpMethod;
                }

                internal string Query { get; }

                internal string QueryNamespace { get; }

                internal string Path { get; }

                internal string HttpMethod { get; }
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
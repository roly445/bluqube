using System.Collections.Generic;
using System.Linq;
using System.Text;
using BluQube.CodeGenerators.Contracts;
using BluQube.SourceGeneration.Utilities;

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

            var shimRecords = new StringBuilder();
            var emittedShimNames = new HashSet<string>();

            foreach (var queryToProcess in data.QueriesToProcess)
            {
                var routeParams = PathTemplateParser.ExtractRouteParameters(queryToProcess.Path);
                var httpMethod = queryToProcess.Method ?? "POST";
                var mapMethod = httpMethod.Equals("GET", System.StringComparison.OrdinalIgnoreCase) ? "MapGet" : "MapPost";

                if (routeParams.Any())
                {
                    var routeParamList = new List<string>();
                    var ctorParamList = new List<string>();
                    var bodyShimProps = new List<string>();

                    foreach (var routeParam in routeParams)
                    {
                        var matchedParam = queryToProcess.AllParameters?.FirstOrDefault(p =>
                            string.Equals(p.Name, routeParam, System.StringComparison.OrdinalIgnoreCase));
                        if (matchedParam != null)
                        {
                            routeParamList.Add($"[Microsoft.AspNetCore.Mvc.FromRoute] {matchedParam.TypeName} {matchedParam.Name.ToLowerInvariant()}");
                            ctorParamList.Add(matchedParam.Name.ToLowerInvariant());
                        }
                    }

                    var nonRouteParams = queryToProcess.AllParameters?
                        .Where(p => !routeParams.Any(rp =>
                            string.Equals(rp, p.Name, System.StringComparison.OrdinalIgnoreCase)))
                        .ToList() ?? new List<RecordParameterInfo>();

                    if (nonRouteParams.Any())
                    {
                        var shimName = $"{queryToProcess.Query}Params";
                        foreach (var param in nonRouteParams)
                        {
                            var fromAttribute = httpMethod.Equals("GET", System.StringComparison.OrdinalIgnoreCase)
                                ? "[property: Microsoft.AspNetCore.Mvc.FromQuery] "
                                : string.Empty;
                            bodyShimProps.Add($"{fromAttribute}{param.TypeName} {param.Name}");
                            ctorParamList.Add($"{queryToProcess.Query.ToLowerInvariant()}Params.{param.Name}");
                        }

                        if (emittedShimNames.Add(shimName))
                        {
                            shimRecords.AppendLine($"     internal record {shimName}({string.Join(", ", bodyShimProps)});");
                        }

                        sb.AppendLine($"        endpointRouteBuilder.{mapMethod}(\"{queryToProcess.Path}\", async (IQueryRunner queryRunner, {string.Join(", ", routeParamList)}, [Microsoft.AspNetCore.Http.AsParameters] {shimName} {queryToProcess.Query.ToLowerInvariant()}Params) => {{");
                    }
                    else
                    {
                        sb.AppendLine($"        endpointRouteBuilder.{mapMethod}(\"{queryToProcess.Path}\", async (IQueryRunner queryRunner, {string.Join(", ", routeParamList)}) => {{");
                    }

                    sb.AppendLine($"             var query = new {queryToProcess.QueryNamespace}.{queryToProcess.Query}({string.Join(", ", ctorParamList)});");
                    sb.AppendLine("             var data = await queryRunner.Send(query);");
                    sb.AppendLine("             return Results.Json(data);");
                    sb.AppendLine("         });");
                }
                else
                {
                    sb.AppendLine($"        endpointRouteBuilder.{mapMethod}(\"{queryToProcess.Path}\", async (IQueryRunner queryRunner, {queryToProcess.QueryNamespace}.{queryToProcess.Query} query) => {{");
                    sb.AppendLine("             var data = await queryRunner.Send(query);");
                    sb.AppendLine("             return Results.Json(data);");
                    sb.AppendLine("         });");
                }
            }

            foreach (var commandToProcess in data.CommandsToProcess.GroupBy(x => x.Path.ToLower())
                         .Select(g => g.First())
                         .ToList())
            {
                var routeParams = PathTemplateParser.ExtractRouteParameters(commandToProcess.Path);

                if (routeParams.Any())
                {
                    var routeParamList = new List<string>();
                    var ctorParamList = new List<string>();
                    var bodyShimProps = new List<string>();

                    foreach (var routeParam in routeParams)
                    {
                        var matchedParam = commandToProcess.AllParameters?.FirstOrDefault(p =>
                            string.Equals(p.Name, routeParam, System.StringComparison.OrdinalIgnoreCase));
                        if (matchedParam != null)
                        {
                            routeParamList.Add($"[Microsoft.AspNetCore.Mvc.FromRoute] {matchedParam.TypeName} {matchedParam.Name.ToLowerInvariant()}");
                            ctorParamList.Add(matchedParam.Name.ToLowerInvariant());
                        }
                    }

                    var nonRouteParams = commandToProcess.AllParameters?
                        .Where(p => !routeParams.Any(rp =>
                            string.Equals(rp, p.Name, System.StringComparison.OrdinalIgnoreCase)))
                        .ToList() ?? new List<RecordParameterInfo>();

                    if (nonRouteParams.Any())
                    {
                        var shimName = $"{commandToProcess.Command}Body";
                        foreach (var param in nonRouteParams)
                        {
                            bodyShimProps.Add($"{param.TypeName} {param.Name}");
                            ctorParamList.Add($"body.{param.Name}");
                        }

                        if (emittedShimNames.Add(shimName))
                        {
                            shimRecords.AppendLine($"     internal record {shimName}({string.Join(", ", bodyShimProps)});");
                        }

                        sb.AppendLine($"        endpointRouteBuilder.MapPost(\"{commandToProcess.Path}\", async (ICommandRunner commandRunner, {string.Join(", ", routeParamList)}, {shimName} body) => {{");
                    }
                    else
                    {
                        sb.AppendLine($"        endpointRouteBuilder.MapPost(\"{commandToProcess.Path}\", async (ICommandRunner commandRunner, {string.Join(", ", routeParamList)}) => {{");
                    }

                    sb.AppendLine($"             var command = new {commandToProcess.CommandNamespace}.{commandToProcess.Command}({string.Join(", ", ctorParamList)});");
                    sb.AppendLine("             var data = await commandRunner.Send(command);");
                    sb.AppendLine("             return Results.Json(data);");
                    sb.AppendLine("         });");
                }
                else
                {
                    sb.AppendLine($"        endpointRouteBuilder.MapPost(\"{commandToProcess.Path}\", async (ICommandRunner commandRunner, {commandToProcess.CommandNamespace}.{commandToProcess.Command} command) => {{");
                    sb.AppendLine("             var data = await commandRunner.Send(command);");
                    sb.AppendLine("             return Results.Json(data);");
                    sb.AppendLine("         });");
                }
            }

            sb.AppendLine(@"        return endpointRouteBuilder;
     }");
            sb.Append(shimRecords.ToString());
            sb.AppendLine("}");
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
                public QueryToProcess(
                    string query,
                    string queryNamespace,
                    string path,
                    string? method = null,
                    IReadOnlyList<RecordParameterInfo>? allParameters = null)
                {
                    this.Query = query;
                    this.QueryNamespace = queryNamespace;
                    this.Path = path;
                    this.Method = method ?? "POST";
                    this.AllParameters = allParameters;
                }

                internal string Query { get; }

                internal string QueryNamespace { get; }

                internal string Path { get; }

                internal string Method { get; }

                internal IReadOnlyList<RecordParameterInfo>? AllParameters { get; }
            }

            internal class CommandToProcess
            {
                public CommandToProcess(
                    string command,
                    string commandNamespace,
                    string path,
                    IReadOnlyList<RecordParameterInfo>? allParameters = null)
                {
                    this.Command = command;
                    this.CommandNamespace = commandNamespace;
                    this.Path = path;
                    this.AllParameters = allParameters;
                }

                internal string Command { get; }

                internal string CommandNamespace { get; }

                internal string Path { get; }

                internal IReadOnlyList<RecordParameterInfo>? AllParameters { get; }
            }
        }
    }
}
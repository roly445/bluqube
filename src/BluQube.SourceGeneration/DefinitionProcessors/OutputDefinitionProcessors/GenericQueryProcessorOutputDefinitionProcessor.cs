using System.Collections.Generic;
using System.Linq;
using System.Text;
using BluQube.CodeGenerators.Contracts;
using BluQube.SourceGeneration.Utilities;

namespace BluQube.SourceGeneration.DefinitionProcessors.OutputDefinitionProcessors
{
    internal class GenericQueryProcessorOutputDefinitionProcessor : IOutputDefinitionProcessor<GenericQueryProcessorOutputDefinitionProcessor.OutputDefinition>
    {
        public string Process(OutputDefinition data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("#nullable enable");
            sb.AppendLine($@"using System.Text.Json.Serialization;
using BluQube.Queries;
using Microsoft.Extensions.Logging;");
            sb.AppendLine($"using {data.QueryNamespace};");
            if (data.QueryNamespace != data.QueryResultNamespace)
            {
                sb.AppendLine($"using {data.QueryResultNamespace};");
            }

            sb.AppendLine($@"
namespace {data.QueryNamespace};

internal class Generic{data.QueryName}Processor(
    IHttpClientFactory httpClientFactory,
    QueryResultConverter<{data.QueryResult}> jsonConverter,
    ILogger<GenericQueryProcessor<{data.QueryName}, {data.QueryResult}>> logger)
        : GenericQueryProcessor<{data.QueryName}, {data.QueryResult}>(httpClientFactory, jsonConverter, logger)
{{
    protected override string Path => {data.Path};
    protected override string HttpMethod => ""{data.Method}"";");

            if (data.RouteParameters != null && data.RouteParameters.Any())
            {
                var pathWithInterpolation = data.Path.Trim('"');
                var nonRouteParams = data.AllParameters?
                    .Where(p => !data.RouteParameters.Any(rp =>
                        string.Equals(rp, p.Name, System.StringComparison.OrdinalIgnoreCase)))
                    .ToList() ?? new List<RecordParameterInfo>();

                foreach (var routeParam in data.RouteParameters)
                {
                    var matchedParam = data.AllParameters?.FirstOrDefault(p =>
                        string.Equals(p.Name, routeParam, System.StringComparison.OrdinalIgnoreCase));
                    if (matchedParam != null)
                    {
                        pathWithInterpolation = pathWithInterpolation.Replace(
                            $"{{{routeParam}}}",
                            $"{{System.Uri.EscapeDataString(request.{matchedParam.Name}.ToString())}}");
                    }
                }

                if (data.Method.Equals("GET", System.StringComparison.OrdinalIgnoreCase) && nonRouteParams.Any())
                {
                    var queryStringParts = nonRouteParams.Select(param => $"(request.{param.Name} != null ? $\"{param.Name}={{System.Uri.EscapeDataString(request.{param.Name}.ToString() ?? string.Empty)}}\" : string.Empty)").ToList();

                    var queryStringJoin = string.Join(", ", queryStringParts);
                    sb.AppendLine($@"
    protected override string BuildPath({data.QueryName} request)
    {{
        var queryString = string.Join(""&"", new[] {{ {queryStringJoin} }}.Where(x => !string.IsNullOrEmpty(x)));
        var path = $""{pathWithInterpolation}"";
        return string.IsNullOrEmpty(queryString) ? path : $""{{path}}?{{queryString}}"";
    }}");
                }
                else
                {
                    sb.AppendLine($@"
    protected override string BuildPath({data.QueryName} request)
    {{
        return $""{pathWithInterpolation}"";
    }}");
                }
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        internal class OutputDefinition : IOutputDefinition
        {
            internal OutputDefinition(
                string queryNamespace,
                string queryResultNamespace,
                string queryName,
                string queryResult,
                string path,
                string method = "POST",
                IReadOnlyList<string>? routeParameters = null,
                IReadOnlyList<RecordParameterInfo>? allParameters = null)
            {
                this.QueryNamespace = queryNamespace;
                this.QueryResultNamespace = queryResultNamespace;
                this.QueryName = queryName;
                this.QueryResult = queryResult;
                this.Path = path;
                this.Method = method;
                this.RouteParameters = routeParameters;
                this.AllParameters = allParameters;
            }

            public string QueryNamespace { get; }

            public string QueryResultNamespace { get; }

            public string QueryName { get; }

            public string QueryResult { get; }

            public string Path { get; }

            public string Method { get; }

            public IReadOnlyList<string>? RouteParameters { get; }

            public IReadOnlyList<RecordParameterInfo>? AllParameters { get; }
        }
    }
}
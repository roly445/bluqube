using System.Collections.Generic;
using System.Linq;
using System.Text;
using BluQube.CodeGenerators.Contracts;
using BluQube.SourceGeneration.Utilities;

namespace BluQube.SourceGeneration.DefinitionProcessors.OutputDefinitionProcessors
{
    internal class GenericCommandOfTHandlerOutputDefinitionProcessor
        : IOutputDefinitionProcessor<GenericCommandOfTHandlerOutputDefinitionProcessor.OutputDefinition>
    {
        public string Process(OutputDefinition data)
        {
            var sb = new StringBuilder();
            sb.AppendLine($@"using System.Text.Json.Serialization;
using BluQube.Commands;
using Microsoft.Extensions.Logging;");
            sb.AppendLine($@"using {data.CommandNamespace};");
            if (data.CommandNamespace != data.CommandResultNamespace)
            {
                sb.AppendLine($@"using {data.CommandResultNamespace};");
            }

            sb.AppendLine($@"
namespace {data.CommandNamespace};

internal class {data.CommandName}GenericHandler(
IHttpClientFactory httpClientFactory, CommandResultConverter<{data.CommandResultName}> jsonConverter, ILogger<GenericCommandHandler<{data.CommandName}, {data.CommandResultName}>> logger)
        : GenericCommandHandler<{data.CommandName}, {data.CommandResultName}>(httpClientFactory, jsonConverter, logger)
{{
    protected override string Path => {data.Path};");

            if (data.RouteParameters != null && data.RouteParameters.Any())
            {
                var pathWithInterpolation = data.Path.Trim('"');
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

                sb.AppendLine($@"
    protected override string BuildPath({data.CommandName} request)
    {{
        return $""{pathWithInterpolation}"";
    }}");
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        internal class OutputDefinition : IOutputDefinition
        {
            public OutputDefinition(
                string commandNamespace,
                string commandResultNamespace,
                string commandName,
                string commandResultName,
                string path,
                IReadOnlyList<string>? routeParameters = null,
                IReadOnlyList<RecordParameterInfo>? allParameters = null)
            {
                this.CommandNamespace = commandNamespace;
                this.CommandResultNamespace = commandResultNamespace;
                this.CommandName = commandName;
                this.CommandResultName = commandResultName;
                this.Path = path;
                this.RouteParameters = routeParameters;
                this.AllParameters = allParameters;
            }

            public string CommandNamespace { get; }

            public string CommandResultNamespace { get; }

            public string CommandName { get; }

            public string CommandResultName { get; }

            public string Path { get; }

            public IReadOnlyList<string>? RouteParameters { get; }

            public IReadOnlyList<RecordParameterInfo>? AllParameters { get; }
        }
    }
}
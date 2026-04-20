using System.Collections.Generic;
using System.Linq;
using BluQube.CodeGenerators.Contracts;
using BluQube.SourceGeneration.Utilities;

namespace BluQube.SourceGeneration.DefinitionProcessors.OutputDefinitionProcessors
{
    internal class GenericCommandHandlerOutputDefinitionProcessor
        : IOutputDefinitionProcessor<GenericCommandHandlerOutputDefinitionProcessor.OutputDefinition>
    {
        public string Process(OutputDefinition data)
        {
            var buildPathOverride = string.Empty;
            if (data.RouteParameters != null && data.RouteParameters.Any())
            {
                var pathWithInterpolation = data.Path.Trim('"');
                foreach (var routeParam in data.RouteParameters)
                {
                    var matchedParam = data.AllParameters.FirstOrDefault(p =>
                        string.Equals(p.Name, routeParam, System.StringComparison.OrdinalIgnoreCase));
                    if (matchedParam != null)
                    {
                        pathWithInterpolation = pathWithInterpolation.Replace(
                            $"{{{routeParam}}}",
                            $"{{System.Uri.EscapeDataString(request.{matchedParam.Name}.ToString())}}");
                    }
                }

                buildPathOverride = $@"
    protected override string BuildPath({data.CommandName} request)
    {{
        return $""{pathWithInterpolation}"";
    }}";
            }

            return $@"using System.Text.Json.Serialization;
using BluQube.Commands;
using Microsoft.Extensions.Logging;
using {data.CommandNamespace};

namespace {data.CommandNamespace};

internal class {data.CommandName}GenericHandler(
    IHttpClientFactory httpClientFactory, CommandResultConverter jsonConverter, ILogger<GenericCommandHandler<{data.CommandName}>> logger)
        : GenericCommandHandler<{data.CommandName}>(httpClientFactory, jsonConverter, logger)
{{
    protected override string Path => {data.Path};{buildPathOverride}
}}";
        }

        public class OutputDefinition : IOutputDefinition
        {
            public OutputDefinition(
                string commandNamespace,
                string commandName,
                string path,
                IReadOnlyList<string>? routeParameters = null,
                IReadOnlyList<RecordParameterInfo>? allParameters = null)
            {
                this.CommandNamespace = commandNamespace;
                this.CommandName = commandName;
                this.Path = path;
                this.RouteParameters = routeParameters;
                this.AllParameters = allParameters;
            }

            public string CommandNamespace { get; }

            public string CommandName { get; }

            public string Path { get; }

            public IReadOnlyList<string>? RouteParameters { get; }

            public IReadOnlyList<RecordParameterInfo>? AllParameters { get; }
        }
    }
}
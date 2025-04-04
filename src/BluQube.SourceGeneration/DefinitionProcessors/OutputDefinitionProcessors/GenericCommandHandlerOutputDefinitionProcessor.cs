using BluQube.CodeGenerators.Contracts;

namespace BluQube.SourceGeneration.DefinitionProcessors.OutputDefinitionProcessors
{
    internal class GenericCommandHandlerOutputDefinitionProcessor
        : IOutputDefinitionProcessor<GenericCommandHandlerOutputDefinitionProcessor.OutputDefinition>
    {
        public string Process(OutputDefinition data)
        {
            return $@"using System.Text.Json.Serialization;
using BluQube.Commands;
using Microsoft.Extensions.Logging;
using {data.CommandNamespace};

namespace {data.Namespace};

internal class {data.CommandName}GenericHandler(
    IHttpClientFactory httpClientFactory, CommandResultConverter jsonConverter, ILogger<GenericCommandHandler<{data.CommandName}>> logger)
        : GenericCommandHandler<{data.CommandName}>(httpClientFactory, jsonConverter, logger)
{{
    protected override string Path => {data.Path};
}}";
        }

        public class OutputDefinition : IOutputDefinition
        {
            public OutputDefinition(string commandNamespace, string ns, string commandName, string path)
            {
                this.CommandNamespace = commandNamespace;
                this.Namespace = ns;
                this.CommandName = commandName;
                this.Path = path;
            }

            public string CommandNamespace { get; }

            public string Namespace { get; }

            public string CommandName { get; }

            public string Path { get; }
        }
    }
}
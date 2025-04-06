using System.Text;
using BluQube.CodeGenerators.Contracts;

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
namespace {data.Namespace};

internal class {data.CommandName}GenericHandler(
IHttpClientFactory httpClientFactory, CommandResultConverter<{data.CommandResultName}> jsonConverter, ILogger<GenericCommandHandler<{data.CommandName}, {data.CommandResultName}>> logger)
        : GenericCommandHandler<{data.CommandName}, {data.CommandResultName}>(httpClientFactory, jsonConverter, logger)
{{
    protected override string Path => {data.Path};
}}");
            return sb.ToString();
        }

        internal class OutputDefinition : IOutputDefinition
        {
            public OutputDefinition(string commandNamespace, string commandResultNamespace, string ns, string commandName, string commandResultName, string path)
            {
                this.CommandNamespace = commandNamespace;
                this.CommandResultNamespace = commandResultNamespace;
                this.Namespace = ns;
                this.CommandName = commandName;
                this.CommandResultName = commandResultName;
                this.Path = path;
            }

            public string CommandNamespace { get; }

            public string CommandResultNamespace { get; }

            public string Namespace { get; }

            public string CommandName { get; }

            public string CommandResultName { get; }

            public string Path { get; }
        }
    }
}
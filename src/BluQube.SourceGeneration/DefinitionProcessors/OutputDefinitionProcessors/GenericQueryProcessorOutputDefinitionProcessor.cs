using System.Text;
using BluQube.CodeGenerators.Contracts;

namespace BluQube.SourceGeneration.DefinitionProcessors.OutputDefinitionProcessors
{
    internal class GenericQueryProcessorOutputDefinitionProcessor : IOutputDefinitionProcessor<GenericQueryProcessorOutputDefinitionProcessor.OutputDefinition>
    {
        public string Process(OutputDefinition data)
        {
            var sb = new StringBuilder();
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
    protected override string HttpMethod => ""{data.HttpMethod}"";
}}");
            return sb.ToString();
        }

        internal class OutputDefinition : IOutputDefinition
        {
            internal OutputDefinition(string queryNamespace, string queryResultNamespace, string queryName, string queryResult, string path, string httpMethod = "GET")
            {
                this.QueryNamespace = queryNamespace;
                this.QueryResultNamespace = queryResultNamespace;
                this.QueryName = queryName;
                this.QueryResult = queryResult;
                this.Path = path;
                this.HttpMethod = httpMethod;
            }

            public string QueryNamespace { get; }

            public string QueryResultNamespace { get; }

            public string QueryName { get; }

            public string QueryResult { get; }

            public string Path { get; }

            public string HttpMethod { get; }
        }
    }
}
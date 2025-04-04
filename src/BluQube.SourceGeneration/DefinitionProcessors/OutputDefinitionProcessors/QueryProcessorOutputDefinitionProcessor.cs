using BluQube.CodeGenerators.Contracts;

namespace BluQube.SourceGeneration.DefinitionProcessors.OutputDefinitionProcessors
{
    internal class QueryProcessorOutputDefinitionProcessor : IOutputDefinitionProcessor<QueryProcessorOutputDefinitionProcessor.OutputDefinition>
    {
        public string Process(OutputDefinition data)
        {
            return $@"using System.Text.Json.Serialization;
using BluQube.Queries;
using Microsoft.Extensions.Logging;
using {data.QueryNamespace};
using {data.QueryResultNamespace};

namespace {data.Namespace};

internal class {data.QueryName}Processor(
    IHttpClientFactory httpClientFactory,
    QueryResultConverter<{data.QueryResult}> jsonConverter,
    ILogger<GenericQueryProcessor<{data.QueryName}, {data.QueryResult}>> logger)
        : GenericQueryProcessor<{data.QueryName}, {data.QueryResult}>(httpClientFactory, jsonConverter, logger)
{{
    protected override string Path => {data.Path};
}}";
        }

        internal class OutputDefinition : IOutputDefinition
        {
            internal OutputDefinition(string queryNamespace, string queryResultNamespace, string ns, string queryName, string queryResult, string path)
            {
                this.QueryNamespace = queryNamespace;
                this.QueryResultNamespace = queryResultNamespace;
                this.Namespace = ns;
                this.QueryName = queryName;
                this.QueryResult = queryResult;
                this.Path = path;
            }

            public string QueryNamespace { get; }

            public string QueryResultNamespace { get; }

            public string Namespace { get; }

            public string QueryName { get; }

            public string QueryResult { get; }

            public string Path { get; }
        }
    }
}
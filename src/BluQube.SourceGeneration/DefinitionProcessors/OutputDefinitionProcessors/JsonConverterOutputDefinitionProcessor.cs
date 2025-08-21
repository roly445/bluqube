using System;
using BluQube.CodeGenerators.Contracts;

namespace BluQube.SourceGeneration.DefinitionProcessors.OutputDefinitionProcessors
{
    internal class JsonConverterOutputDefinitionProcessor : IOutputDefinitionProcessor<JsonConverterOutputDefinitionProcessor.OutputDefinition>
    {
        public string Process(OutputDefinition data)
        {
            return $@"using System.Text.Json.Serialization;
using {GetUsing(data.OutputType)};
using Microsoft.Extensions.Logging;
using {data.QueryResultNamespace};

namespace {data.QueryResultNamespace};

public class {data.QueryResult}Converter : {GetConverter(data.OutputType)}<{data.QueryResult}>;";
        }

        private static string GetUsing(OutputDefinition.Type outputType)
        {
            return outputType switch
            {
                OutputDefinition.Type.QueryResult => "BluQube.Queries",
                OutputDefinition.Type.CommandResult => "BluQube.Commands",
                _ => throw new ArgumentOutOfRangeException(nameof(outputType), outputType, null),
            };
        }

        private static string GetConverter(OutputDefinition.Type outputType)
        {
            return outputType switch
            {
                OutputDefinition.Type.QueryResult => "QueryResultConverter",
                OutputDefinition.Type.CommandResult => "CommandResultConverter",
                _ => throw new ArgumentOutOfRangeException(nameof(outputType), outputType, null),
            };
        }

        internal class OutputDefinition : IOutputDefinition
        {
            internal OutputDefinition(string queryResultNamespace, string queryResult, Type outputType)
            {
                this.QueryResultNamespace = queryResultNamespace;
                this.QueryResult = queryResult;
                this.OutputType = outputType;
            }

            internal enum Type
            {
                None,
                QueryResult,
                CommandResult,
                BasicCommandResult,
            }

            public string QueryResultNamespace { get; }

            public string QueryResult { get; }

            public Type OutputType { get; }
        }
    }
}
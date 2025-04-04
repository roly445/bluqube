using System.Collections.Generic;
using System.Linq;
using System.Text;
using BluQube.CodeGenerators.Contracts;

namespace BluQube.SourceGeneration.DefinitionProcessors.OutputDefinitionProcessors.Requesting
{
    internal class ServiceCollectionExtensionsOutputDefinitionProcessor : IOutputDefinitionProcessor<ServiceCollectionExtensionsOutputDefinitionProcessor.OutputDefinition>
    {
        public string Process(OutputDefinition data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using BluQube.Queries;");
            sb.AppendLine("using BluQube.Commands;");
            foreach (var dataJsonConverterOutputDefinition in data.JsonConverterOutputDefinitions.Select(x => x.QueryResultNamespace).Distinct())
            {
                sb.AppendLine($"using {dataJsonConverterOutputDefinition};");
            }

            sb.AppendLine($@"
namespace {data.Namespace};

internal static class ServiceCollectionExtensions
{{
     internal static IServiceCollection AddBluQubeRequesters(this IServiceCollection services)
     {{");
            foreach (var dataJsonConverterOutputDefinition in data.JsonConverterOutputDefinitions)
            {
                if (dataJsonConverterOutputDefinition.OutputType == JsonConverterOutputDefinitionProcessor.OutputDefinition.Type.QueryResult)
                {
                    sb.AppendLine($@"           services
                     .AddTransient<QueryResultConverter<{dataJsonConverterOutputDefinition.QueryResult}>, {dataJsonConverterOutputDefinition.QueryResult}Converter>();");
                }
                else if (dataJsonConverterOutputDefinition.OutputType ==
                         JsonConverterOutputDefinitionProcessor.OutputDefinition.Type.CommandResult)
                {
                    sb.AppendLine($@"           services
                     .AddTransient<CommandResultConverter<{dataJsonConverterOutputDefinition.QueryResult}>, {dataJsonConverterOutputDefinition.QueryResult}Converter>();");
                }
            }

            sb.AppendLine(@"           return services;
     }
}");
            return sb.ToString();
        }

        internal class OutputDefinition : IOutputDefinition
        {
            public OutputDefinition(string ns, IReadOnlyList<JsonConverterOutputDefinitionProcessor.OutputDefinition> jsonConverterOutputDefinitions)
            {
                this.Namespace = ns;
                this.JsonConverterOutputDefinitions = jsonConverterOutputDefinitions;
            }

            public string Namespace { get; }

            public IReadOnlyList<JsonConverterOutputDefinitionProcessor.OutputDefinition> JsonConverterOutputDefinitions { get; }
        }
    }
}
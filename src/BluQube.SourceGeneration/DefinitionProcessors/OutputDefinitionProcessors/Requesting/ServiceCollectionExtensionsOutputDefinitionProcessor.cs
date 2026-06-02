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
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sb.AppendLine("using BluQube.Queries;");
            sb.AppendLine("using BluQube.Commands;");
            foreach (var ns in data.JsonConverterOutputDefinitions.Select(x => x.QueryResultNamespace)
                         .Concat(data.QueryProcessorOutputDefinitions.Select(x => x.QueryNamespace))
                         .Concat(data.GenericCommandHandlerOutputDefinitions.Select(x => x.CommandNamespace))
                         .Concat(data.GenericCommandOfTHandlerOutputDefinitions.Select(x => x.CommandNamespace))
                         .Distinct())
            {
                sb.AppendLine($"using {ns};");
            }

            sb.AppendLine($@"
namespace {data.Namespace};

internal static class ServiceCollectionExtensions
{{
     internal static IServiceCollection AddBluQubeRequesters(this IServiceCollection services)
     {{");
            sb.AppendLine("           services.AddBluQubeMediator();");

            foreach (var queryProcessorOutputDefinition in data.QueryProcessorOutputDefinitions)
            {
                sb.AppendLine($@"           services
                     .AddTransient<IQueryProcessor<{queryProcessorOutputDefinition.QueryName}, {queryProcessorOutputDefinition.QueryResult}>, Generic{queryProcessorOutputDefinition.QueryName}Processor>();");
            }

            foreach (var registration in data.GenericCommandHandlerOutputDefinitions.Select(
                         genericCommandHandlerOutputDefinition =>
                             $@"           services
                     .AddTransient<ICommandHandler<{genericCommandHandlerOutputDefinition.CommandName}>, {genericCommandHandlerOutputDefinition.CommandName}GenericHandler>();"))
            {
                sb.AppendLine(registration);
            }

            foreach (var genericCommandOfTHandlerOutputDefinition in data.GenericCommandOfTHandlerOutputDefinitions)
            {
                sb.AppendLine($@"           services
                     .AddTransient<ICommandHandler<{genericCommandOfTHandlerOutputDefinition.CommandName}, {genericCommandOfTHandlerOutputDefinition.CommandResultName}>, {genericCommandOfTHandlerOutputDefinition.CommandName}GenericHandler>();");
            }

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
            public OutputDefinition(
                string ns,
                IReadOnlyList<JsonConverterOutputDefinitionProcessor.OutputDefinition> jsonConverterOutputDefinitions,
                IReadOnlyList<GenericQueryProcessorOutputDefinitionProcessor.OutputDefinition> queryProcessorOutputDefinitions,
                IReadOnlyList<GenericCommandHandlerOutputDefinitionProcessor.OutputDefinition> genericCommandHandlerOutputDefinitions,
                IReadOnlyList<GenericCommandOfTHandlerOutputDefinitionProcessor.OutputDefinition> genericCommandOfTHandlerOutputDefinitions)
            {
                this.Namespace = ns;
                this.JsonConverterOutputDefinitions = jsonConverterOutputDefinitions;
                this.QueryProcessorOutputDefinitions = queryProcessorOutputDefinitions;
                this.GenericCommandHandlerOutputDefinitions = genericCommandHandlerOutputDefinitions;
                this.GenericCommandOfTHandlerOutputDefinitions = genericCommandOfTHandlerOutputDefinitions;
            }

            public string Namespace { get; }

            public IReadOnlyList<JsonConverterOutputDefinitionProcessor.OutputDefinition> JsonConverterOutputDefinitions { get; }

            public IReadOnlyList<GenericQueryProcessorOutputDefinitionProcessor.OutputDefinition> QueryProcessorOutputDefinitions { get; }

            public IReadOnlyList<GenericCommandHandlerOutputDefinitionProcessor.OutputDefinition> GenericCommandHandlerOutputDefinitions { get; }

            public IReadOnlyList<GenericCommandOfTHandlerOutputDefinitionProcessor.OutputDefinition> GenericCommandOfTHandlerOutputDefinitions { get; }
        }
    }
}
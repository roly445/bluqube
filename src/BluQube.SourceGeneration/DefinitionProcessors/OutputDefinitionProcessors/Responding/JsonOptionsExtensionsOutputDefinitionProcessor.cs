using System.Collections.Generic;
using System.Linq;
using System.Text;
using BluQube.CodeGenerators.Contracts;

namespace BluQube.SourceGeneration.DefinitionProcessors.OutputDefinitionProcessors.Responding
{
    internal class JsonOptionsExtensionsOutputDefinitionProcessor : IOutputDefinitionProcessor<JsonOptionsExtensionsOutputDefinitionProcessor.OutputDefinition>
    {
        public string Process(OutputDefinition data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using Microsoft.AspNetCore.Http.Json;");

            sb.AppendLine($@"
namespace {data.Namespace};

internal static class JsonOptionsExtensions
{{
     internal static JsonOptions AddBluQubeJsonConverters(this JsonOptions jsonOptions)
     {{");
            foreach (var jsonConverterToProcess in data.JsonConvertersToProcess.Distinct())
            {
                sb.AppendLine($"            jsonOptions.SerializerOptions.Converters.Add(new {jsonConverterToProcess.ConverterNamespace}.{jsonConverterToProcess.ConverterName}());");
            }

            sb.AppendLine(@"            return jsonOptions;
     }
}");

            return sb.ToString();
        }

        internal class OutputDefinition : IOutputDefinition
        {
            public OutputDefinition(string ns, IReadOnlyList<JsonConverterToProcess> jsonConvertersToProcess)
            {
                this.Namespace = ns;
                this.JsonConvertersToProcess = jsonConvertersToProcess;
            }

            internal string Namespace { get; }

            internal IReadOnlyList<JsonConverterToProcess> JsonConvertersToProcess { get; }

            internal class JsonConverterToProcess
            {
                public JsonConverterToProcess(string converterNamespace, string converterName)
                {
                    this.ConverterNamespace = converterNamespace;
                    this.ConverterName = converterName;
                }

                internal string ConverterNamespace { get; }

                internal string ConverterName { get; }
            }
        }
    }
}
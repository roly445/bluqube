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
                sb.AppendLine($"            jsonOptions.SerializerOptions.Converters.Add({jsonConverterToProcess.ToRegistrationExpression()});");
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
                private readonly ConverterKind _kind;

                public JsonConverterToProcess(string converterNamespace, string converterName)
                {
                    this._kind = ConverterKind.Named;
                    this.ConverterNamespace = converterNamespace;
                    this.ConverterName = converterName;
                    this.ResultNamespace = string.Empty;
                    this.ResultTypeName = string.Empty;
                }

                private JsonConverterToProcess(ConverterKind kind, string resultNamespace, string resultTypeName)
                {
                    this._kind = kind;
                    this.ConverterNamespace = string.Empty;
                    this.ConverterName = string.Empty;
                    this.ResultNamespace = resultNamespace;
                    this.ResultTypeName = resultTypeName;
                }

                private enum ConverterKind
                {
                    Named,
                    GenericCommandResult,
                }

                internal string ConverterNamespace { get; }

                internal string ConverterName { get; }

                internal string ResultNamespace { get; }

                internal string ResultTypeName { get; }

                public override bool Equals(object obj)
                {
                    if (obj is not JsonConverterToProcess other || this._kind != other._kind)
                    {
                        return false;
                    }

                    return this._kind switch
                    {
                        ConverterKind.Named =>
                            this.ConverterNamespace == other.ConverterNamespace &&
                            this.ConverterName == other.ConverterName,
                        ConverterKind.GenericCommandResult =>
                            this.ResultNamespace == other.ResultNamespace &&
                            this.ResultTypeName == other.ResultTypeName,
                        _ => false,
                    };
                }

                public override int GetHashCode()
                {
                    return this._kind switch
                    {
                        ConverterKind.Named => (this.ConverterNamespace, this.ConverterName).GetHashCode(),
                        ConverterKind.GenericCommandResult => (this._kind, this.ResultNamespace, this.ResultTypeName).GetHashCode(),
                        _ => 0,
                    };
                }

                internal static JsonConverterToProcess ForCommandResult(string resultNamespace, string resultTypeName)
                    => new JsonConverterToProcess(ConverterKind.GenericCommandResult, resultNamespace, resultTypeName);

                internal string ToRegistrationExpression()
                {
                    return this._kind switch
                    {
                        ConverterKind.Named =>
                            $"new {this.ConverterNamespace}.{this.ConverterName}()",
                        ConverterKind.GenericCommandResult =>
                            $"new BluQube.Commands.CommandResultConverter<{this.ResultNamespace}.{this.ResultTypeName}>()",
                        _ => throw new System.InvalidOperationException($"Unknown converter kind: {this._kind}"),
                    };
                }
            }
        }
    }
}
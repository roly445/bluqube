using System.Collections.Generic;
using System.Linq;
using System.Text;
using BluQube.CodeGenerators.Contracts;

namespace BluQube.SourceGeneration.DefinitionProcessors.OutputDefinitionProcessors.Responding
{
    internal class OpenApiOutputDefinitionProcessor : IOutputDefinitionProcessor<OpenApiOutputDefinitionProcessor.OutputDefinition>
    {
        public string Process(OutputDefinition data)
        {
            var sb = new StringBuilder();

            sb.AppendLine("{");
            sb.AppendLine("  \"openapi\": \"3.0.1\",");
            sb.AppendLine("  \"info\": {");
            sb.AppendLine("    \"title\": \"BluQube API\",");
            sb.AppendLine("    \"version\": \"1.0.0\",");
            sb.AppendLine("    \"description\": \"Auto-generated API from BluQube commands and queries\"");
            sb.AppendLine("  },");
            sb.AppendLine("  \"paths\": {");

            var allPaths = new List<string>();

            // Map enum value to scheme name for path references
            string securitySchemeName = data.SecurityScheme switch
            {
                0 => "Bearer",
                1 => "Cookie",
                2 => "ApiKey",
                _ => "Bearer",
            };

            // Add queries
            foreach (var query in data.QueriesToProcess)
            {
                var httpMethod = query.HttpMethod.ToLower();
                var requiresAuth = data.AuthorizationMap.ContainsKey(query.Query);
                var requirements = requiresAuth ? string.Join(", ", data.AuthorizationMap[query.Query]) : string.Empty;

                var pathJson = new StringBuilder();
                pathJson.AppendLine($"    \"/{query.Path}\": {{");
                pathJson.AppendLine($"      \"{httpMethod}\": {{");
                pathJson.AppendLine($"        \"operationId\": \"{query.QueryNamespace}.{query.Query}\",");
                pathJson.AppendLine($"        \"tags\": [\"Queries\"],");

                if (requiresAuth)
                {
                    pathJson.AppendLine($"        \"description\": \"Requires: {requirements}\",");
                    pathJson.AppendLine("        \"security\": [{ \"Bearer\": [] }],");
                }

                if (!httpMethod.Equals("get"))
                {
                    pathJson.AppendLine("        \"requestBody\": {");
                    pathJson.AppendLine("          \"required\": true,");
                    pathJson.AppendLine("          \"content\": {");
                    pathJson.AppendLine("            \"application/json\": {");
                    pathJson.AppendLine($"              \"schema\": {{ \"type\": \"object\", \"description\": \"{query.QueryNamespace}.{query.Query}\" }}");
                    pathJson.AppendLine("            }");
                    pathJson.AppendLine("          }");
                    pathJson.AppendLine("        },");
                }

                pathJson.AppendLine("        \"responses\": {");
                pathJson.AppendLine("          \"200\": {");
                pathJson.AppendLine("            \"description\": \"Success\",");
                pathJson.AppendLine("            \"content\": {");
                pathJson.AppendLine("              \"application/json\": { \"schema\": { \"type\": \"object\" } }");
                pathJson.AppendLine("            }");
                pathJson.AppendLine("          },");
                pathJson.AppendLine("          \"401\": { \"description\": \"Unauthorized\" }");
                pathJson.AppendLine("        }");
                pathJson.AppendLine("      }");
                pathJson.Append("    }");

                allPaths.Add(pathJson.ToString());
            }

            // Add commands
            foreach (var command in data.CommandsToProcess)
            {
                var requiresAuth = data.AuthorizationMap.ContainsKey(command.Command);
                var requirements = requiresAuth ? string.Join(", ", data.AuthorizationMap[command.Command]) : string.Empty;

                var pathJson = new StringBuilder();
                pathJson.AppendLine($"    \"/{command.Path}\": {{");
                pathJson.AppendLine("      \"post\": {");
                pathJson.AppendLine($"        \"operationId\": \"{command.CommandNamespace}.{command.Command}\",");
                pathJson.AppendLine($"        \"tags\": [\"Commands\"],");

                if (requiresAuth)
                {
                    pathJson.AppendLine($"        \"description\": \"Requires: {requirements}\",");
                    pathJson.AppendLine($"        \"security\": [{{ \"{securitySchemeName}\": [] }}],");
                }

                pathJson.AppendLine("        \"requestBody\": {");
                pathJson.AppendLine("          \"required\": true,");
                pathJson.AppendLine("          \"content\": {");
                pathJson.AppendLine("            \"application/json\": {");
                pathJson.AppendLine($"              \"schema\": {{ \"type\": \"object\", \"description\": \"{command.CommandNamespace}.{command.Command}\" }}");
                pathJson.AppendLine("            }");
                pathJson.AppendLine("          }");
                pathJson.AppendLine("        },");
                pathJson.AppendLine("        \"responses\": {");
                pathJson.AppendLine("          \"200\": {");
                pathJson.AppendLine("            \"description\": \"Success\",");
                pathJson.AppendLine("            \"content\": {");
                pathJson.AppendLine("              \"application/json\": { \"schema\": { \"type\": \"object\" } }");
                pathJson.AppendLine("            }");
                pathJson.AppendLine("          },");
                pathJson.AppendLine("          \"400\": { \"description\": \"Validation failed\" },");
                pathJson.AppendLine("          \"401\": { \"description\": \"Unauthorized\" }");
                pathJson.AppendLine("        }");
                pathJson.AppendLine("      }");
                pathJson.Append("    }");

                allPaths.Add(pathJson.ToString());
            }

            sb.Append(string.Join(",\n", allPaths));
            sb.AppendLine();
            sb.AppendLine("  },");

            // Add security schemes if any auth is required
            sb.AppendLine("  \"components\": {");
            sb.AppendLine("    \"securitySchemes\": {");
            if (data.AuthorizationMap.Any())
            {
                // Map enum value to scheme name: 0=Bearer, 1=Cookie, 2=ApiKey
                string schemeName;
                string schemeType;
                string schemeIn;
                string schemeNameParam;
                string schemeDescription;
                string additionalProps = string.Empty;

                switch (data.SecurityScheme)
                {
                    case 0: // Bearer
                        schemeName = "Bearer";
                        schemeType = "http";
                        schemeIn = "header";
                        schemeNameParam = "Authorization";
                        schemeDescription = "Enter your bearer token";
                        additionalProps = "        \"scheme\": \"bearer\",\n        \"bearerFormat\": \"JWT\",\n";
                        break;
                    case 1: // Cookie
                        schemeName = "Cookie";
                        schemeType = "apiKey";
                        schemeIn = "cookie";
                        schemeNameParam = ".AspNetCore.Cookies";
                        schemeDescription = "Cookie-based authentication";
                        break;
                    default: // ApiKey (case 2) or any other value
                        schemeName = "ApiKey";
                        schemeType = "apiKey";
                        schemeIn = "header";
                        schemeNameParam = "X-API-Key";
                        schemeDescription = "API Key authentication";
                        break;
                }

                sb.AppendLine($"      \"{schemeName}\": {{");
                sb.AppendLine($"        \"type\": \"{schemeType}\",");
                if (!string.IsNullOrEmpty(additionalProps))
                {
                    sb.Append(additionalProps);
                }

                sb.AppendLine($"        \"in\": \"{schemeIn}\",");
                sb.AppendLine($"        \"name\": \"{schemeNameParam}\",");
                sb.AppendLine($"        \"description\": \"{schemeDescription}\"");
                sb.AppendLine("      }");
            }

            sb.AppendLine("    }");
            sb.AppendLine("  }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        internal class OutputDefinition : IOutputDefinition
        {
            public OutputDefinition(
                string ns,
                IReadOnlyList<QueryToProcess> queriesToProcess,
                IReadOnlyList<CommandToProcess> commandsToProcess,
                Dictionary<string, List<string>> authorizationMap,
                int securityScheme)
            {
                this.Namespace = ns;
                this.QueriesToProcess = queriesToProcess;
                this.CommandsToProcess = commandsToProcess;
                this.AuthorizationMap = authorizationMap;
                this.SecurityScheme = securityScheme;
            }

            internal string Namespace { get; }

            internal IReadOnlyList<QueryToProcess> QueriesToProcess { get; }

            internal IReadOnlyList<CommandToProcess> CommandsToProcess { get; }

            internal Dictionary<string, List<string>> AuthorizationMap { get; }

            internal int SecurityScheme { get; }

            internal class QueryToProcess
            {
                public QueryToProcess(string query, string queryNamespace, string path, string httpMethod)
                {
                    this.Query = query;
                    this.QueryNamespace = queryNamespace;
                    this.Path = path;
                    this.HttpMethod = httpMethod;
                }

                internal string Query { get; }

                internal string QueryNamespace { get; }

                internal string Path { get; }

                internal string HttpMethod { get; }
            }

            internal class CommandToProcess
            {
                public CommandToProcess(string command, string commandNamespace, string path)
                {
                    this.Command = command;
                    this.CommandNamespace = commandNamespace;
                    this.Path = path;
                }

                internal string Command { get; }

                internal string CommandNamespace { get; }

                internal string Path { get; }
            }
        }
    }
}

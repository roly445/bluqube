using System.Linq;
using BluQube.CodeGenerators.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BluQube.SourceGeneration.DefinitionProcessors.InputDefinitionProcessors
{
    internal class ResponderInputDefinitionProcessor : InputDefinitionProcessor<ResponderInputDefinitionProcessor.InputDefinition>
    {
        public override bool CanProcess(SyntaxNode syntaxNode)
        {
            TypeDeclarationSyntax typeDeclarationSyntax;
            switch (syntaxNode)
            {
                case RecordDeclarationSyntax recordDeclarationSyntax:
                    typeDeclarationSyntax = recordDeclarationSyntax;
                    break;
                case ClassDeclarationSyntax classDeclarationSyntax:
                    typeDeclarationSyntax = classDeclarationSyntax;
                    break;
                default:
                    return false;
            }

            return typeDeclarationSyntax.AttributeLists
                .SelectMany(attributeList => attributeList.Attributes)
                .Any(attribute => attribute.Name.ToString() == "BluQubeResponder");
        }

        protected override InputDefinition? ProcessInternal(SyntaxNode syntaxNode)
        {
            TypeDeclarationSyntax typeDeclarationSyntax;
            switch (syntaxNode)
            {
                case RecordDeclarationSyntax recordDeclarationSyntax:
                    typeDeclarationSyntax = recordDeclarationSyntax;
                    break;
                case ClassDeclarationSyntax classDeclarationSyntax:
                    typeDeclarationSyntax = classDeclarationSyntax;
                    break;
                default:
                    return null;
            }

            var responderAttribute = typeDeclarationSyntax.AttributeLists
                .SelectMany(attributeList => attributeList.Attributes)
                .FirstOrDefault(attribute => attribute.Name.ToString() == "BluQubeResponder");

            if (responderAttribute != null)
            {
                // Extract OpenApiSecurityScheme from attribute arguments
                var securityScheme = 0; // default to Bearer (enum value 0)
                if (responderAttribute.ArgumentList?.Arguments.Count > 0)
                {
                    foreach (var arg in responderAttribute.ArgumentList.Arguments)
                    {
                        if (arg.NameEquals?.Name.ToString() == "OpenApiSecurityScheme")
                        {
                            // Parse enum: OpenApiSecurityScheme.Cookie -> "Cookie" -> 1
                            var enumValue = arg.Expression.ToString();
                            if (enumValue.Contains("."))
                            {
                                enumValue = enumValue.Split('.').Last();
                            }

                            // Map enum name to value: Bearer=0, Cookie=1, ApiKey=2
                            switch (enumValue.Trim())
                            {
                                case "Bearer":
                                    securityScheme = 0;
                                    break;
                                case "Cookie":
                                    securityScheme = 1;
                                    break;
                                case "ApiKey":
                                    securityScheme = 2;
                                    break;
                            }
                        }
                    }
                }

                return new InputDefinition(typeDeclarationSyntax, securityScheme);
            }

            return null;
        }

        internal class InputDefinition : IInputDefinition
        {
            public InputDefinition(TypeDeclarationSyntax typeWithAttribute, int openApiSecurityScheme)
            {
                this.TypeWithAttribute = typeWithAttribute;
                this.OpenApiSecurityScheme = openApiSecurityScheme;
            }

            public TypeDeclarationSyntax TypeWithAttribute { get; }

            public int OpenApiSecurityScheme { get; }
        }
    }
}
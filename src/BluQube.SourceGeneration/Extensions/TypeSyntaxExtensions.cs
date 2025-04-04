using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BluQube.SourceGeneration.Extensions
{
    internal static class TypeSyntaxExtensions
    {
        internal static string GetNamespace(this TypeSyntax typeSyntax, SemanticModel semanticModel)
        {
            var typeSymbol = semanticModel.GetSymbolInfo(typeSyntax).Symbol as INamedTypeSymbol;

            var namespaceName = string.Empty;
            if (typeSymbol != null)
            {
                namespaceName = typeSymbol.ContainingNamespace.ToDisplayString();
            }

            return namespaceName;
        }

        internal static string GetPath(this TypeSyntax typeSyntax, SemanticModel semanticModel)
        {
            var typeSymbol = semanticModel.GetSymbolInfo(typeSyntax).Symbol as INamedTypeSymbol;

            var attr = typeSymbol?.GetAttributes()
                .FirstOrDefault(att => att is { AttributeClass: { Name: "BluQubeQueryAttribute" } });

            if (attr == null)
            {
                return string.Empty;
            }

            if (attr.NamedArguments.Any(att => att.Key == "Path"))
            {
                return attr.NamedArguments
                    .FirstOrDefault(att => att.Key == "Path").Value!.Value!.ToString();
            }

            return string.Empty;
        }
    }
}
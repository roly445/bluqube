using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BluQube.SourceGeneration.Extensions
{
    internal static class ClassDeclarationSyntaxExtensions
    {
        internal static string GetNamespace(this ClassDeclarationSyntax syntaxNode)
        {
            var namespaceDeclaration = syntaxNode.AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault();
            if (namespaceDeclaration != null)
            {
                return namespaceDeclaration.Name.ToString();
            }

            var namespaceDeclaration2 = syntaxNode.AncestorsAndSelf().OfType<FileScopedNamespaceDeclarationSyntax>()
                .FirstOrDefault();
            return namespaceDeclaration2 != null ? namespaceDeclaration2.Name.ToString() : string.Empty;
        }
    }
}
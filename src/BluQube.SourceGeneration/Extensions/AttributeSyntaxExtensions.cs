using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BluQube.SourceGeneration.Extensions
{
    public static class AttributeSyntaxExtensions
    {
        public static string? GetPath(this AttributeSyntax attributeSyntax)
        {
            return attributeSyntax.ArgumentList?.Arguments
                .SingleOrDefault(x => x.NameEquals?.Name.Identifier.Text == "Path")?
                .Expression.ToString() ?? string.Empty;
        }
    }
}
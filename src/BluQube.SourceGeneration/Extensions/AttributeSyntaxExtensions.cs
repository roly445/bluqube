using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BluQube.SourceGeneration.Extensions
{
    public static class AttributeSyntaxExtensions
    {
        /// <summary>
        /// Gets the Path from the attribute.
        /// </summary>
        /// <param name="attributeSyntax">The attribute syntax to extract path from.</param>
        /// <returns>The path value or empty string if not found.</returns>
        public static string? GetPath(this AttributeSyntax attributeSyntax)
        {
            return attributeSyntax.ArgumentList?.Arguments
                .SingleOrDefault(x => x.NameEquals?.Name.Identifier.Text == "Path")?
                .Expression.ToString() ?? string.Empty;
        }
    }
}
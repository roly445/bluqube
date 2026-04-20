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

        /// <summary>
        /// Gets the Method from the attribute.
        /// </summary>
        /// <param name="attributeSyntax">The attribute syntax to extract method from.</param>
        /// <returns>The method value or "POST" if not found.</returns>
        public static string GetMethod(this AttributeSyntax attributeSyntax)
        {
            var methodArg = attributeSyntax.ArgumentList?.Arguments
                .SingleOrDefault(x => x.NameEquals?.Name.Identifier.Text == "Method")?
                .Expression.ToString();
            if (methodArg != null)
            {
                return methodArg.Trim('"');
            }

            return "POST";
        }
    }
}
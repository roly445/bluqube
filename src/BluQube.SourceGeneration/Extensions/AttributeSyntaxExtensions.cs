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
        /// Gets the HttpMethod from the attribute.
        /// </summary>
        /// <param name="attributeSyntax">The attribute syntax to extract HttpMethod from.</param>
        /// <returns>The HttpMethod value (Get or Post) or "GET" as default.</returns>
        public static string GetHttpMethod(this AttributeSyntax attributeSyntax)
        {
            var httpMethodArgument = attributeSyntax.ArgumentList?.Arguments
                .SingleOrDefault(x => x.NameEquals?.Name.Identifier.Text == "HttpMethod")?
                .Expression.ToString();

            if (string.IsNullOrEmpty(httpMethodArgument))
            {
                return "GET";
            }

            // Extract the enum value name (e.g., "HttpRequestMethod.Post" -> "Post")
            var lastDotIndex = httpMethodArgument!.LastIndexOf('.');
            var enumValue = lastDotIndex >= 0 ? httpMethodArgument.Substring(lastDotIndex + 1) : httpMethodArgument;

            // Normalize to uppercase for HTTP method string
            return enumValue.Equals("Post", System.StringComparison.OrdinalIgnoreCase) ? "POST" : "GET";
        }
    }
}
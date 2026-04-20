using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BluQube.SourceGeneration.Utilities
{
    internal static class PathTemplateParser
    {
        private static readonly Regex RouteParamRegex = new Regex(@"\{(\w+)\}", RegexOptions.Compiled);

        public static IReadOnlyList<string> ExtractRouteParameters(string path)
        {
            return RouteParamRegex.Matches(path)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .ToList();
        }
    }
}
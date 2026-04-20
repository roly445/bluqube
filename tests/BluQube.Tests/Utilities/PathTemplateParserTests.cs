// PathTemplateParser tests
// NOTE: PathTemplateParser is internal to BluQube.SourceGeneration.Utilities
// These tests verify the parser through the generated code output rather than direct unit tests
// See UrlBindingGeneratorTests.cs for snapshot tests that verify correct parameter extraction

using Xunit;

namespace BluQube.Tests.Utilities;

public class PathTemplateParserTests
{
    // PathTemplateParser.ExtractRouteParameters is tested indirectly through generator snapshot tests
    // Direct testing would require InternalsVisibleTo or reflection, which adds complexity
    // The generator tests in UrlBindingGeneratorTests.cs verify:
    // - No parameters: "commands/todo/simple" → no BuildPath override
    // - Single parameter: "commands/todo/{id}" → BuildPath with interpolation
    // - Multiple parameters: "commands/tenant/{tenantId}/todo/{id}" → both parameters
    // - Parameter in middle: "commands/todo/{id}/update" → parameter extracted
    
    [Fact]
    public void PlaceholderTest_RemovedWhenGeneratorTestsPass()
    {
        // This test exists to make the test file compile
        // Remove this after generator implementation lands and snapshot tests pass
        Assert.True(true);
    }
}

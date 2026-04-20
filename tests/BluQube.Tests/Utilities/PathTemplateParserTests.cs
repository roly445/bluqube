using System.Linq;
using BluQube.SourceGeneration.Utilities;
using Xunit;

namespace BluQube.Tests.Utilities;

public class PathTemplateParserTests
{
    [Fact]
    public void ExtractRouteParameters_EmptyString_ReturnsEmptyList()
    {
        var result = PathTemplateParser.ExtractRouteParameters("");
        
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractRouteParameters_NoParameters_ReturnsEmptyList()
    {
        var result = PathTemplateParser.ExtractRouteParameters("commands/todo/simple");
        
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractRouteParameters_SingleParameter_ReturnsParameterName()
    {
        var result = PathTemplateParser.ExtractRouteParameters("commands/todo/{id}");
        
        Assert.Single(result);
        Assert.Equal("id", result[0]);
    }

    [Fact]
    public void ExtractRouteParameters_ParameterInMiddle_ReturnsParameterName()
    {
        var result = PathTemplateParser.ExtractRouteParameters("commands/todo/{id}/update");
        
        Assert.Single(result);
        Assert.Equal("id", result[0]);
    }

    [Fact]
    public void ExtractRouteParameters_MultipleParameters_ReturnsInOrder()
    {
        var result = PathTemplateParser.ExtractRouteParameters("commands/tenant/{tenantId}/todo/{id}");
        
        Assert.Equal(2, result.Count);
        Assert.Equal("tenantId", result[0]);
        Assert.Equal("id", result[1]);
    }

    [Fact]
    public void ExtractRouteParameters_OnlyParameter_ReturnsParameterName()
    {
        var result = PathTemplateParser.ExtractRouteParameters("{root}");
        
        Assert.Single(result);
        Assert.Equal("root", result[0]);
    }

    [Fact]
    public void ExtractRouteParameters_QueryPath_ReturnsParameterName()
    {
        var result = PathTemplateParser.ExtractRouteParameters("queries/todo/{userId}");
        
        Assert.Single(result);
        Assert.Equal("userId", result[0]);
    }
}
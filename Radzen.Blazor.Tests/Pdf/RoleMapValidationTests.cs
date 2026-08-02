#nullable enable
using System;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class RoleMapValidationTests
{
    [Theory]
    [InlineData("P")]
    [InlineData("Div")]
    [InlineData("Sect")]
    [InlineData("Figure")]
    [InlineData("H3")]
    [InlineData("TOCI")]
    [InlineData("Span")]
    [InlineData("Formula")]
    [InlineData("Figure2")]
    [InlineData("p")]
    [InlineData("Paragraph")]
    [InlineData("Artifact")]
    [InlineData("H7")]
    public void StandardAndNonStandardStructureTypesAreAcceptedAsChainLinks(string structureType)
    {
        var map = new RoleMap();

        map.Add("Callout", structureType);

        Assert.True(map.Contains("Callout"));
    }

    [Theory]
    [InlineData("P")]
    [InlineData("Div")]
    [InlineData("Span")]
    [InlineData("TOCI")]
    public void RemappingAStandardStructureTypeIsRejected(string role)
    {
        var map = new RoleMap();

        var error = Assert.Throws<ArgumentException>(() => map.Add(role, "Div"));

        Assert.Contains("standard types shall not be remapped", error.Message, StringComparison.Ordinal);
        Assert.Contains("ISO 14289-1 7.1", error.Message, StringComparison.Ordinal);
        Assert.False(map.Contains(role));
    }

    [Fact]
    public void SelfMappingIsRejectedAsACycle()
    {
        var map = new RoleMap();

        var error = Assert.Throws<ArgumentException>(() => map.Add("Callout", "Callout"));

        Assert.Contains("cycle", error.Message, StringComparison.Ordinal);
        Assert.False(map.Contains("Callout"));
    }

    [Fact]
    public void ClosingALongerCycleIsRejected()
    {
        var map = new RoleMap();
        map.Add("Callout", "Aside");
        map.Add("Aside", "Sidebar");

        var error = Assert.Throws<ArgumentException>(() => map.Add("Sidebar", "Callout"));

        Assert.Contains("Sidebar -> Callout -> Aside -> Sidebar", error.Message, StringComparison.Ordinal);
        Assert.False(map.Contains("Sidebar"));
    }

    [Fact]
    public void MultiHopChainTerminatingAtAStandardTypeIsAccepted()
    {
        var map = new RoleMap();

        map.Add("Callout", "Aside");
        map.Add("Aside", "Sidebar");
        map.Add("Sidebar", "Div");

        Assert.True(map.Contains("Callout"));
        Assert.True(map.Contains("Aside"));
        Assert.True(map.Contains("Sidebar"));
    }

    [Fact]
    public void EmptyRoleOrStructureTypeIsRejected()
    {
        var map = new RoleMap();

        Assert.Throws<ArgumentException>(() => map.Add("", "P"));
        Assert.Throws<ArgumentException>(() => map.Add("Callout", ""));
    }
}

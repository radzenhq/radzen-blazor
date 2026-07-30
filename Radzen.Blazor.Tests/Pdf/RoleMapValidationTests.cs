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
    public void StandardStructureTypesAreAccepted(string structureType)
    {
        var map = new RoleMap();

        map.Add("Callout", structureType);

        Assert.True(map.Contains("Callout"));
    }

    [Theory]
    [InlineData("Figure2")]
    [InlineData("p")]
    [InlineData("Paragraph")]
    [InlineData("Artifact")]
    [InlineData("H7")]
    [InlineData("Callout")]
    public void NonStandardStructureTypesAreRejected(string structureType)
    {
        var map = new RoleMap();

        var error = Assert.Throws<ArgumentException>(() => map.Add("Callout", structureType));

        Assert.Contains("is not a standard ISO 32000-1 structure type", error.Message, StringComparison.Ordinal);
        Assert.False(map.Contains("Callout"));
    }

    [Fact]
    public void EmptyRoleOrStructureTypeIsRejected()
    {
        var map = new RoleMap();

        Assert.Throws<ArgumentException>(() => map.Add("", "P"));
        Assert.Throws<ArgumentException>(() => map.Add("Callout", ""));
    }
}

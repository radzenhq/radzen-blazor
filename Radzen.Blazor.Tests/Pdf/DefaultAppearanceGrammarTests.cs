using System;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class DefaultAppearanceGrammarTests
{
    [Fact]
    public void WriteEscapesDelimitersInTheFontName()
    {
        Assert.Equal("/My#20Font 12 Tf 0 g", DefaultAppearanceGrammar.Write("My Font", 12, "0 g"));
    }

    [Fact]
    public void WriteLeavesAnUnescapedNameByteIdentical()
    {
        Assert.Equal("/Helv 11.5 Tf 0 g", DefaultAppearanceGrammar.Write("Helv", 11.5, "0 g"));
    }

    [Fact]
    public void WriteRejectsNonFiniteSize()
    {
        Assert.Throws<InvalidOperationException>(() => DefaultAppearanceGrammar.Write("Helv", double.NaN, "0 g"));
        Assert.Throws<InvalidOperationException>(() => DefaultAppearanceGrammar.Write("Helv", double.PositiveInfinity, "0 g"));
    }
}

#nullable enable
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class ExtractionFontSharingTests
{
    private static PortableDocument Build(int paragraphs)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        for (var i = 0; i < paragraphs; i++)
        {
            BuildTestSupport.AddText(section, "Page filling text " + i, BuildTestSupport.Latin, 36);
        }

        return new DocumentRenderer().Render(document);
    }

    private static List<ReverseFont> Fonts(PortableDocument document)
        => document.Pages.SelectMany(page => page.TextFonts!.Values).ToList();

    [Fact]
    public void EveryPageSharesOneReverseFontPerFont()
    {
        var document = Build(60);
        Assert.True(document.Pages.Count > 1, "the fixture must paginate");

        var fonts = Fonts(document);
        Assert.Equal(document.Pages.Count, fonts.Count);
        Assert.Single(fonts.Distinct(ReferenceEqualityComparer<ReverseFont>.Instance));
    }

    [Fact]
    public void ReverseFontInstancesDoNotScaleWithPageCount()
    {
        var small = Fonts(Build(10)).Distinct(ReferenceEqualityComparer<ReverseFont>.Instance).Count();
        var large = Fonts(Build(60)).Distinct(ReferenceEqualityComparer<ReverseFont>.Instance).Count();

        Assert.Equal(small, large);
    }

    [Fact]
    public void SharedReverseFontStillExtractsText()
    {
        var document = Build(60);
        Assert.Contains("Page filling text 0", document.ExtractText(), System.StringComparison.Ordinal);
        Assert.Contains("Page filling text 59", document.ExtractText(), System.StringComparison.Ordinal);
    }

    private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
        where T : class
    {
        public static ReferenceEqualityComparer<T> Instance { get; } = new();

        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

        public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}

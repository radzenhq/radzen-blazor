#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class PageLabelsTests
{
    private static PortableDocument Document(int pages)
    {
        var document = new PortableDocument();
        for (var i = 0; i < pages; i++)
        {
            document.Pages.Add(PageSizes.A4).SetContent(Encoding.ASCII.GetBytes("BT (p) Tj ET"));
        }

        return document;
    }

    private static string Catalog(PortableDocument document) => Line(Emit(document), "/Type /Catalog");

    [Fact]
    public void PageLabels_EmitStyledRangesWithPrefixAndStart()
    {
        var document = Document(6);
        document.PageLabels.Add(new PageLabel(0) { Style = PageLabelStyle.LowercaseRoman });
        document.PageLabels.Add(new PageLabel(2) { Style = PageLabelStyle.Decimal, Prefix = "A-", Start = 5 });

        var ranges = Shaped(
            "catalog /PageLabels",
            @"/PageLabels << /Nums \[0 (<<[^>]*>>) 2 (<<[^>]*>>)\] >>",
            Catalog(document));

        var front = ranges.Groups[1].Value;
        Carries("front label range", "/S /r", front);
        Lacks("front label range", "/P ", front);
        Lacks("front label range", "/St ", front);

        var body = ranges.Groups[2].Value;
        Carries("body label range", "/S /D", body);
        Carries("body label range", "/P (A-)", body);
        Carries("body label range", "/St 5", body);
    }

    [Fact]
    public void PageLabels_RangesAreSortedByStartPage()
    {
        var document = Document(4);
        document.PageLabels.Add(new PageLabel(2) { Style = PageLabelStyle.Decimal });
        document.PageLabels.Add(new PageLabel(0) { Style = PageLabelStyle.UppercaseRoman });

        Shaped(
            "catalog /PageLabels",
            @"/PageLabels << /Nums \[0 <<[^>]*>> 2 <<",
            Catalog(document));
    }

    [Fact]
    public void PrefixOnlyRange_OmitsStyle()
    {
        var document = Document(2);
        document.PageLabels.Add(new PageLabel(0) { Prefix = "Cover" });

        var range = Shaped(
            "catalog /PageLabels",
            @"/PageLabels << /Nums \[0 (<<[^>]*>>)\] >>",
            Catalog(document)).Groups[1].Value;

        Lacks("label range", "/S ", range);
        Carries("label range", "/P (Cover)", range);
    }

    [Fact]
    public void MissingPageZeroRange_Throws()
    {
        var document = Document(4);
        document.PageLabels.Add(new PageLabel(2) { Style = PageLabelStyle.Decimal });

        Assert.Throws<InvalidOperationException>(() => document.ToArray());
    }

    [Fact]
    public void DuplicateStartPage_Throws()
    {
        var document = Document(4);
        document.PageLabels.Add(new PageLabel(0) { Style = PageLabelStyle.Decimal });
        document.PageLabels.Add(new PageLabel(0) { Style = PageLabelStyle.UppercaseRoman });

        Assert.Throws<InvalidOperationException>(() => document.ToArray());
    }

    [Fact]
    public void StartOrdinalBelowOne_Throws()
    {
        var document = Document(2);
        document.PageLabels.Add(new PageLabel(0) { Style = PageLabelStyle.Decimal, Start = 0 });

        Assert.Throws<InvalidOperationException>(() => document.ToArray());
    }

    [Fact]
    public void NoPageLabels_EmitsNothing_AndByteIdentical()
    {
        var bytes = Document(2).ToArray();
        Assert.Equal(bytes, Document(2).ToArray());

        Lacks("catalog", "/PageLabels", Line(Encoding.Latin1.GetString(bytes), "/Type /Catalog"));
    }

    [Theory]
    [InlineData(PageLabelStyle.Decimal, "D")]
    [InlineData(PageLabelStyle.UppercaseRoman, "R")]
    [InlineData(PageLabelStyle.LowercaseRoman, "r")]
    [InlineData(PageLabelStyle.UppercaseLetters, "A")]
    [InlineData(PageLabelStyle.LowercaseLetters, "a")]
    public void EveryPageLabelStyle_WritesItsSpecifiedNumberingStyleName(PageLabelStyle style, string name)
    {
        var document = Document(2);
        document.PageLabels.Add(new PageLabel(0) { Style = style });

        Shaped(
            $"catalog /PageLabels /S /{name}",
            $@"/PageLabels << /Nums \[0 << /S /{name} >>\] >>",
            Catalog(document));
    }
}

#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

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

    private static Dictionary<int, DictionaryObject> Labels(DocumentReader reader)
    {
        var catalog = ContentTestHelpers.Catalog(reader);
        var tree = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["PageLabels"]));
        var nums = Assert.IsType<ArrayObject>(reader.Resolve(tree["Nums"]));
        var result = new Dictionary<int, DictionaryObject>();
        for (var i = 0; i + 1 < nums.Count; i += 2)
        {
            var key = Assert.IsType<NumberObject>(reader.Resolve(nums[i])).IntValue;
            result[key] = Assert.IsType<DictionaryObject>(reader.Resolve(nums[i + 1]));
        }

        return result;
    }

    [Fact]
    public void PageLabels_EmitStyledRangesWithPrefixAndStart()
    {
        var document = Document(6);
        document.PageLabels.Add(new PageLabel(0) { Style = PageLabelStyle.LowercaseRoman });
        document.PageLabels.Add(new PageLabel(2) { Style = PageLabelStyle.Decimal, Prefix = "A-", Start = 5 });

        var reader = DocumentReader.Parse(document.ToArray());
        var labels = Labels(reader);

        Assert.Equal(new[] { 0, 2 }, new List<int>(labels.Keys).ToArray());

        var front = labels[0];
        Assert.Equal("r", Assert.IsType<NameObject>(reader.Resolve(front["S"])).Value);
        Assert.False(front.ContainsKey("P"));
        Assert.False(front.ContainsKey("St"));

        var body = labels[2];
        Assert.Equal("D", Assert.IsType<NameObject>(reader.Resolve(body["S"])).Value);
        Assert.Equal("A-", Assert.IsType<StringObject>(reader.Resolve(body["P"])).Value);
        Assert.Equal(5, Assert.IsType<NumberObject>(reader.Resolve(body["St"])).IntValue);
    }

    [Fact]
    public void PageLabels_RangesAreSortedByStartPage()
    {
        var document = Document(4);
        document.PageLabels.Add(new PageLabel(2) { Style = PageLabelStyle.Decimal });
        document.PageLabels.Add(new PageLabel(0) { Style = PageLabelStyle.UppercaseRoman });

        var reader = DocumentReader.Parse(document.ToArray());
        var catalog = ContentTestHelpers.Catalog(reader);
        var nums = Assert.IsType<ArrayObject>(reader.Resolve(
            Assert.IsType<DictionaryObject>(reader.Resolve(catalog["PageLabels"]))["Nums"]));
        Assert.Equal(0, Assert.IsType<NumberObject>(reader.Resolve(nums[0])).IntValue);
        Assert.Equal(2, Assert.IsType<NumberObject>(reader.Resolve(nums[2])).IntValue);
    }

    [Fact]
    public void PrefixOnlyRange_OmitsStyle()
    {
        var document = Document(2);
        document.PageLabels.Add(new PageLabel(0) { Prefix = "Cover" });

        var reader = DocumentReader.Parse(document.ToArray());
        var label = Labels(reader)[0];
        Assert.False(label.ContainsKey("S"));
        Assert.Equal("Cover", Assert.IsType<StringObject>(reader.Resolve(label["P"])).Value);
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
        Assert.False(ContentTestHelpers.Catalog(DocumentReader.Parse(bytes)).ContainsKey("PageLabels"));
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

        var reader = DocumentReader.Parse(document.ToArray());

        Assert.Equal(name, Assert.IsType<NameObject>(reader.Resolve(Labels(reader)[0]["S"])).Value);
    }
}

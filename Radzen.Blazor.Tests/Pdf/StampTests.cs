#nullable enable
using System;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class StampTests
{
    [Fact]
    public void StampedTextAppearsInReloadedPageContent()
    {
        var document = FormTestSupport.LoadFixture();
        var page = document.Pages[0];
        page.Content.Add(new TextContent("STAMPED", Unit.FromPoint(72), Unit.FromPoint(400)));

        var reader = FormTestSupport.Reload(document);
        var content = FormTestSupport.PageContentText(reader);

        Assert.Contains("STAMPED", content);
        Assert.Contains("Tj", content);
    }

    [Fact]
    public void StampAppendsWithoutDisturbingOtherStamps()
    {
        var document = FormTestSupport.LoadFixture();
        var page = document.Pages[0];
        page.Content.Add(new TextContent("FIRST", Unit.FromPoint(72), Unit.FromPoint(400)));
        page.Content.Add(new TextContent("SECOND", Unit.FromPoint(72), Unit.FromPoint(380)));

        var reader = FormTestSupport.Reload(document);
        var content = FormTestSupport.PageContentText(reader);

        Assert.Contains("FIRST", content);
        Assert.Contains("SECOND", content);
        Assert.True(content.IndexOf("FIRST", StringComparison.Ordinal)
            < content.IndexOf("SECOND", StringComparison.Ordinal));
    }

    [Fact]
    public void StampPreservesFormAnnotations()
    {
        var document = FormTestSupport.LoadFixture();
        document.Pages[0].Content.Add(new TextContent("STAMPED", Unit.FromPoint(72), Unit.FromPoint(400)));

        var reader = FormTestSupport.Reload(document);
        var pageDict = FormTestSupport.FirstPage(reader);

        Assert.True(pageDict.TryGetValue("Annots", out var annotsObject), "stamped page lost /Annots");
        var annots = Assert.IsType<ArrayObject>(reader.Resolve(annotsObject!));
        Assert.Equal(2, annots.Count);

        var catalog = FormTestSupport.Catalog(reader);
        Assert.True(catalog.TryGetValue("AcroForm", out _));
    }

    [Fact]
    public void StampPreservesPageSize()
    {
        var document = FormTestSupport.LoadFixture();
        document.Pages[0].Content.Add(new TextContent("STAMPED", Unit.FromPoint(72), Unit.FromPoint(400)));

        var reader = FormTestSupport.Reload(document);
        var pageDict = FormTestSupport.FirstPage(reader);

        var box = Assert.IsType<ArrayObject>(reader.Resolve(pageDict["MediaBox"]));
        Assert.Equal(612.0, ((NumberObject)box[2]).DoubleValue, 3);
        Assert.Equal(792.0, ((NumberObject)box[3]).DoubleValue, 3);
    }
}

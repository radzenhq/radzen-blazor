#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class DeterministicDocumentIdTests
{
    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    private static Document PlainDocument()
    {
        var document = new Document { IncludeDocumentId = true };
        document.Info.Title = "Deterministic";
        document.Pages.Add(PageSizes.A4).SetContent(Ascii("BT (hello) Tj ET"));
        return document;
    }

    private static ArrayObject Id(byte[] pdf)
    {
        var reader = DocumentReader.Parse(pdf);
        Assert.True(reader.Trailer.TryGetValue("ID", out var idObject), "trailer must carry /ID");
        return Assert.IsType<ArrayObject>(reader.Resolve(idObject!));
    }

    [Fact]
    public void EverySavedDocument_HasTrailerId_WithTwoEqual32HexHalves()
    {
        var id = Id(PlainDocument().ToArray());
        Assert.Equal(2, id.Count);
        var first = Assert.IsType<StringObject>(id[0]);
        var second = Assert.IsType<StringObject>(id[1]);
        Assert.Equal(32, first.Value.Length);
        Assert.Equal(first.Value, second.Value);
        foreach (var ch in first.Value)
        {
            Assert.True(Uri.IsHexDigit(ch), $"'{ch}' is not a hex digit");
        }
    }

    [Fact]
    public void DocumentId_IsDeterministic_AcrossIndependentSaves()
    {
        var first = Assert.IsType<StringObject>(Id(PlainDocument().ToArray())[0]);
        var second = Assert.IsType<StringObject>(Id(PlainDocument().ToArray())[0]);
        Assert.Equal(first.Value, second.Value);
    }

    [Fact]
    public void DocumentId_DependsOnContent()
    {
        var a = new Document { IncludeDocumentId = true };
        a.Pages.Add(PageSizes.A4).SetContent(Ascii("BT (alpha) Tj ET"));

        var b = new Document { IncludeDocumentId = true };
        b.Pages.Add(PageSizes.A4).SetContent(Ascii("BT (beta) Tj ET"));

        Assert.NotEqual(
            Assert.IsType<StringObject>(Id(a.ToArray())[0]).Value,
            Assert.IsType<StringObject>(Id(b.ToArray())[0]).Value);
    }

    [Fact]
    public void PlainSave_StaysByteIdentical_AcrossTwoBuilds()
    {
        Assert.Equal(PlainDocument().ToArray(), PlainDocument().ToArray());
    }

    [Fact]
    public void DocumentId_HasAPinnedValue_ForAKnownDocument()
    {
        Assert.Equal(
            "FB42652C3E52C1AECAC2A39EFA11EC9D",
            Assert.IsType<StringObject>(Id(PlainDocument().ToArray())[0]).Value);
    }

}

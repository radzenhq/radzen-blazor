#nullable enable
using System;
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ForeignProducerCorpusTests
{
    public static readonly string[] Producers = ["ghostscript", "mupdf", "cups", "chromium"];

    public static TheoryData<string> Names()
    {
        var data = new TheoryData<string>();
        foreach (var name in Producers)
        {
            data.Add(name);
        }

        return data;
    }

    internal static byte[] Source(string name) => PdfTestResources.ReadAllBytes("Foreign/" + name + ".pdf");

    internal static PortableDocument Load(byte[] bytes) => PortableDocument.LoadFromStream(new MemoryStream(bytes));

    internal static byte[] Save(PortableDocument document)
    {
        using var buffer = new MemoryStream();
        document.SaveToStream(buffer);
        return buffer.ToArray();
    }

    private static string[] Expectations(string name) => name switch
    {
        "ghostscript" => ["Ghostscript corpus page"],
        "mupdf" => ["MuPDF corpus page"],
        "cups" => ["CUPS corpus page from Apple Core Graphics.", "Second line of text."],
        "chromium" => ["Chromium corpus page", "Printed to PDF by a browser engine."],
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    [Theory]
    [MemberData(nameof(Names))]
    public void ForeignFile_LoadsWithOneSanePage(string name)
    {
        var document = Load(Source(name));

        Assert.Single(document.Pages);

        var page = document.Pages[0];
        Assert.True(page.MediaBox.Width > 0);
        Assert.True(page.MediaBox.Height > 0);
        Assert.True(page.Width.Point > 0);
        Assert.True(page.Height.Point > 0);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void ForeignFile_ExtractsExpectedText(string name)
    {
        var text = Load(Source(name)).Pages[0].ExtractText();

        foreach (var expectation in Expectations(name))
        {
            Assert.Contains(expectation, text, StringComparison.Ordinal);
        }
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void ForeignFile_SurvivesRoundTripThroughTheWriter(string name)
    {
        var document = Load(Source(name));
        var text = document.Pages[0].ExtractText();

        var reloaded = Load(Save(document));

        Assert.Single(reloaded.Pages);
        Assert.Equal(text, reloaded.Pages[0].ExtractText());
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void ForeignFile_ResavesDeterministically(string name)
    {
        var document = Load(Source(name));

        Assert.Equal(Save(document), Save(document));
    }
}

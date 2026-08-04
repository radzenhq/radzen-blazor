#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class DocumentDateProducerTests
{
    private static readonly DateTimeOffset Created =
        new(2024, 1, 15, 10, 30, 0, TimeSpan.FromHours(2));

    private static readonly DateTimeOffset Modified =
        new(2024, 3, 20, 8, 15, 45, TimeSpan.FromHours(-5));

    private static PortableDocument Document(bool withMetadata)
    {
        var document = new PortableDocument();
        document.Info.Title = "Report";
        document.Pages.Add(PageSizes.A4).SetContent(Encoding.ASCII.GetBytes("BT (hi) Tj ET"));
        if (withMetadata)
        {
            document.Info.Producer = "Acme Publisher 3.0";
            document.Info.CreationDate = Created;
            document.Info.ModificationDate = Modified;
        }

        return document;
    }

    private static string Info(string emission)
    {
        var reference = Shaped("trailer", @"/Info (\d+) 0 R", Line(emission, "/Info "));
        return IndirectObject(emission, reference.Groups[1].Value);
    }

    private static string Xmp(string emission)
    {
        var reference = Shaped("catalog", @"/Metadata (\d+) 0 R", Line(emission, "/Type /Catalog"));
        return IndirectObject(emission, reference.Groups[1].Value);
    }

    [Fact]
    public void CreationAndModificationDate_WrittenToInfoInPdfDateFormat()
    {
        var info = Info(Emit(Document(withMetadata: true)));

        Carries("info dictionary", "/CreationDate (D:20240115103000+02'00')", info);
        Carries("info dictionary", "/ModDate (D:20240320081545-05'00')", info);
        Carries("info dictionary", "/Producer (Acme Publisher 3.0)", info);
    }

    [Fact]
    public void Producer_And_Dates_Mirrored_IntoXmpPacket()
    {
        var xmp = Xmp(Emit(Document(withMetadata: true)));

        Carries("xmp packet", "<pdf:Producer>Acme Publisher 3.0</pdf:Producer>", xmp);
        Carries("xmp packet", "<xmp:CreateDate>2024-01-15T10:30:00+02:00</xmp:CreateDate>", xmp);
        Carries("xmp packet", "<xmp:ModifyDate>2024-03-20T08:15:45-05:00</xmp:ModifyDate>", xmp);
    }

    [Fact]
    public void WithoutMetadata_NoDatesProducerOrXmp_AndByteIdentical()
    {
        var bytes = Document(withMetadata: false).ToArray();
        Assert.Equal(bytes, Document(withMetadata: false).ToArray());

        var emission = Encoding.Latin1.GetString(bytes);
        Lacks("emission", "/CreationDate", emission);
        Lacks("emission", "/ModDate", emission);
        Lacks("emission", "/Producer", emission);
        Lacks("emission", "/Metadata", emission);
        Lacks("catalog", "/Metadata", Line(emission, "/Type /Catalog"));
    }
}

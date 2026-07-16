#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Feature 1: caller-supplied CreationDate/ModificationDate (no clock) and Producer on
// DocumentInfo are written to the /Info dictionary AND mirrored into an XMP packet.
// With none set the output stays byte identical to the pre-feature build.
public class DocumentDateProducerTests
{
    private static readonly DateTimeOffset Created =
        new(2024, 1, 15, 10, 30, 0, TimeSpan.FromHours(2));

    private static readonly DateTimeOffset Modified =
        new(2024, 3, 20, 8, 15, 45, TimeSpan.FromHours(-5));

    private static Document Document(bool withMetadata)
    {
        var document = new Document();
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

    private static DictionaryObject Info(DocumentReader reader)
    {
        Assert.True(reader.Trailer.TryGetValue("Info", out var infoObject), "trailer has /Info");
        return Assert.IsType<DictionaryObject>(reader.Resolve(infoObject!));
    }

    private static string Xmp(DocumentReader reader)
    {
        var catalog = ContentTestHelpers.Catalog(reader);
        Assert.True(catalog.TryGetValue("Metadata", out var metadataObject), "catalog has /Metadata");
        var stream = Assert.IsType<StreamObject>(reader.Resolve(metadataObject!));
        return Encoding.UTF8.GetString(stream.Data.ToArray());
    }

    [Fact]
    public void CreationAndModificationDate_WrittenToInfoInPdfDateFormat()
    {
        var reader = DocumentReader.Parse(Document(withMetadata: true).ToArray());
        var info = Info(reader);

        Assert.Equal("D:20240115103000+02'00'",
            Assert.IsType<StringObject>(reader.Resolve(info["CreationDate"])).Value);
        Assert.Equal("D:20240320081545-05'00'",
            Assert.IsType<StringObject>(reader.Resolve(info["ModDate"])).Value);
        Assert.Equal("Acme Publisher 3.0",
            Assert.IsType<StringObject>(reader.Resolve(info["Producer"])).Value);
    }

    [Fact]
    public void Producer_And_Dates_Mirrored_IntoXmpPacket()
    {
        var reader = DocumentReader.Parse(Document(withMetadata: true).ToArray());
        var xmp = Xmp(reader);

        Assert.Contains("<pdf:Producer>Acme Publisher 3.0</pdf:Producer>", xmp);
        Assert.Contains("<xmp:CreateDate>2024-01-15T10:30:00+02:00</xmp:CreateDate>", xmp);
        Assert.Contains("<xmp:ModifyDate>2024-03-20T08:15:45-05:00</xmp:ModifyDate>", xmp);
    }

    [Fact]
    public void WithoutMetadata_NoDatesProducerOrXmp_AndByteIdentical()
    {
        var bytes = Document(withMetadata: false).ToArray();
        Assert.Equal(bytes, Document(withMetadata: false).ToArray());

        var text = Encoding.Latin1.GetString(bytes);
        Assert.DoesNotContain("/CreationDate", text);
        Assert.DoesNotContain("/ModDate", text);
        Assert.DoesNotContain("/Producer", text);
        Assert.DoesNotContain("/Metadata", text);

        var reader = DocumentReader.Parse(bytes);
        Assert.False(ContentTestHelpers.Catalog(reader).ContainsKey("Metadata"));
    }
}

#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// P2: PDF/A-3 conformance mode. Pins the public DocumentBuilder.Conformance
// option (enum PdfAConformance { None, PdfA3B, PdfA3A }) and the conforming
// SaveToStream output: unfiltered /Metadata XMP with pdfaid part/conformance
// and dc/xmp/pdf entries consistent with DocumentInfo, an sRGB GTS_PDFA1
// /OutputIntents entry (DestOutputProfile ICC stream /N 3), a trailer /ID,
// no /Encrypt, fully embedded subset fonts with CIDSet, a hard error for
// base-14 fonts referenced by name, and Level A tagging (MarkInfo +
// StructTreeRoot).
public class PdfAConformanceTests
{
    private const string MissingApi =
        "DocumentBuilder.Conformance (enum PdfAConformance) is missing - P2 PDF/A conformance mode is not implemented";

    private static PropertyInfo? ConformanceProperty()
        => typeof(DocumentBuilder).GetProperty("Conformance");

    private static void SetConformance(DocumentBuilder builder, string name)
    {
        var property = ConformanceProperty();
        Assert.True(property is not null && property.PropertyType.IsEnum && property.CanWrite, MissingApi);
        Assert.True(Enum.GetNames(property!.PropertyType).Contains(name),
            $"PdfAConformance is missing the '{name}' value");
        property.SetValue(builder, Enum.Parse(property.PropertyType, name));
    }

    private static DocumentBuilder Author(string conformance)
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);

        builder.Info.Title = "PDF/A Invoice";
        builder.Info.Author = "Radzen Ltd";
        builder.Info.Subject = "Conformance fixture";
        builder.Info.Creator = "Radzen.Documents.Pdf tests";
        builder.Info.Keywords = "pdfa, invoice";

        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Hello PDF/A", BuildTestSupport.Latin);

        SetConformance(builder, conformance);
        return builder;
    }

    private static DictionaryObject Catalog(DocumentReader reader)
    {
        Assert.True(reader.Trailer.TryGetValue("Root", out var rootObject), "trailer has /Root");
        return Assert.IsType<DictionaryObject>(reader.Resolve(rootObject!));
    }

    private static StreamObject MetadataStream(DocumentReader reader)
    {
        var catalog = Catalog(reader);
        Assert.True(catalog.TryGetValue("Metadata", out var metadataObject), "catalog has /Metadata");
        return Assert.IsType<StreamObject>(reader.Resolve(metadataObject!));
    }

    private static string MetadataPacket(DocumentReader reader)
    {
        var metadata = MetadataStream(reader);
        Assert.False(metadata.Dictionary.ContainsKey("Filter"), "/Metadata must be unfiltered");
        return Encoding.UTF8.GetString(metadata.Data.ToArray());
    }

    [Fact]
    public void Conformance_PublicEnumOptionShape()
    {
        var property = ConformanceProperty();
        Assert.True(property is not null, MissingApi);
        var type = property!.PropertyType;
        Assert.Equal("PdfAConformance", type.Name);
        Assert.Equal("Radzen.Documents.Pdf", type.Namespace);
        Assert.True(type.IsEnum, "PdfAConformance must be an enum");
        Assert.True(type.IsPublic, "PdfAConformance must be public");
        Assert.Equal(["None", "PdfA3B", "PdfA3A", "PdfA2B", "PdfA2A", "PdfA4", "PdfA4E", "PdfA4F"], Enum.GetNames(type));
        Assert.True(property.CanRead && property.CanWrite, "Conformance must be a read/write property");
        Assert.Equal("None", property.GetValue(new DocumentBuilder())!.ToString());
    }

    [Fact]
    public void PdfA3B_MetadataStream_HasPdfaidPart3ConformanceB()
    {
        var reader = BuildTestSupport.Read(Author("PdfA3B"));

        var metadata = MetadataStream(reader);
        Assert.Equal("Metadata", BuildTestSupport.Name(reader, metadata.Dictionary, "Type"));
        Assert.Equal("XML", BuildTestSupport.Name(reader, metadata.Dictionary, "Subtype"));

        var packet = MetadataPacket(reader);
        Assert.Contains("<?xpacket begin=", packet, StringComparison.Ordinal);
        Assert.Contains("<?xpacket end=\"w\"?>", packet, StringComparison.Ordinal);
        Assert.Contains("<pdfaid:part>3</pdfaid:part>", packet, StringComparison.Ordinal);
        Assert.Contains("<pdfaid:conformance>B</pdfaid:conformance>", packet, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfA3A_MetadataStream_HasConformanceA()
    {
        var packet = MetadataPacket(BuildTestSupport.Read(Author("PdfA3A")));
        Assert.Contains("<pdfaid:part>3</pdfaid:part>", packet, StringComparison.Ordinal);
        Assert.Contains("<pdfaid:conformance>A</pdfaid:conformance>", packet, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfA3B_Xmp_MatchesDocumentInfo()
    {
        var packet = MetadataPacket(BuildTestSupport.Read(Author("PdfA3B")));

        Assert.Contains("<dc:title><rdf:Alt><rdf:li xml:lang=\"x-default\">PDF/A Invoice</rdf:li>", packet, StringComparison.Ordinal);
        Assert.Contains("<dc:creator><rdf:Seq><rdf:li>Radzen Ltd</rdf:li>", packet, StringComparison.Ordinal);
        Assert.Contains("Conformance fixture</rdf:li></rdf:Alt></dc:description>", packet, StringComparison.Ordinal);
        Assert.Contains("<xmp:CreatorTool>Radzen.Documents.Pdf tests</xmp:CreatorTool>", packet, StringComparison.Ordinal);
        Assert.Contains("<pdf:Keywords>pdfa, invoice</pdf:Keywords>", packet, StringComparison.Ordinal);
        Assert.Contains("<pdf:Producer>", packet, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfA3B_OutputIntents_SrgbGtsPdfa1WithIccProfile()
    {
        var reader = BuildTestSupport.Read(Author("PdfA3B"));

        var catalog = Catalog(reader);
        Assert.True(catalog.TryGetValue("OutputIntents", out var intentsObject), "catalog has /OutputIntents");
        var intents = Assert.IsType<ArrayObject>(reader.Resolve(intentsObject!));
        var intent = Assert.IsType<DictionaryObject>(reader.Resolve(Assert.Single(intents)));

        Assert.Equal("OutputIntent", BuildTestSupport.Name(reader, intent, "Type"));
        Assert.Equal("GTS_PDFA1", BuildTestSupport.Name(reader, intent, "S"));
        Assert.True(intent.ContainsKey("OutputConditionIdentifier"), "OutputIntent has /OutputConditionIdentifier");

        Assert.True(intent.TryGetValue("DestOutputProfile", out var profileObject), "OutputIntent has /DestOutputProfile");
        var profile = Assert.IsType<StreamObject>(reader.Resolve(profileObject!));
        Assert.Equal(3, ((NumberObject)reader.Resolve(profile.Dictionary["N"])).IntValue);
        var icc = reader.DecodeStream(profile);
        Assert.True(icc.Length > 128, "DestOutputProfile ICC data is present");
        Assert.Equal("acsp", Encoding.ASCII.GetString(icc, 36, 4));
    }

    [Fact]
    public void PdfA3B_Trailer_HasDocumentId()
    {
        var reader = BuildTestSupport.Read(Author("PdfA3B"));

        Assert.True(reader.Trailer.TryGetValue("ID", out var idObject), "trailer has /ID");
        var id = Assert.IsType<ArrayObject>(reader.Resolve(idObject!));
        Assert.Equal(2, id.Count);
        foreach (var half in id)
        {
            var text = Assert.IsType<StringObject>(reader.Resolve(half));
            Assert.False(string.IsNullOrEmpty(text.Value), "trailer /ID halves are non-empty");
        }
    }

    [Fact]
    public void PdfA3B_HasNoEncryption()
    {
        var reader = BuildTestSupport.Read(Author("PdfA3B"));
        Assert.False(reader.Trailer.ContainsKey("Encrypt"), "PDF/A forbids /Encrypt");
        Assert.False(reader.IsEncrypted);
    }

    [Fact]
    public void PdfA3B_AllFonts_EmbeddedSubsetsWithCidSet()
    {
        var reader = BuildTestSupport.Read(Author("PdfA3B"));

        var fonts = BuildTestSupport.Fonts(reader);
        Assert.NotEmpty(fonts);

        foreach (var font in fonts)
        {
            Assert.Equal("Type0", BuildTestSupport.Name(reader, font, "Subtype"));
            var descendants = Assert.IsType<ArrayObject>(reader.Resolve(font["DescendantFonts"]));
            var descendant = Assert.IsType<DictionaryObject>(reader.Resolve(Assert.Single(descendants)));
            var descriptor = Assert.IsType<DictionaryObject>(reader.Resolve(descendant["FontDescriptor"]));

            var embedded = descriptor.ContainsKey("FontFile2") || descriptor.ContainsKey("FontFile3");
            Assert.True(embedded, "PDF/A fonts must embed FontFile2 or FontFile3");

            Assert.True(descriptor.TryGetValue("CIDSet", out var cidSetObject), "PDF/A subset fonts must carry /CIDSet");
            var cidSet = Assert.IsType<StreamObject>(reader.Resolve(cidSetObject!));
            Assert.True(reader.DecodeStream(cidSet).Length > 0, "/CIDSet stream has data");
        }
    }

    [Fact]
    public void PdfA3B_Base14FontByName_ThrowsActionable()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Hello", "Helvetica");
        SetConformance(builder, "PdfA3B");

        var exception = Record.Exception(() => builder.ToArray());

        Assert.True(exception is not null, "base-14 font by name must throw under PDF/A");
        Assert.Contains("PDF/A", exception!.Message, StringComparison.Ordinal);
        Assert.Contains("Helvetica", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void None_Base14FontByName_Succeeds()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Hello", "Helvetica");

        Assert.NotEmpty(builder.ToArray());
    }

    [Fact]
    public void PdfA3A_OutputIsTagged()
    {
        var reader = BuildTestSupport.Read(Author("PdfA3A"));
        var catalog = Catalog(reader);

        Assert.True(catalog.TryGetValue("MarkInfo", out var markInfoObject), "Level A requires /MarkInfo");
        var markInfo = Assert.IsType<DictionaryObject>(reader.Resolve(markInfoObject!));
        Assert.True(((BooleanObject)reader.Resolve(markInfo["Marked"])).Value, "/MarkInfo Marked must be true");

        Assert.True(catalog.TryGetValue("StructTreeRoot", out var structObject), "Level A requires /StructTreeRoot");
        var structRoot = Assert.IsType<DictionaryObject>(reader.Resolve(structObject!));
        Assert.Equal("StructTreeRoot", BuildTestSupport.Name(reader, structRoot, "Type"));
    }

    [Fact]
    public void None_ProducesNoPdfaMachinery()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Plain", BuildTestSupport.Latin);

        var reader = BuildTestSupport.Read(builder);
        var catalog = Catalog(reader);

        Assert.False(catalog.ContainsKey("OutputIntents"), "no /OutputIntents without conformance");
        if (catalog.TryGetValue("Metadata", out var metadataObject)
            && reader.Resolve(metadataObject!) is StreamObject metadata)
        {
            Assert.DoesNotContain("pdfaid:part", Encoding.UTF8.GetString(reader.DecodeStream(metadata)), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void PdfA3B_ContentSurvives_ExtractText()
    {
        var document = BuildTestSupport.Reload(Author("PdfA3B"));
        Assert.Contains("Hello PDF/A", document.Pages[0].ExtractText(), StringComparison.Ordinal);
    }
}

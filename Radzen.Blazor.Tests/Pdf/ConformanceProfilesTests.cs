#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ConformanceProfilesTests
{
    private static DocumentBuilder Author(PdfAConformance conformance = PdfAConformance.None, bool ua = false)
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);

        builder.Info.Title = "Conformance profiles";
        builder.Info.Author = "Radzen Ltd";

        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Hello conformance", BuildTestSupport.Latin);

        builder.Conformance = conformance;
        builder.PdfUA = ua;
        if (ua)
        {
            builder.Language = "en-US";
        }

        return builder;
    }

    private static DictionaryObject Catalog(DocumentReader reader)
    {
        Assert.True(reader.Trailer.TryGetValue("Root", out var rootObject), "trailer has /Root");
        return Assert.IsType<DictionaryObject>(reader.Resolve(rootObject!));
    }

    private static string MetadataPacket(DocumentReader reader)
    {
        var catalog = Catalog(reader);
        Assert.True(catalog.TryGetValue("Metadata", out var metadataObject), "catalog has /Metadata");
        var metadata = Assert.IsType<StreamObject>(reader.Resolve(metadataObject!));
        Assert.Equal("Metadata", BuildTestSupport.Name(reader, metadata.Dictionary, "Type"));
        Assert.Equal("XML", BuildTestSupport.Name(reader, metadata.Dictionary, "Subtype"));
        Assert.False(metadata.Dictionary.ContainsKey("Filter"), "/Metadata must be unfiltered");
        return Encoding.UTF8.GetString(metadata.Data.ToArray());
    }

    private static void AssertSrgbOutputIntent(DocumentReader reader)
    {
        var catalog = Catalog(reader);
        Assert.True(catalog.TryGetValue("OutputIntents", out var intentsObject), "catalog has /OutputIntents");
        var intents = Assert.IsType<ArrayObject>(reader.Resolve(intentsObject!));
        var intent = Assert.IsType<DictionaryObject>(reader.Resolve(Assert.Single(intents)));

        Assert.Equal("GTS_PDFA1", BuildTestSupport.Name(reader, intent, "S"));
        var profile = Assert.IsType<StreamObject>(reader.Resolve(intent["DestOutputProfile"]));
        Assert.Equal(3, ((NumberObject)reader.Resolve(profile.Dictionary["N"])).IntValue);
        Assert.Equal("acsp", Encoding.ASCII.GetString(reader.DecodeStream(profile), 36, 4));
    }

    private static void AssertTagged(DocumentReader reader)
    {
        var catalog = Catalog(reader);

        Assert.True(catalog.TryGetValue("MarkInfo", out var markInfoObject), "catalog has /MarkInfo");
        var markInfo = Assert.IsType<DictionaryObject>(reader.Resolve(markInfoObject!));
        Assert.True(((BooleanObject)reader.Resolve(markInfo["Marked"])).Value, "/MarkInfo Marked must be true");

        Assert.True(catalog.TryGetValue("StructTreeRoot", out var structObject), "catalog has /StructTreeRoot");
        var structRoot = Assert.IsType<DictionaryObject>(reader.Resolve(structObject!));
        Assert.Equal("StructTreeRoot", BuildTestSupport.Name(reader, structRoot, "Type"));
    }

    [Theory]
    [InlineData(PdfAConformance.PdfA2B, 2, "B")]
    [InlineData(PdfAConformance.PdfA2A, 2, "A")]
    [InlineData(PdfAConformance.PdfA4E, 4, "E")]
    public void PdfALevel_Xmp_HasPartAndConformance(PdfAConformance level, int part, string conformance)
    {
        var reader = BuildTestSupport.Read(Author(level));

        var packet = MetadataPacket(reader);
        Assert.Contains($"<pdfaid:part>{part}</pdfaid:part>", packet, StringComparison.Ordinal);
        Assert.Contains($"<pdfaid:conformance>{conformance}</pdfaid:conformance>", packet, StringComparison.Ordinal);

        AssertSrgbOutputIntent(reader);
        Assert.True(reader.Trailer.ContainsKey("ID"), "trailer has /ID");
    }

    [Fact]
    public void PdfA4_Xmp_HasPart4RevAndNoConformanceLetter()
    {
        var reader = BuildTestSupport.Read(Author(PdfAConformance.PdfA4));

        var packet = MetadataPacket(reader);
        Assert.Contains("<pdfaid:part>4</pdfaid:part>", packet, StringComparison.Ordinal);
        Assert.Contains("<pdfaid:rev>2020</pdfaid:rev>", packet, StringComparison.Ordinal);
        Assert.DoesNotContain("<pdfaid:conformance>", packet, StringComparison.Ordinal);

        AssertSrgbOutputIntent(reader);
    }

    [Theory]
    [InlineData(PdfAConformance.PdfA4)]
    [InlineData(PdfAConformance.PdfA4E)]
    [InlineData(PdfAConformance.PdfA4F)]
    public void PdfA4Levels_Catalog_DeclaresVersion20(PdfAConformance level)
    {
        var builder = Author(level);
        if (level == PdfAConformance.PdfA4F)
        {
            builder.Attachments.Add("data.xml", Encoding.UTF8.GetBytes("<data/>"), AttachmentRelationship.Data, "text/xml");
        }

        var catalog = Catalog(BuildTestSupport.Read(builder));
        Assert.True(catalog.TryGetValue("Version", out var version), "catalog has /Version");
        Assert.Equal("2.0", Assert.IsType<NameObject>(version).Value);
    }

    [Theory]
    [InlineData(PdfAConformance.PdfA4, "%PDF-2.0")]
    [InlineData(PdfAConformance.PdfA4E, "%PDF-2.0")]
    [InlineData(PdfAConformance.PdfA2B, "%PDF-1.7")]
    [InlineData(PdfAConformance.PdfA3B, "%PDF-1.7")]
    public void FileHeader_MatchesConformancePart(PdfAConformance level, string expectedHeader)
    {
        var bytes = Author(level).ToArray();
        Assert.Equal(expectedHeader, Encoding.ASCII.GetString(bytes, 0, expectedHeader.Length));
    }

    [Fact]
    public void PdfA2A_OutputIsTagged()
    {
        AssertTagged(BuildTestSupport.Read(Author(PdfAConformance.PdfA2A)));
    }

    [Fact]
    public void PdfA4F_WithAttachment_HasConformanceF()
    {
        var builder = Author(PdfAConformance.PdfA4F);
        builder.Attachments.Add("data.xml", Encoding.UTF8.GetBytes("<data/>"), AttachmentRelationship.Data, "text/xml");

        var packet = MetadataPacket(BuildTestSupport.Read(builder));
        Assert.Contains("<pdfaid:part>4</pdfaid:part>", packet, StringComparison.Ordinal);
        Assert.Contains("<pdfaid:rev>2020</pdfaid:rev>", packet, StringComparison.Ordinal);
        Assert.Contains("<pdfaid:conformance>F</pdfaid:conformance>", packet, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfA4F_WithoutAttachment_Throws()
    {
        var exception = Record.Exception(() => Author(PdfAConformance.PdfA4F).ToArray());

        Assert.NotNull(exception);
        Assert.Contains("PDF/A-4F", exception!.Message, StringComparison.Ordinal);
        Assert.Contains("embedded file", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(PdfAConformance.PdfA2B)]
    [InlineData(PdfAConformance.PdfA2A)]
    [InlineData(PdfAConformance.PdfA4)]
    [InlineData(PdfAConformance.PdfA4E)]
    public void AttachmentRestrictedLevels_WithAttachment_Throw(PdfAConformance level)
    {
        var builder = Author(level);
        builder.Attachments.Add("data.xml", Encoding.UTF8.GetBytes("<data/>"), AttachmentRelationship.Data, "text/xml");

        var exception = Record.Exception(() => builder.ToArray());

        Assert.NotNull(exception);
        Assert.Contains(level.ToString(), exception!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfUA_Xmp_HasPdfuaidPart1()
    {
        var reader = BuildTestSupport.Read(Author(ua: true));

        var packet = MetadataPacket(reader);
        Assert.Contains("xmlns:pdfuaid=\"http://www.aiim.org/pdfua/ns/id/\"", packet, StringComparison.Ordinal);
        Assert.Contains("<pdfuaid:part>1</pdfuaid:part>", packet, StringComparison.Ordinal);
        Assert.DoesNotContain("<pdfaid:part>", packet, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfUA_OutputIsTaggedWithDisplayDocTitle()
    {
        var reader = BuildTestSupport.Read(Author(ua: true));
        AssertTagged(reader);

        var catalog = Catalog(reader);
        Assert.True(catalog.TryGetValue("ViewerPreferences", out var preferencesObject), "catalog has /ViewerPreferences");
        var preferences = Assert.IsType<DictionaryObject>(reader.Resolve(preferencesObject!));
        Assert.True(((BooleanObject)reader.Resolve(preferences["DisplayDocTitle"])).Value, "DisplayDocTitle must be true");
    }

    [Fact]
    public void PdfUA_Catalog_HasLang()
    {
        var catalog = Catalog(BuildTestSupport.Read(Author(ua: true)));
        var lang = Assert.IsType<StringObject>(catalog["Lang"]);
        Assert.Equal("en-US", lang.Value);
    }

    [Fact]
    public void PdfUA_WithoutLanguage_Throws()
    {
        var builder = Author(ua: true);
        builder.Language = null;

        var exception = Record.Exception(() => builder.ToArray());

        Assert.NotNull(exception);
        Assert.Contains("Language", exception!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfUA_WithPdfA_Xmp_DeclaresPdfuaidExtensionSchema()
    {
        var packet = MetadataPacket(BuildTestSupport.Read(Author(PdfAConformance.PdfA2A, ua: true)));
        Assert.Contains("<pdfaSchema:prefix>pdfuaid</pdfaSchema:prefix>", packet, StringComparison.Ordinal);
        Assert.Contains("<pdfaSchema:namespaceURI>http://www.aiim.org/pdfua/ns/id/</pdfaSchema:namespaceURI>", packet, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfA4_Trailer_HasNoInfo()
    {
        var reader = BuildTestSupport.Read(Author(PdfAConformance.PdfA4));
        Assert.False(reader.Trailer.ContainsKey("Info"), "PDF/A-4 forbids the trailer /Info key");
    }

    [Fact]
    public void PdfUA_Alone_EmitsNoPdfAMachinery()
    {
        var catalog = Catalog(BuildTestSupport.Read(Author(ua: true)));
        Assert.False(catalog.ContainsKey("OutputIntents"), "PDF/UA alone requires no /OutputIntents");
    }

    [Fact]
    public void PdfUA_ComposesWithPdfA2A()
    {
        var reader = BuildTestSupport.Read(Author(PdfAConformance.PdfA2A, ua: true));

        var packet = MetadataPacket(reader);
        Assert.Contains("<pdfaid:part>2</pdfaid:part>", packet, StringComparison.Ordinal);
        Assert.Contains("<pdfaid:conformance>A</pdfaid:conformance>", packet, StringComparison.Ordinal);
        Assert.Contains("<pdfuaid:part>1</pdfuaid:part>", packet, StringComparison.Ordinal);

        AssertTagged(reader);
        AssertSrgbOutputIntent(reader);
    }

    [Fact]
    public void PdfUA_Base14FontByName_ThrowsActionable()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Hello", "Helvetica");
        builder.PdfUA = true;

        var exception = Record.Exception(() => builder.ToArray());

        Assert.NotNull(exception);
        Assert.Contains("PDF/UA", exception!.Message, StringComparison.Ordinal);
        Assert.Contains("Helvetica", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(PdfAConformance.PdfA2B)]
    [InlineData(PdfAConformance.PdfA4)]
    public void NewLevels_ContentSurvives_ExtractText(PdfAConformance level)
    {
        var document = BuildTestSupport.Reload(Author(level));
        Assert.Contains("Hello conformance", document.Pages[0].ExtractText(), StringComparison.Ordinal);
    }
}

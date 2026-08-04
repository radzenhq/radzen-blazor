#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class ConformanceProfilesTests
{
    private static string EmitAuthored((Document Document, DocumentRenderer Renderer) authored)
        => Emit(authored.Renderer.Render(authored.Document));

    private static byte[] RenderAuthored((Document Document, DocumentRenderer Renderer) authored)
        => authored.Renderer.ToArray(authored.Document);

    private static (Document Document, DocumentRenderer Renderer) Author(PdfAConformance conformance = PdfAConformance.None, bool ua = false)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);

        document.Info.Title = "Conformance profiles";
        document.Info.Author = "Radzen Ltd";

        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Hello conformance", BuildTestSupport.Latin);

        var renderer = new DocumentRenderer();
        renderer.Conformance = conformance;
        renderer.Accessibility = ua ? PdfUaConformance.PdfUa1 : PdfUaConformance.None;
        if (ua)
        {
            document.Language = "en-US";
        }

        return (document, renderer);
    }

    private static string Catalog(string emission) => Line(emission, "/Type /Catalog");

    private static string Trailer(string emission) => Line(emission, "/Root ");

    private static string MetadataPacket(string emission)
    {
        var reference = Shaped("catalog", @"/Metadata (\d+) 0 R", Catalog(emission));
        var metadata = IndirectObject(emission, reference.Groups[1].Value);

        Carries("metadata stream", "/Type /Metadata", metadata);
        Carries("metadata stream", "/Subtype /XML", metadata);
        Lacks("metadata stream", "/Filter", metadata);
        return metadata;
    }

    private static void AssertSrgbOutputIntent(string emission)
    {
        var intents = References("catalog", "OutputIntents", 1, Catalog(emission));
        var intent = IndirectObject(emission, intents[0]);

        Carries("output intent", "/S /GTS_PDFA1", intent);

        var reference = Shaped("output intent", @"/DestOutputProfile (\d+) 0 R", intent);
        var profile = IndirectObject(emission, reference.Groups[1].Value);

        Assert.Equal(3, NumberIn(profile, "N"));
        Shaped("destination output profile", @"stream\n[\s\S]{36}acsp", profile);
    }

    private static void AssertTagged(string emission)
    {
        var catalog = Catalog(emission);

        Carries("catalog", "/MarkInfo << /Marked true >>", catalog);

        var reference = Shaped("catalog", @"/StructTreeRoot (\d+) 0 R", catalog);
        Carries(
            "structure tree root",
            "/Type /StructTreeRoot",
            IndirectObject(emission, reference.Groups[1].Value));
    }

    [Theory]
    [InlineData(PdfAConformance.PdfA2B, 2, "B")]
    [InlineData(PdfAConformance.PdfA2A, 2, "A")]
    [InlineData(PdfAConformance.PdfA4E, 4, "E")]
    public void PdfALevel_Xmp_HasPartAndConformance(PdfAConformance level, int part, string conformance)
    {
        var emission = EmitAuthored(Author(level));

        var packet = MetadataPacket(emission);
        Carries("XMP packet", $"<pdfaid:part>{part}</pdfaid:part>", packet);
        Carries("XMP packet", $"<pdfaid:conformance>{conformance}</pdfaid:conformance>", packet);

        AssertSrgbOutputIntent(emission);
        Carries("trailer", "/ID [", Trailer(emission));
    }

    [Fact]
    public void PdfA4_Xmp_HasPart4RevAndNoConformanceLetter()
    {
        var emission = EmitAuthored(Author(PdfAConformance.PdfA4));

        var packet = MetadataPacket(emission);
        Carries("XMP packet", "<pdfaid:part>4</pdfaid:part>", packet);
        Carries("XMP packet", "<pdfaid:rev>2020</pdfaid:rev>", packet);
        Lacks("XMP packet", "<pdfaid:conformance>", packet);

        AssertSrgbOutputIntent(emission);
    }

    [Theory]
    [InlineData(PdfAConformance.PdfA4)]
    [InlineData(PdfAConformance.PdfA4E)]
    [InlineData(PdfAConformance.PdfA4F)]
    public void PdfA4Levels_Catalog_DeclaresVersion20(PdfAConformance level)
    {
        var (document, renderer) = Author(level);
        var rendered = renderer.Render(document);
        if (level == PdfAConformance.PdfA4F)
        {
            rendered.Attachments.Add("data.xml", Encoding.UTF8.GetBytes("<data/>"), AttachmentRelationship.Data, "text/xml");
        }

        Carries("catalog", "/Version /2.0", Catalog(Emit(rendered)));
    }

    [Theory]
    [InlineData(PdfAConformance.PdfA4, "%PDF-2.0")]
    [InlineData(PdfAConformance.PdfA4E, "%PDF-2.0")]
    [InlineData(PdfAConformance.PdfA2B, "%PDF-1.7")]
    [InlineData(PdfAConformance.PdfA3B, "%PDF-1.7")]
    public void FileHeader_MatchesConformancePart(PdfAConformance level, string expectedHeader)
    {
        var bytes = RenderAuthored(Author(level));
        Assert.Equal(expectedHeader, Encoding.ASCII.GetString(bytes, 0, expectedHeader.Length));
    }

    [Fact]
    public void PdfA2A_OutputIsTagged()
    {
        AssertTagged(EmitAuthored(Author(PdfAConformance.PdfA2A)));
    }

    [Fact]
    public void PdfA4F_WithAttachment_HasConformanceF()
    {
        var (document, renderer) = Author(PdfAConformance.PdfA4F);
        var rendered = renderer.Render(document);
        rendered.Attachments.Add("data.xml", Encoding.UTF8.GetBytes("<data/>"), AttachmentRelationship.Data, "text/xml");

        var packet = MetadataPacket(Emit(rendered));
        Carries("XMP packet", "<pdfaid:part>4</pdfaid:part>", packet);
        Carries("XMP packet", "<pdfaid:rev>2020</pdfaid:rev>", packet);
        Carries("XMP packet", "<pdfaid:conformance>F</pdfaid:conformance>", packet);
    }

    [Fact]
    public void PdfA4F_WithoutAttachment_Throws()
    {
        var exception = Record.Exception(() => RenderAuthored(Author(PdfAConformance.PdfA4F)));

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
        var (document, renderer) = Author(level);
        var rendered = renderer.Render(document);
        rendered.Attachments.Add("data.xml", Encoding.UTF8.GetBytes("<data/>"), AttachmentRelationship.Data, "text/xml");

        var exception = Record.Exception(rendered.ToArray);

        Assert.NotNull(exception);
        Assert.Contains(level.ToString(), exception!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfUA_Xmp_HasPdfuaidPart1()
    {
        var packet = MetadataPacket(EmitAuthored(Author(ua: true)));

        Carries("XMP packet", "xmlns:pdfuaid=\"http://www.aiim.org/pdfua/ns/id/\"", packet);
        Carries("XMP packet", "<pdfuaid:part>1</pdfuaid:part>", packet);
        Lacks("XMP packet", "<pdfaid:part>", packet);
    }

    [Fact]
    public void PdfUA_OutputIsTaggedWithDisplayDocTitle()
    {
        var emission = EmitAuthored(Author(ua: true));
        AssertTagged(emission);

        Carries("catalog /ViewerPreferences", "/DisplayDocTitle true", Catalog(emission));
    }

    [Fact]
    public void PdfUA_Catalog_HasLang()
    {
        Carries("catalog", "/Lang (en-US)", Catalog(EmitAuthored(Author(ua: true))));
    }

    [Fact]
    public void PdfUA_WithoutLanguage_Throws()
    {
        var (document, renderer) = Author(ua: true);
        document.Language = null;

        var exception = Record.Exception(() => renderer.ToArray(document));

        Assert.NotNull(exception);
        Assert.Contains("Language", exception!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfUA_WithPdfA_Xmp_DeclaresPdfuaidExtensionSchema()
    {
        var packet = MetadataPacket(EmitAuthored(Author(PdfAConformance.PdfA2A, ua: true)));

        Carries("XMP packet", "<pdfaSchema:prefix>pdfuaid</pdfaSchema:prefix>", packet);
        Carries("XMP packet", "<pdfaSchema:namespaceURI>http://www.aiim.org/pdfua/ns/id/</pdfaSchema:namespaceURI>", packet);
    }

    [Fact]
    public void PdfA4_Trailer_HasNoInfo()
    {
        Lacks("trailer", "/Info", Trailer(EmitAuthored(Author(PdfAConformance.PdfA4))));
    }

    [Fact]
    public void PdfUA_Alone_EmitsNoPdfAMachinery()
    {
        Lacks("catalog", "/OutputIntents", Catalog(EmitAuthored(Author(ua: true))));
    }

    [Fact]
    public void PdfUA_ComposesWithPdfA2A()
    {
        var emission = EmitAuthored(Author(PdfAConformance.PdfA2A, ua: true));

        var packet = MetadataPacket(emission);
        Carries("XMP packet", "<pdfaid:part>2</pdfaid:part>", packet);
        Carries("XMP packet", "<pdfaid:conformance>A</pdfaid:conformance>", packet);
        Carries("XMP packet", "<pdfuaid:part>1</pdfuaid:part>", packet);

        AssertTagged(emission);
        AssertSrgbOutputIntent(emission);
    }

    [Fact]
    public void PdfUA_Base14FontByName_ThrowsActionable()
    {
        var document = new Document();
        var renderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Hello", "Helvetica");

        var exception = Record.Exception(() => renderer.ToArray(document));

        Assert.NotNull(exception);
        Assert.Contains("PDF/UA", exception!.Message, StringComparison.Ordinal);
        Assert.Contains("Helvetica", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(PdfAConformance.PdfA2B)]
    [InlineData(PdfAConformance.PdfA4)]
    public void NewLevels_ContentSurvives_ExtractText(PdfAConformance level)
    {
        var authored = Author(level);
        var document = BuildTestSupport.Reload(authored.Document, authored.Renderer);
        Assert.Contains("Hello conformance", document.Pages[0].ExtractText(), StringComparison.Ordinal);
    }
}

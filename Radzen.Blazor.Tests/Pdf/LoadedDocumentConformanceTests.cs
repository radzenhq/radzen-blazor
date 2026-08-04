#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using Xunit;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class LoadedDocumentConformanceTests
{
    private static Document Authored()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Loaded conformance";
        BuildTestSupport.RegisterLatin(document);
        BuildTestSupport.AddText(document.Sections.Add(), "Body", BuildTestSupport.Latin);
        return document;
    }

    private static PortableDocument LoadedFromEmbeddedFontSource()
    {
        using var buffer = new MemoryStream(new DocumentRenderer().ToArray(Authored()));
        return PortableDocument.LoadFromStream(buffer);
    }

    [Fact]
    public void LoadedDocument_ResavedAsPdfA2b_CarriesIdentificationAndOutputIntent()
    {
        var loaded = LoadedFromEmbeddedFontSource();

        loaded.Conformance = PdfAConformance.PdfA2B;

        var emission = Emit(loaded);
        Assert.Contains("pdfaid:part", emission, StringComparison.Ordinal);

        Carries("catalog", "/OutputIntents [", Line(emission, "/Type /Catalog"));
        Carries("trailer", "/ID [", Line(emission, "/Root "));
    }

    [Fact]
    public void LoadedDocument_ResavedAsPdfA3b_CarriesIdentification()
    {
        var loaded = LoadedFromEmbeddedFontSource();

        loaded.Conformance = PdfAConformance.PdfA3B;

        Assert.Contains("pdfaid:part", Encoding.Latin1.GetString(loaded.ToArray()), StringComparison.Ordinal);
    }

    [Fact]
    public void LoadedDocument_SetToLevelA_ThrowsAtSet()
    {
        var source = new PortableDocument();
        source.Pages.Add().Content.Add(new TextContent("Untagged", 72, 700) { Font = new Radzen.Documents.Fonts.Font { Size = 12 } });
        var loaded = InterpreterTestSupport.Load(source.ToArray());
        Assert.False(loaded.HasPreservableStructureGraph);

        var error = Assert.Throws<InvalidOperationException>(() => loaded.Conformance = PdfAConformance.PdfA2A);

        Assert.Contains("Tagged PDF conformance level", error.Message, StringComparison.Ordinal);
        Assert.Equal(PdfAConformance.None, loaded.Conformance);
    }

    [Fact]
    public void LoadedTaggedDocument_SetToLevelA_IsAccepted()
    {
        using var buffer = new MemoryStream(
            new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 }.ToArray(Authored()));
        var loaded = PortableDocument.LoadFromStream(buffer);
        Assert.True(loaded.HasPreservableStructureGraph);

        loaded.Conformance = PdfAConformance.PdfA2A;

        Assert.Equal(PdfAConformance.PdfA2A, loaded.Conformance);
    }

    [Fact]
    public void LoadedDocument_WithUnembeddedFont_IsRefusedAtSave()
    {
        var source = new PortableDocument();
        source.Pages.Add().Content.Add(new TextContent("Standard 14", 72, 700) { Font = new Radzen.Documents.Fonts.Font { Size = 12 } });
        var loaded = InterpreterTestSupport.Load(source.ToArray());

        loaded.Conformance = PdfAConformance.PdfA2B;

        var error = Assert.Throws<InvalidOperationException>(() => loaded.ToArray());
        Assert.Contains("requires every font to be embedded", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LanguageAndRoleMap_AreNotPubliclySettable()
    {
        Assert.False(typeof(PortableDocument).GetProperty(nameof(PortableDocument.Language))!.SetMethod!.IsPublic);
        Assert.False(typeof(PortableDocument).GetProperty(nameof(PortableDocument.RoleMap))!.SetMethod!.IsPublic);
    }
}

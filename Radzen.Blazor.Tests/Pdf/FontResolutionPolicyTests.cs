#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Fonts;

using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Core;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class FontResolutionPolicyTests
{
    private static byte[] RenderAuthored((Document Document, DocumentRenderer Renderer) authored)
        => authored.Renderer.ToArray(authored.Document);

    private static (Document Document, DocumentRenderer Renderer) Author(PdfAConformance conformance = PdfAConformance.None)
    {
        var document = new Document();
        var renderer = new DocumentRenderer { Conformance = conformance };
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Body", BuildTestSupport.Latin);
        return (document, renderer);
    }

    [Fact]
    public void LaidOutText_UnknownFamily_StillFailsAtMeasure()
    {
        var document = new Document();
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Body", "Arial");

        var renderer = new DocumentRenderer();
        var exception = Record.Exception(() => renderer.ToArray(document));

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("No font is registered for family 'Arial'", exception!.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Arial")]
    [InlineData("Times New Roman")]
    [InlineData("Courier New")]
    public void GeneratedWatermark_UnknownFamily_ThrowsWithEmbedRemedy(string family)
    {
        var document = new Document();
        var section = document.Sections.Add();
        var watermark = new Watermark { Text = "DRAFT" };
        watermark.Font.Family = family;
        section.Watermark = watermark;
        BuildTestSupport.AddText(section, "Body", "Helvetica");

        var exception = Record.Exception(() => new DocumentRenderer().ToArray(document));

        Assert.IsType<NotSupportedException>(exception);
        Assert.Contains(family, exception!.Message, StringComparison.Ordinal);
        Assert.Contains("Document.Fonts", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("base-14", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedFormField_UnknownFamily_ThrowsWithBase14Remedy()
    {
        var document = new Document();
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Body", "Helvetica");
        var rendered = new DocumentRenderer().Render(document);
        rendered.FormFields.Add(Field(new Font { Family = "Arial", Size = 12 }));

        var exception = Record.Exception(rendered.ToArray);

        Assert.IsType<NotSupportedException>(exception);
        Assert.Contains("Arial", exception!.Message, StringComparison.Ordinal);
        Assert.Contains("base-14", exception.Message, StringComparison.Ordinal);
    }

    private static TextFieldDefinition Field(Font font) => new("Name")
    {
        PageIndex = 0,
        X = 72,
        Y = 700,
        Width = 200,
        Height = 20,
        Value = "hello",
        Font = font,
    };

    [Fact]
    public void Overlay_UnknownFamily_ThrowsWithBase14Remedy()
    {
        var authored = Author();
        var document = authored.Renderer.Render(authored.Document);
        document.Pages[0].Content.Add(
            new TextContent("STAMP", Unit.FromPoint(72), Unit.FromPoint(650))
            {
                Font = new Font { Family = "Arial", Size = 12 },
            });

        var exception = Record.Exception(() => document.ToArray());

        Assert.IsType<NotSupportedException>(exception);
        Assert.Contains("Arial", exception!.Message, StringComparison.Ordinal);
        Assert.Contains("base-14", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyFamily_KeepsDefaultFont()
    {
        var authored = Author();
        var document = authored.Renderer.Render(authored.Document);
        document.Pages[0].Content.Add(new TextContent("STAMP", Unit.FromPoint(72), Unit.FromPoint(650)));

        Assert.NotEmpty(document.ToArray());
    }

    [Fact]
    public void Overlay_RegisteredEmbeddedFamily_ThrowsRatherThanSubstituting()
    {
        var authored = Author();
        var document = authored.Renderer.Render(authored.Document);
        document.Pages[0].Content.Add(
            new TextContent("STAMP", Unit.FromPoint(72), Unit.FromPoint(650))
            {
                Font = new Font { Family = BuildTestSupport.Latin, Size = 12 },
            });

        var exception = Record.Exception(() => document.ToArray());

        Assert.IsType<NotSupportedException>(exception);
        Assert.Contains(BuildTestSupport.Latin, exception!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FormField_RegisteredEmbeddedFamily_ThrowsRatherThanSubstituting()
    {
        var (document, renderer) = Author();
        var rendered = renderer.Render(document);
        rendered.FormFields.Add(Field(new Font { Family = BuildTestSupport.Latin, Size = 12 }));

        var exception = Record.Exception(rendered.ToArray);

        Assert.IsType<NotSupportedException>(exception);
        Assert.Contains(BuildTestSupport.Latin, exception!.Message, StringComparison.Ordinal);
        Assert.Contains("cannot embed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfA3B_TextFormField_RejectsUnembeddedBase14()
    {
        var (document, renderer) = Author(PdfAConformance.PdfA3B);
        var rendered = renderer.Render(document);
        rendered.FormFields.Add(Field(new Font { Family = "Helvetica", Size = 12 }));

        var exception = Record.Exception(rendered.ToArray);

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("PDF/A", exception!.Message, StringComparison.Ordinal);
        Assert.Contains("Helvetica", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfA3B_Watermark_RejectsUnembeddedBase14()
    {
        var document = new Document();
        var renderer = new DocumentRenderer { Conformance = PdfAConformance.PdfA3B };
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        section.Watermark = new Watermark { Text = "DRAFT" };
        BuildTestSupport.AddText(section, "Body", BuildTestSupport.Latin);

        var exception = Record.Exception(() => renderer.ToArray(document));

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("PDF/A", exception!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfUA_Base14_FailsDuringRenderMaterialization()
    {
        var document = new Document { Language = "en-US" };
        var renderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        document.Info.Title = "Title";
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Body", "Helvetica");

        var exception = Record.Exception(() => renderer.Render(document));

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("PDF/UA", exception!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfA3B_CleanDocument_EmitsNoUnembeddedType1()
    {
        var emission = Encoding.Latin1.GetString(RenderAuthored(Author(PdfAConformance.PdfA3B)));

        Lacks("emission", "/BaseFont /Helvetica ", emission);
    }

    [Fact]
    public void StandardFonts_DeriveThePostScriptNameFromTheNeutralFace()
    {
        (BuiltInFontFamily Family, bool Bold, bool Italic, string Expected)[] cases =
        [
            (BuiltInFontFamily.Sans, false, false, "Helvetica"),
            (BuiltInFontFamily.Sans, true, false, "Helvetica-Bold"),
            (BuiltInFontFamily.Sans, false, true, "Helvetica-Oblique"),
            (BuiltInFontFamily.Sans, true, true, "Helvetica-BoldOblique"),
            (BuiltInFontFamily.Monospace, false, false, "Courier"),
            (BuiltInFontFamily.Monospace, true, false, "Courier-Bold"),
            (BuiltInFontFamily.Monospace, false, true, "Courier-Oblique"),
            (BuiltInFontFamily.Monospace, true, true, "Courier-BoldOblique"),
            (BuiltInFontFamily.Serif, false, false, "Times-Roman"),
            (BuiltInFontFamily.Serif, true, false, "Times-Bold"),
            (BuiltInFontFamily.Serif, false, true, "Times-Italic"),
            (BuiltInFontFamily.Serif, true, true, "Times-BoldItalic"),
            (BuiltInFontFamily.Symbol, false, false, "Symbol"),
            (BuiltInFontFamily.ZapfDingbats, false, false, "ZapfDingbats"),
        ];

        foreach (var (family, bold, italic, expected) in cases)
        {
            Assert.Equal(
                expected,
                StandardFonts.PostScriptName(new CapturedBuiltInFace(family, bold, italic, default)));
        }
    }

    [Fact]
    public void EveryBuiltInFace_RoundTripsThroughTheNeutralFace()
    {
        foreach (var entry in BuiltInFontData.Fonts)
        {
            var metrics = BuiltInFontMetrics.Resolve(new Font { Family = entry.FontName })!;

            Assert.Equal(entry.FontName, StandardFonts.PostScriptName(metrics.Face()));
        }
    }
}

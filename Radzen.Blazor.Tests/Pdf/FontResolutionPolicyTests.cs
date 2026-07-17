#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// The three decisions at the font resolve seam: an unknown family throws rather than
// silently rendering Helvetica, an embedded face in a context that cannot embed throws
// rather than silently degrading to Helvetica, and a conforming document's base-14
// rejection covers every generated stream, not just the ones ConformanceWriter scans.
public class FontResolutionPolicyTests
{
    private static DocumentBuilder Author(PdfAConformance conformance = PdfAConformance.None)
    {
        var builder = new DocumentBuilder { Conformance = conformance };
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Body", BuildTestSupport.Latin);
        return builder;
    }

    // B1: laid-out text never reaches the resolve seam - measuring an unregistered family
    // already fails first, and has always done so. Pinned to keep that error from regressing
    // into the seam's, which would report a different type for the same mistake.
    [Fact]
    public void LaidOutText_UnknownFamily_StillFailsAtMeasure()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Body", "Arial");

        var exception = Record.Exception(() => builder.ToArray());

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("No font is registered for family 'Arial'", exception!.Message, StringComparison.Ordinal);
    }

    // B1: the generated paths that skip measure and reach the seam. Both emitted
    // /BaseFont /Helvetica and saved cleanly before this policy landed. The common Windows
    // families fall through too: Base14Metrics matches only the bare lowercase names.
    [Theory]
    [InlineData("Arial")]
    [InlineData("Times New Roman")]
    [InlineData("Courier New")]
    public void GeneratedWatermark_UnknownFamily_ThrowsWithEmbedRemedy(string family)
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var watermark = new Watermark { Text = "DRAFT" };
        watermark.Font.Name = family;
        section.Watermark = watermark;
        BuildTestSupport.AddText(section, "Body", "Helvetica");

        var exception = Record.Exception(() => builder.ToArray());

        Assert.IsType<NotSupportedException>(exception);
        Assert.Contains(family, exception!.Message, StringComparison.Ordinal);
        Assert.Contains("DocumentBuilder.Fonts", exception.Message, StringComparison.Ordinal);
    }

    // A generated form field's appearance stream cannot embed, so its remedy is the base-14 one.
    [Fact]
    public void GeneratedFormField_UnknownFamily_ThrowsWithBase14Remedy()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Body", "Helvetica");
        builder.FormFields.Add(Field(new Font { Name = "Arial", Size = 12 }));

        var exception = Record.Exception(() => builder.ToArray());

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

    // B1: overlay/loaded world - a different remedy, since it cannot embed.
    [Fact]
    public void Overlay_UnknownFamily_ThrowsWithBase14Remedy()
    {
        var document = Author().Build();
        document.Pages[0].Content.Add(
            new TextContent("STAMP", Unit.FromPoint(72), Unit.FromPoint(650))
            {
                Font = new Font { Name = "Arial", Size = 12 },
            });

        var exception = Record.Exception(() => document.ToArray());

        Assert.IsType<NotSupportedException>(exception);
        Assert.Contains("Arial", exception!.Message, StringComparison.Ordinal);
        Assert.Contains("base-14", exception.Message, StringComparison.Ordinal);
    }

    // B1: an empty Font.Name is the documented default-font path, not a substitution.
    [Fact]
    public void EmptyFamily_KeepsDefaultFont()
    {
        var document = Author().Build();
        document.Pages[0].Content.Add(new TextContent("STAMP", Unit.FromPoint(72), Unit.FromPoint(650)));

        Assert.NotEmpty(document.ToArray());
    }

    // B2: an embedded (registered sfnt) face cannot be reached from a non-embedding
    // context. Closing that gap is a feature; until then it must not become Helvetica.
    [Fact]
    public void Overlay_RegisteredEmbeddedFamily_ThrowsRatherThanSubstituting()
    {
        var document = Author().Build();
        document.Pages[0].Content.Add(
            new TextContent("STAMP", Unit.FromPoint(72), Unit.FromPoint(650))
            {
                Font = new Font { Name = BuildTestSupport.Latin, Size = 12 },
            });

        var exception = Record.Exception(() => document.ToArray());

        Assert.IsType<NotSupportedException>(exception);
        Assert.Contains(BuildTestSupport.Latin, exception!.Message, StringComparison.Ordinal);
    }

    // B2: a form field's appearance stream is the other non-embedding context, and it must
    // reject a registered face for the same reason - not silently draw it as Helvetica.
    [Fact]
    public void FormField_RegisteredEmbeddedFamily_ThrowsRatherThanSubstituting()
    {
        var builder = Author();
        builder.FormFields.Add(Field(new Font { Name = BuildTestSupport.Latin, Size = 12 }));

        var exception = Record.Exception(() => builder.ToArray());

        Assert.IsType<NotSupportedException>(exception);
        Assert.Contains(BuildTestSupport.Latin, exception!.Message, StringComparison.Ordinal);
        Assert.Contains("cannot embed", exception.Message, StringComparison.Ordinal);
    }

    // B3: the observed leak - a PDF/A document with a text form field saved cleanly with
    // an unembedded /Helvetica Type1 inside the widget's /AP form XObject.
    [Fact]
    public void PdfA3B_TextFormField_RejectsUnembeddedBase14()
    {
        var builder = Author(PdfAConformance.PdfA3B);
        builder.FormFields.Add(Field(new Font { Name = "Helvetica", Size = 12 }));

        var exception = Record.Exception(() => builder.ToArray());

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("PDF/A", exception!.Message, StringComparison.Ordinal);
        Assert.Contains("Helvetica", exception.Message, StringComparison.Ordinal);
    }

    // B3: the same leak through a watermark, which is neither generated.Fonts nor
    // a TextContent the ConformanceWriter scan can see.
    [Fact]
    public void PdfA3B_Watermark_RejectsUnembeddedBase14()
    {
        var builder = new DocumentBuilder { Conformance = PdfAConformance.PdfA3B };
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        section.Watermark = new Watermark { Text = "DRAFT" };
        BuildTestSupport.AddText(section, "Body", BuildTestSupport.Latin);

        var exception = Record.Exception(() => builder.ToArray());

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("PDF/A", exception!.Message, StringComparison.Ordinal);
    }

    // The gate reads PdfUA only in the non-embedding scope, never in the generator's: a
    // PDF/UA-only document must still reach a built Document and fail at save, not at Build().
    [Fact]
    public void PdfUA_Base14_StillFailsAtSaveNotBuild()
    {
        var builder = new DocumentBuilder { PdfUA = true, Language = "en-US" };
        builder.Info.Title = "Title";
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Body", "Helvetica");

        var document = builder.Build();

        var exception = Record.Exception(() => document.ToArray());

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("PDF/UA", exception!.Message, StringComparison.Ordinal);
    }

    // Documents the leak concretely: no unembedded base-14 Type1 survives in the bytes.
    [Fact]
    public void PdfA3B_CleanDocument_EmitsNoUnembeddedType1()
    {
        var bytes = Author(PdfAConformance.PdfA3B).ToArray();
        var text = Encoding.Latin1.GetString(bytes);

        Assert.DoesNotContain("/BaseFont /Helvetica", text, StringComparison.Ordinal);
    }
}

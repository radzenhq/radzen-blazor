#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

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

    [Fact]
    public void EmptyFamily_KeepsDefaultFont()
    {
        var document = Author().Build();
        document.Pages[0].Content.Add(new TextContent("STAMP", Unit.FromPoint(72), Unit.FromPoint(650)));

        Assert.NotEmpty(document.ToArray());
    }

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

    [Fact]
    public void PdfA3B_CleanDocument_EmitsNoUnembeddedType1()
    {
        var bytes = Author(PdfAConformance.PdfA3B).ToArray();
        var text = Encoding.Latin1.GetString(bytes);

        Assert.DoesNotContain("/BaseFont /Helvetica", text, StringComparison.Ordinal);
    }
}

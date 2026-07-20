#nullable enable
using System.Text;
using System.IO;
using Xunit;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Blazor.Pdf.Tests;

public class Base14FallbackKerningTests
{
    private static string Content(bool enableKerning)
    {
        var builder = new DocumentBuilder();
        builder.Fonts.EnableKerning = enableKerning;
        BuildTestSupport.RegisterLatin(builder);
        builder.Fonts.SetFallback(BuildTestSupport.Latin);

        var section = builder.Sections.Add();
        section.Blocks.AddParagraph("АТ");

        var reader = BuildTestSupport.Read(builder);
        var page = BuildTestSupport.PageLeaves(reader)[0].Page;
        return Encoding.Latin1.GetString(BuildTestSupport.Content(reader, page));
    }

    [Fact]
    public void Fallback_Run_Applies_Kerning_When_Enabled()
    {
        Assert.Contains("TJ", Content(enableKerning: true));
    }

    [Fact]
    public void Fallback_Run_Has_No_Kerning_When_Disabled()
    {
        Assert.DoesNotContain("TJ", Content(enableKerning: false));
    }

    [Fact]
    public void MeasureBase14_FallbackRunIncludesTheKerningPaintedByEmitter()
    {
        var face = SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf"));
        var font = new Font { Size = 12 };
        var fonts = new FontCollection();
        fonts.Register(BuildTestSupport.Latin, new MemoryStream(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        fonts.SetFallback(BuildTestSupport.Latin);
        var plain = fonts.MeasureText("АТ", font);
        var expectedKern = face.GetKerning(face.GetGlyphId('А'), face.GetGlyphId('Т')) * font.Size / face.UnitsPerEm;

        fonts.EnableKerning = true;

        Assert.NotEqual(0, expectedKern);
        Assert.Equal(plain + expectedKern, fonts.MeasureText("АТ", font), 9);
    }

    [Fact]
    public void MeasureBase14_FallbackRunStaysUnkernedWhenKerningIsDisabled()
    {
        var fonts = new FontCollection();
        fonts.Register(BuildTestSupport.Latin, new MemoryStream(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        fonts.SetFallback(BuildTestSupport.Latin);
        var font = new Font { Size = 12 };

        Assert.Equal(fonts.MeasureText("А", font) + fonts.MeasureText("Т", font), fonts.MeasureText("АТ", font), 9);
    }

    [Fact]
    public void ClassifyBase14Glyph_IsTheSingleHomeSharedByMeasureAndEmit()
    {
        var fonts = new FontCollection();
        fonts.Register(BuildTestSupport.Latin, new MemoryStream(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        fonts.SetFallback(BuildTestSupport.Latin);

        Assert.Equal(Base14GlyphKind.WinAnsi, fonts.ClassifyBase14Glyph('A', out var code, out _, out _));
        Assert.Equal((byte)'A', code);

        Assert.Equal(Base14GlyphKind.Fallback, fonts.ClassifyBase14Glyph('А', out _, out var face, out var glyph));
        Assert.NotNull(face);
        Assert.NotEqual(0, glyph);

        Assert.Equal(Base14GlyphKind.Missing, fonts.ClassifyBase14Glyph(0x1F600, out _, out _, out _));
    }
}

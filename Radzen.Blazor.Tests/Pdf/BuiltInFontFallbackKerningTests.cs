#nullable enable
using System.Text;
using System.IO;
using Xunit;
using Radzen.Documents.Pdf;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents;
using Document = Radzen.Documents.Document;
using Radzen.Documents.Fonts;

namespace Radzen.Blazor.Pdf.Tests;

public class BuiltInFontFallbackKerningTests
{
    private static string Content(bool enableKerning)
    {
        var document = new Document();
        document.Fonts.EnableKerning = enableKerning;
        BuildTestSupport.RegisterLatin(document);
        document.Fonts.SetFallback(BuildTestSupport.Latin);

        var section = document.Sections.Add();
        section.Blocks.AddParagraph("АТ");

        var reader = BuildTestSupport.Read(document);
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
    public void MeasureBuiltIn_FallbackRunIncludesTheKerningPaintedByEmitter()
    {
        var face = SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf"));
        var font = new Font { Size = 12 };
        var fonts = new FontCollection();
        fonts.Register(BuildTestSupport.Latin, new MemoryStream(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        fonts.SetFallback(BuildTestSupport.Latin);
        var plain = fonts.MeasureText("АТ", font);
        var expectedKern = face.GetKerning(face.GetGlyphId('А'), face.GetGlyphId('Т')) * font.Size!.Value.Point / face.UnitsPerEm;

        fonts.EnableKerning = true;

        Assert.NotEqual(0, expectedKern);
        Assert.Equal(plain + expectedKern, fonts.MeasureText("АТ", font), 9);
    }

    [Fact]
    public void MeasureBuiltIn_FallbackRunStaysUnkernedWhenKerningIsDisabled()
    {
        var fonts = new FontCollection();
        fonts.Register(BuildTestSupport.Latin, new MemoryStream(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        fonts.SetFallback(BuildTestSupport.Latin);
        var font = new Font { Size = 12 };

        Assert.Equal(fonts.MeasureText("А", font) + fonts.MeasureText("Т", font), fonts.MeasureText("АТ", font), 9);
    }

    [Fact]
    public void ClassifyBuiltInGlyph_UsesMetricsBeforeTheFallbackChain()
    {
        var fonts = new FontCollection();
        fonts.Register(BuildTestSupport.Latin, new MemoryStream(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        fonts.SetFallback(BuildTestSupport.Latin);
        var metrics = BuiltInFontMetrics.Resolve(new Font())!;

        Assert.Equal(BuiltInGlyphKind.BuiltIn, fonts.ClassifyBuiltInGlyph(metrics, 'A', out var width, out _, out _));
        Assert.True(width > 0);
        Assert.Equal(BuiltInGlyphKind.BuiltIn, fonts.ClassifyBuiltInGlyph(metrics, 'ﬁ', out _, out _, out _));

        Assert.Equal(BuiltInGlyphKind.Fallback, fonts.ClassifyBuiltInGlyph(metrics, 'А', out _, out var face, out var glyph));
        Assert.NotNull(face);
        Assert.NotEqual(0, glyph);

        Assert.Equal(BuiltInGlyphKind.Missing, fonts.ClassifyBuiltInGlyph(metrics, 0x1F600, out _, out _, out _));
    }
}

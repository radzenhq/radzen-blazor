using System;
using System.Collections.Generic;
using static Radzen.Documents.Pdf.Emit.GeneratorFontResolver;

namespace Radzen.Documents.Pdf.Emit;

// Plans a section watermark onto a page: shapes the text during page planning (so
// glyph usage lands in the document-wide subsets before compaction), decodes the
// image, and records a WatermarkDraw the generator serializes after all content.
internal sealed class WatermarkEmitter(FontCollection fonts, GeneratorFontResolver fontResolver, ImageStore imageStore)
{
    public void Plan(PagePlan plan, Watermark watermark)
    {
        var draw = new WatermarkDraw
        {
            CenterX = plan.Size.Width.Point / 2,
            CenterY = plan.Size.Height.Point / 2,
            Rotation = watermark.Rotation,
            ExtGState = watermark.Opacity < 1
                ? plan.RegisterExtGState(watermark.Opacity, watermark.Opacity)
                : null,
        };

        if (watermark.Image is { } image)
        {
            var generated = imageStore.Decode(image);
            var (width, height) = ImageDecoder.Measure(image, generated.Image, plan.Size.Width.Point);
            draw.Image = new ImageDraw
            {
                X = -width / 2,
                Y = -height / 2,
                Width = width,
                Height = height,
                Image = generated,
            };
            plan.UsedImages.Add(generated);
        }

        if (!string.IsNullOrEmpty(watermark.Text))
        {
            PlanText(plan, draw, watermark.Text, watermark.Font);
        }

        plan.Watermark = draw;
    }

    // Segments are laid out in the rotated coordinate system, centered on its origin:
    // the run starts at -width/2 with the baseline dropped so the glyph body straddles
    // the page center.
    private void PlanText(PagePlan plan, WatermarkDraw draw, string text, Font font)
    {
        var size = font.Size;
        var baseline = -size * 0.35;
        if (fonts.TryResolvePrimary(font, out var primary))
        {
            var x = -fonts.MeasureText(text, font) / 2;
            var i = 0;
            while (i < text.Length)
            {
                var (face, _) = fonts.ResolveGlyph(primary, CodePointAt(text, i));
                var generated = fontResolver.ResolveSfnt(face);
                var bytes = new List<byte>();
                var advance = 0.0;
                while (i < text.Length)
                {
                    var codepoint = CodePointAt(text, i);
                    var (candidate, gid) = fonts.ResolveGlyph(primary, codepoint);
                    if (candidate != face)
                    {
                        break;
                    }

                    generated.GidToUnicode.TryAdd(gid, codepoint);
                    bytes.Add((byte)(gid >> 8));
                    bytes.Add((byte)(gid & 0xFF));
                    advance += face.GetAdvanceWidth(gid) * size / face.UnitsPerEm;
                    i += codepoint > 0xFFFF ? 2 : 1;
                }

                plan.UsedFonts.Add(generated);
                draw.Texts.Add(new TextDraw
                {
                    X = x,
                    Baseline = baseline,
                    Size = size,
                    Color = font.Color,
                    Font = generated,
                    Bytes = [.. bytes],
                });
                x += advance;
            }
        }
        else
        {
            // Base-14 watermarks are WinAnsi-only; fail loud rather than silently dropping
            // unrepresentable codepoints (which would blank or mangle the watermark).
            foreach (var c in text)
            {
                if (!IsWinAnsi(c))
                {
                    throw new NotSupportedException(
                        $"Watermark text contains a character (U+{(int)c:X4}) not representable in the base-14 WinAnsi encoding; register a font that covers it.");
                }
            }

            var metrics = Fonts.Base14Metrics.Resolve(font) ?? Fonts.Base14Metrics.Resolve(new Font())!;
            var segment = text;
            var generated = fontResolver.ResolveBase14(font);
            plan.UsedFonts.Add(generated);
            draw.Texts.Add(new TextDraw
            {
                X = -metrics.MeasureString(segment, size) / 2,
                Baseline = baseline,
                Size = size,
                Color = font.Color,
                Font = generated,
                Bytes = EncodeWinAnsi(segment),
            });
        }
    }
}

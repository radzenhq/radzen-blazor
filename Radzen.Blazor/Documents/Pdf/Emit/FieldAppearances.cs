using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;
using System.Globalization;

using Radzen.Documents.Pdf.Content;
namespace Radzen.Documents.Pdf.Emit;


// Builds AcroForm widget appearance streams (/AP /N) and the geometry shared with
// form flattening, so a filled, created and flattened field all render identically.
internal static class FieldAppearances
{
    // Fallback appearance size when the /DA carries none (a /DA size of 0 means auto).
    public const double DefaultFontSize = 12.0;

    public static double Baseline(double height, double fontSize)
        => height > fontSize ? (height - fontSize) / 2.0 : 2.0;

    public static StreamObject BuildText(string value, double width, double height, Font font)
    {
        using var writer = new ContentWriter();
        writer.WriteRaw("/Tx BMC\nq\n");
        new TextContent(value, Unit.FromPoint(2.0), Unit.FromPoint(Baseline(height, font.Size)))
        {
            Font = font,
        }.Emit(writer);
        writer.WriteRaw("Q\nEMC\n");

        return Wrap(writer, width, height);
    }

    // A visible signature widget appearance: the given text lines stacked from the
    // top of the box down, each in the supplied base-14 font. Non-encodable glyphs
    // are dropped by the WinAnsi text encoder rather than failing the whole stream.
    public static StreamObject BuildSignatureAppearance(
        IReadOnlyList<string> lines, double width, double height, Font font)
    {
        using var writer = new ContentWriter();
        writer.WriteRaw("q\n");
        var lineHeight = font.Size * 1.2;
        var y = height - font.Size - 2.0;
        foreach (var line in lines)
        {
            if (line.Length > 0 && y >= 0.0)
            {
                new TextContent(line, Unit.FromPoint(2.0), Unit.FromPoint(y)) { Font = font }.Emit(writer);
            }

            y -= lineHeight;
        }

        writer.WriteRaw("Q\n");
        return Wrap(writer, width, height);
    }

    public static StreamObject BuildCheck(double width, double height)
    {
        using var writer = new ContentWriter();
        CheckMark(0.0, 0.0, width, height).Emit(writer);
        return Wrap(writer, width, height);
    }

    public static StreamObject BuildOff(double width, double height)
    {
        using var writer = new ContentWriter();
        return Wrap(writer, width, height);
    }

    public static StreamObject BuildRadio(double width, double height, bool selected)
    {
        using var writer = new ContentWriter();
        RadioBorder(0.0, 0.0, width, height).Emit(writer);
        if (selected)
        {
            RadioDot(0.0, 0.0, width, height).Emit(writer);
        }

        return Wrap(writer, width, height);
    }

    // The circular outline every radio widget state draws, positioned inside the
    // given rectangle in the target coordinate space (appearance bbox or page).
    public static PathContent RadioBorder(double x, double y, double width, double height)
    {
        var extent = Math.Min(width, height);
        var path = new PathContent
        {
            Stroke = true,
            Thickness = Math.Max(1.0, extent * 0.08),
        };
        Circle(path, x + width / 2.0, y + height / 2.0, extent * 0.42);
        return path;
    }

    // The filled dot drawn for the selected radio option; shared with form
    // flattening so a flattened selection matches the widget appearance.
    public static PathContent RadioDot(double x, double y, double width, double height)
    {
        var path = new PathContent { Fill = true };
        Circle(path, x + width / 2.0, y + height / 2.0, Math.Min(width, height) * 0.22);
        return path;
    }

    // Approximates a circle with four cubic Bezier arcs.
    private static void Circle(PathContent path, double cx, double cy, double r)
    {
        const double Kappa = 0.5522847498307936;
        var k = r * Kappa;
        path.MoveTo(Unit.FromPoint(cx + r), Unit.FromPoint(cy));
        path.CurveTo(
            Unit.FromPoint(cx + r), Unit.FromPoint(cy + k),
            Unit.FromPoint(cx + k), Unit.FromPoint(cy + r),
            Unit.FromPoint(cx), Unit.FromPoint(cy + r));
        path.CurveTo(
            Unit.FromPoint(cx - k), Unit.FromPoint(cy + r),
            Unit.FromPoint(cx - r), Unit.FromPoint(cy + k),
            Unit.FromPoint(cx - r), Unit.FromPoint(cy));
        path.CurveTo(
            Unit.FromPoint(cx - r), Unit.FromPoint(cy - k),
            Unit.FromPoint(cx - k), Unit.FromPoint(cy - r),
            Unit.FromPoint(cx), Unit.FromPoint(cy - r));
        path.CurveTo(
            Unit.FromPoint(cx + k), Unit.FromPoint(cy - r),
            Unit.FromPoint(cx + r), Unit.FromPoint(cy - k),
            Unit.FromPoint(cx + r), Unit.FromPoint(cy));
        path.Close();
    }

    // The check-mark glyph drawn for a checked box, positioned inside the given
    // rectangle in the target coordinate space (appearance bbox or page).
    public static PathContent CheckMark(double x, double y, double width, double height)
    {
        var path = new PathContent
        {
            Stroke = true,
            Thickness = Math.Max(1.0, Math.Min(width, height) * 0.12),
        };
        path.MoveTo(Unit.FromPoint(x + width * 0.22), Unit.FromPoint(y + height * 0.52));
        path.LineTo(Unit.FromPoint(x + width * 0.44), Unit.FromPoint(y + height * 0.28));
        path.LineTo(Unit.FromPoint(x + width * 0.8), Unit.FromPoint(y + height * 0.74));
        return path;
    }

    public static bool CanEncode(string value)
    {
        foreach (var c in value)
        {
            if (!WinAnsiEncoding.CanEncode(c))
            {
                return false;
            }
        }

        return true;
    }

    // Reads the font resource name and size from a "/Font size Tf" default-appearance string.
    public static (string? Font, double Size) ParseDefaultAppearance(string? da)
    {
        if (da is null)
        {
            return (null, 0.0);
        }

        var tokens = da.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 2; i < tokens.Length; i++)
        {
            if (tokens[i] == "Tf")
            {
                var name = tokens[i - 2];
                var font = name.StartsWith('/') ? name[1..] : name;
                _ = double.TryParse(tokens[i - 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var size);
                return (font, size);
            }
        }

        return (null, 0.0);
    }

    // Maps a standard AcroForm /DA font resource name to a base-14 family for the appearance.
    public static Font AppearanceFont(string? daFont, double size) => new()
    {
        Name = daFont switch
        {
            "Cour" or "Courier" => "Courier",
            "TiRo" or "Times" or "Times-Roman" => "Times-Roman",
            _ => "Helvetica",
        },
        Size = size,
    };

    private static StreamObject Wrap(ContentWriter writer, double width, double height)
    {
        ArrayObject bbox =
        [
            new NumberObject(0.0),
            new NumberObject(0.0),
            new NumberObject(width),
            new NumberObject(height),
        ];

        var appearance = new StreamObject(writer.ToArray());
        appearance.Dictionary["Type"] = new NameObject("XObject");
        appearance.Dictionary["Subtype"] = new NameObject("Form");
        appearance.Dictionary["BBox"] = bbox;

        var resources = BuildFontResources(writer);
        if (resources is not null)
        {
            appearance.Dictionary["Resources"] = resources;
        }

        return appearance;
    }

    private static DictionaryObject? BuildFontResources(ContentWriter writer)
    {
        DictionaryObject? fonts = null;
        foreach (var (baseFont, key) in writer.Fonts)
        {
            fonts ??= new DictionaryObject();
            fonts[key] = PageResourceBuilder.Base14FontDictionary(baseFont);
        }

        return fonts is null ? null : new DictionaryObject { ["Font"] = fonts };
    }
}

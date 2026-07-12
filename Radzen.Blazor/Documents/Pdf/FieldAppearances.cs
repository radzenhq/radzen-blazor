using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Objects;
using System.Globalization;

namespace Radzen.Documents.Pdf;


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

    // The check-mark glyph drawn for a checked box, positioned inside the given
    // rectangle in the target coordinate space (appearance bbox or page).
    public static PathContent CheckMark(double x, double y, double width, double height)
    {
        var path = new PathContent
        {
            Stroke = true,
            Thickness = System.Math.Max(1.0, System.Math.Min(width, height) * 0.12),
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

        var tokens = da.Split((char[]?)null, System.StringSplitOptions.RemoveEmptyEntries);
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
            fonts[key] = new DictionaryObject
            {
                ["Type"] = new NameObject("Font"),
                ["Subtype"] = new NameObject("Type1"),
                ["BaseFont"] = new NameObject(baseFont),
                ["Encoding"] = new NameObject("WinAnsiEncoding"),
            };
        }

        return fonts is null ? null : new DictionaryObject { ["Font"] = fonts };
    }
}

using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

using Radzen.Documents.Pdf.Content;
namespace Radzen.Documents.Pdf.Emit;


internal static class FieldAppearances
{
    public const double DefaultFontSize = 12.0;

    public static double Baseline(double height, double fontSize)
        => height > fontSize ? (height - fontSize) / 2.0 : 2.0;

    public static StreamObject BuildText(string value, double width, double height, Font font, FontScope scope)
    {
        using var writer = new ContentWriter(scope);
        writer.WriteRaw("/Tx BMC\nq\n");
        Text(value, 0.0, 0.0, height, font).Emit(writer);
        writer.WriteRaw("Q\nEMC\n");

        return Wrap(writer, width, height);
    }

    public static StreamObject BuildSignatureAppearance(
        IReadOnlyList<string> lines, double width, double height, Font font, FontScope scope)
    {
        using var writer = new ContentWriter(scope);
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
        foreach (var path in RadioVisual(0.0, 0.0, width, height, selected))
        {
            path.Emit(writer);
        }

        return Wrap(writer, width, height);
    }

    public static IReadOnlyList<PathContent> RadioVisual(
        double x, double y, double width, double height, bool selected)
        => selected
            ? [RadioBorder(x, y, width, height), RadioDot(x, y, width, height)]
            : [RadioBorder(x, y, width, height)];

    public static PathContent RadioBorder(double x, double y, double width, double height)
    {
        var extent = Math.Min(width, height);
        var path = new PathContent
        {
            Stroke = true,
            Thickness = Math.Max(1.0, extent * 0.08),
        };
        BezierGeometry.AppendCircle(path, x + width / 2.0, y + height / 2.0, extent * 0.42);
        return path;
    }

    public static PathContent RadioDot(double x, double y, double width, double height)
    {
        var path = new PathContent { Fill = true };
        BezierGeometry.AppendCircle(path, x + width / 2.0, y + height / 2.0, Math.Min(width, height) * 0.22);
        return path;
    }

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

    public static bool CanEncode(string value) => WinAnsiText.CanEncode(value);

    public static bool CanBakeSingleLine(FormFieldDefinition definition) => definition switch
    {
        TextFieldDefinition text => CanEncode(text.Value) && !text.Multiline && !text.Password && !text.Comb,
        ChoiceFieldDefinition choice => CanEncode(choice.Value),
        _ => false,
    };

    public static TextContent Text(string value, double x, double y, double height, Font font)
        => new(value, Unit.FromPoint(x + 2.0), Unit.FromPoint(y + Baseline(height, font.Size)))
        {
            Font = font,
        };

    public static Font AppearanceFont(string? daFont, double size) => new()
    {
        Name = daFont switch
        {
            "Cour" or "Courier" => "Courier",
            "TiRo" or "Times" or "Times-Roman" => "Times-Roman",
            _ => "Helvetica",
        },
        Size = size > 0.0 ? size : DefaultFontSize,
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
        FormXObjectShell.ApplyHeader(appearance.Dictionary, bbox, formType: false);

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

using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Fonts;
using Radzen.Documents.Core;
namespace Radzen.Documents.Pdf.Write;


internal static class FieldAppearances
{
    public const double DefaultFontSize = 12.0;

    public static double Baseline(double height, double fontSize)
        => height > fontSize ? (height - fontSize) / 2.0 : 2.0;

    public static StreamObject BuildText(string value, double width, double height, Font font, FontScope scope)
        => Appearance(scope, width, height, [Text(value, 0.0, 0.0, height, font)], "/Tx BMC\nq\n", "Q\nEMC\n");

    public static StreamObject BuildSignatureAppearance(
        IReadOnlyList<string> lines, double width, double height, Font font, FontScope scope)
        => Appearance(scope, width, height, SignatureLines(lines, height, font), "q\n", "Q\n");

    private static IEnumerable<ContentElement> SignatureLines(IReadOnlyList<string> lines, double height, Font font)
    {
        var lineHeight = font.EffectiveSize.Point * 1.2;
        var y = height - font.EffectiveSize.Point - 2.0;
        foreach (var line in lines)
        {
            if (line.Length > 0 && y >= 0.0)
            {
                yield return new TextContent(line, Unit.FromPoint(2.0), Unit.FromPoint(y)) { Font = font };
            }

            y -= lineHeight;
        }
    }

    public static StreamObject BuildCheck(double width, double height)
        => Appearance(default, width, height, [CheckMark(0.0, 0.0, width, height)]);

    public static StreamObject BuildOff(double width, double height)
        => Appearance(default, width, height, []);

    public static StreamObject BuildRadio(double width, double height, bool selected)
        => Appearance(default, width, height, RadioVisual(0.0, 0.0, width, height, selected));

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
        => new(value, Unit.FromPoint(x + 2.0), Unit.FromPoint(y + Baseline(height, font.EffectiveSize.Point)))
        {
            Font = font,
        };

    public static Font AppearanceFont(string? daFont, double size) => new()
    {
        Family = daFont switch
        {
            "Cour" or "Courier" => "Courier",
            "TiRo" or "Times" or "Times-Roman" => "Times-Roman",
            _ => "Helvetica",
        },
        Size = size > 0.0 ? size : DefaultFontSize,
    };

    private static StreamObject Appearance(
        FontScope scope,
        double width,
        double height,
        IEnumerable<ContentElement> elements,
        string? prologue = null,
        string? epilogue = null)
        => AppearanceStreamBuilder.Render(
            scope,
            ContentResourcePrefixes.Page,
            [
                new NumberObject(0.0),
                new NumberObject(0.0),
                new NumberObject(width),
                new NumberObject(height),
            ],
            formType: false,
            elements,
            prologue: prologue,
            epilogue: epilogue,
            validateResources: RejectXObjects);

    private static void RejectXObjects(ContentResourceManifest manifest)
    {
        if (manifest.ImagesForWriting.Count > 0 || manifest.Patterns.Count > 0)
        {
            throw new NotSupportedException(
                "A field appearance stream cannot reference an image or shading-pattern resource.");
        }
    }
}

using System.Globalization;
using System.Text;
using Radzen.Documents.Pdf.Content;
using static Radzen.Documents.Pdf.Content.ContentOperands;

namespace Radzen.Documents.Pdf;

internal readonly record struct DefaultAppearance(string? Font, double Size, Color? FillColor);

internal static class DefaultAppearanceGrammar
{
    internal static string Write(string fontName, double size, string colorOperator)
        => string.Create(CultureInfo.InvariantCulture, $"/{fontName} {size:0.###} Tf {colorOperator}");

    internal static DefaultAppearance Parse(string? value)
    {
        if (value is null)
        {
            return default;
        }

        string? font = null;
        var size = 0.0;
        Color? color = null;
        var tokens = ContentTokenizer.Tokenize(Encoding.Latin1.GetBytes(value));
        foreach (var frame in ContentOperandScan.Scan(tokens))
        {
            var op = frame.Operator.Text;
            if (op == "Tf")
            {
                font = LastName(frame.Operands);
                size = LastNumber(frame.Operands);
                continue;
            }

            var expected = op switch
            {
                "g" => 1,
                "rg" => 3,
                "k" => 4,
                _ => 0,
            };
            if (expected == 0)
            {
                continue;
            }

            var components = AllNumbers(frame.Operands);
            if (components.Length == expected)
            {
                color = DeviceColorConverter.FromComponents(components);
            }
        }

        return new DefaultAppearance(font, size, color);
    }

}

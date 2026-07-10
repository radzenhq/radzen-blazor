#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;

namespace Radzen.Blazor.Pdf.Tests;

// Shared plumbing for the P3 style-cascade / line-break / underline / fallback tests.
// Parses the decoded first-page content stream produced by DocumentBuilder.Build()
// into the raw text-positioning and path operators so tests can assert on the real
// emitted PDF operators (Tf sizes, Td positions, stroked/filled line geometry).
internal static class CascadeTestSupport
{
    public static string FirstPageContent(DocumentBuilder builder)
    {
        var reader = BuildTestSupport.Read(builder);
        var leaves = BuildTestSupport.PageLeaves(reader);
        return Encoding.Latin1.GetString(BuildTestSupport.Content(reader, leaves[0].Page));
    }

    public static List<double> TfSizes(string content)
    {
        var sizes = new List<double>();
        foreach (Match m in Regex.Matches(content, @"(-?[\d.]+) Tf"))
        {
            sizes.Add(double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture));
        }

        return sizes;
    }

    public static List<(double X, double Y)> TdPositions(string content)
    {
        var positions = new List<(double, double)>();
        foreach (Match m in Regex.Matches(content, @"(-?[\d.]+) (-?[\d.]+) Td"))
        {
            positions.Add((
                double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)));
        }

        return positions;
    }

    // Horizontal drawn spans: 'm/l' segments with equal y, and 're' rectangles
    // (reported at their vertical midline). Covers both ways an underline can be drawn.
    public static List<(double X, double Y, double Width)> HorizontalSpans(string content)
    {
        var spans = new List<(double, double, double)>();
        foreach (Match m in Regex.Matches(content, @"(-?[\d.]+) (-?[\d.]+) m\s+(-?[\d.]+) (-?[\d.]+) l"))
        {
            var y1 = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            var y2 = double.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture);
            if (Math.Abs(y1 - y2) < 0.01)
            {
                var x1 = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                var x2 = double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
                spans.Add((Math.Min(x1, x2), y1, Math.Abs(x2 - x1)));
            }
        }

        foreach (Match m in Regex.Matches(content, @"(-?[\d.]+) (-?[\d.]+) (-?[\d.]+) (-?[\d.]+) re"))
        {
            var x = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            var y = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            var w = double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
            var h = double.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture);
            if (w > h)
            {
                spans.Add((x, y + h / 2.0, w));
            }
        }

        return spans;
    }
}

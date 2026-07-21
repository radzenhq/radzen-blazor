using System;

namespace Radzen.Documents.Pdf.Emit;

internal static class HorizontalAlignmentOffset
{
    public static double Of(HorizontalAlignment alignment, double available, double contentWidth)
        => Math.Max(0, alignment switch
        {
            HorizontalAlignment.Center => (available - contentWidth) / 2.0,
            HorizontalAlignment.Right or HorizontalAlignment.End => available - contentWidth,
            _ => 0,
        });
}

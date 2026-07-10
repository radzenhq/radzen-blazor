#nullable enable

namespace Radzen.Documents.Pdf.Fonts.Sfnt;

// hmtx: advance widths keyed by glyph id, with last-advance reuse for
// glyphs beyond numberOfHMetrics (monospaced tail of the table).
internal sealed class HorizontalMetrics
{
    private readonly ushort[] advanceWidths;

    private HorizontalMetrics(ushort[] advanceWidths) => this.advanceWidths = advanceWidths;

    public static HorizontalMetrics Parse(byte[] data, int offset, int numberOfHMetrics)
    {
        var reader = new SfntReader(data, offset);
        var widths = new ushort[numberOfHMetrics == 0 ? 1 : numberOfHMetrics];
        for (var i = 0; i < numberOfHMetrics; i++)
        {
            widths[i] = reader.ReadUInt16();
            reader.ReadInt16(); // leftSideBearing
        }

        return new HorizontalMetrics(widths);
    }

    public ushort GetAdvanceWidth(ushort glyphId)
    {
        if (glyphId < advanceWidths.Length)
        {
            return advanceWidths[glyphId];
        }

        return advanceWidths[^1];
    }
}

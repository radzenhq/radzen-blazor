using System.IO;

namespace Radzen.Documents.Fonts.Sfnt;

internal sealed class HorizontalMetrics
{
    private readonly ushort[] advanceWidths;

    private HorizontalMetrics(ushort[] advanceWidths) => this.advanceWidths = advanceWidths;

    public static HorizontalMetrics Parse(byte[] data, int offset, int numberOfHMetrics)
    {
        if (numberOfHMetrics <= 0)
        {
            throw new InvalidDataException(
                "The 'hhea' table declares no horizontal metrics, so no glyph has an advance width.");
        }

        var reader = new SfntReader(data, offset);
        var widths = new ushort[numberOfHMetrics];
        for (var i = 0; i < numberOfHMetrics; i++)
        {
            widths[i] = reader.ReadUInt16();
            reader.ReadInt16();
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

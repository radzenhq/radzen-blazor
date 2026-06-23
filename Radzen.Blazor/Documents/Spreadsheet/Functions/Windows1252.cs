#nullable enable

namespace Radzen.Documents.Spreadsheet;

// Windows-1252 (ANSI) mapping used by Excel CHAR and CODE. Identical to Latin-1 except for the
// 128-159 range, which holds typographic characters rather than C1 control codes.
static class Windows1252
{
    // Unicode code points for Windows-1252 bytes 128-159 (undefined slots map to themselves).
    private static readonly int[] ExtraCodePoints =
    {
        0x20AC, 0x0081, 0x201A, 0x0192, 0x201E, 0x2026, 0x2020, 0x2021,
        0x02C6, 0x2030, 0x0160, 0x2039, 0x0152, 0x008D, 0x017D, 0x008F,
        0x0090, 0x2018, 0x2019, 0x201C, 0x201D, 0x2022, 0x2013, 0x2014,
        0x02DC, 0x2122, 0x0161, 0x203A, 0x0153, 0x009D, 0x017E, 0x0178
    };

    public static char ToChar(int code) =>
        code is >= 128 and <= 159 ? (char)ExtraCodePoints[code - 128] : (char)code;

    public static int ToCode(char ch)
    {
        if (ch < 128 || (ch >= 160 && ch <= 255))
        {
            return ch;
        }

        for (var i = 0; i < ExtraCodePoints.Length; i++)
        {
            if (ExtraCodePoints[i] == ch)
            {
                return 128 + i;
            }
        }

        return ch;
    }
}

namespace Radzen.Documents.Fonts;

internal static class IgnorableCharacters
{
    // Unicode TR44 Default_Ignorable_Code_Point (the classes that reach text runs in practice),
    // plus the Cc controls: characters that legitimately have no glyph, so a font not mapping
    // them is not a coverage failure.
    public static bool IsIgnorableOnMiss(int codepoint) => codepoint switch
    {
        0x00AD => true,
        0x200B or 0x200C or 0x200D or 0x200E or 0x200F => true,
        >= 0x202A and <= 0x202E => true,
        >= 0x2060 and <= 0x2064 => true,
        >= 0x2066 and <= 0x2069 => true,
        0xFEFF => true,
        >= 0xD800 and <= 0xDFFF => true,
        >= 0xFE00 and <= 0xFE0F => true,
        >= 0xE0100 and <= 0xE01EF => true,
        <= 0x001F => true,
        >= 0x007F and <= 0x009F => true,
        _ => false,
    };

    // ISO/IEC 14496-22 leaves space variants optional in cmap; a font that draws U+0020 can
    // stand in for them at the space advance.
    public static bool IsSpaceOnMiss(int codepoint) => codepoint switch
    {
        0x00A0 or 0x2007 or 0x202F => true,
        _ => false,
    };
}

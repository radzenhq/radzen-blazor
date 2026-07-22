namespace Radzen.Documents.Pdf.Objects;

internal static class PdfDocEncoding
{
    internal static readonly char[] ToUnicode = BuildTable();

    internal static bool IsRemapped(char ch) => ch <= 0xFF && ToUnicode[ch] != ch;

    // ISO 32000-1 Annex D.2: PDFDocEncoding maps these byte values to characters
    // that differ from ISO/IEC 8859-1 (Latin-1); every other byte is its Latin-1 value.
    private static char[] BuildTable()
    {
        var table = new char[256];
        for (var i = 0; i < table.Length; i++)
        {
            table[i] = (char)i;
        }

        table[0x18] = '\u02d8';
        table[0x19] = '\u02c7';
        table[0x1a] = '\u02c6';
        table[0x1b] = '\u02d9';
        table[0x1c] = '\u02dd';
        table[0x1d] = '\u02db';
        table[0x1e] = '\u02da';
        table[0x1f] = '\u02dc';
        table[0x80] = '\u2022';
        table[0x81] = '\u2020';
        table[0x82] = '\u2021';
        table[0x83] = '\u2026';
        table[0x84] = '\u2014';
        table[0x85] = '\u2013';
        table[0x86] = '\u0192';
        table[0x87] = '\u2044';
        table[0x88] = '\u2039';
        table[0x89] = '\u203a';
        table[0x8a] = '\u2212';
        table[0x8b] = '\u2030';
        table[0x8c] = '\u201e';
        table[0x8d] = '\u201c';
        table[0x8e] = '\u201d';
        table[0x8f] = '\u2018';
        table[0x90] = '\u2019';
        table[0x91] = '\u201a';
        table[0x92] = '\u2122';
        table[0x93] = '\ufb01';
        table[0x94] = '\ufb02';
        table[0x95] = '\u0141';
        table[0x96] = '\u0152';
        table[0x97] = '\u0160';
        table[0x98] = '\u0178';
        table[0x99] = '\u017d';
        table[0x9a] = '\u0131';
        table[0x9b] = '\u0142';
        table[0x9c] = '\u0153';
        table[0x9d] = '\u0161';
        table[0x9e] = '\u017e';
        table[0xa0] = '\u20ac';

        return table;
    }
}

namespace Radzen.Documents.Pdf.Objects;

internal enum TokenKind
{
    Integer,
    Real,
    Name,
    StringLiteral,
    HexString,
    ArrayOpen,
    ArrayClose,
    DictOpen,
    DictClose,
    Keyword,
    EndOfData,
}

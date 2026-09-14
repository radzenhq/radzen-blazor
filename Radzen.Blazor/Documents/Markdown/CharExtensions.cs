namespace Radzen.Documents.Markdown;

static class CharExtensions
{
    public static bool IsNullOrWhiteSpace(this char ch) => ch == '\0' || char.IsWhiteSpace(ch);

    // https://spec.commonmark.org/0.31.2/#unicode-punctuation-character
    public static bool IsPunctuation(this char ch) => ch.IsEscapable() || char.IsPunctuation(ch);

    // https://spec.commonmark.org/0.31.2/#backslash-escapes
    public static bool IsEscapable(this char ch) => ch is (>= '!' and <= '/') or (>= ':' and <= '@') or (>= '[' and <= '`') or (>= '{' and <= '~');

    public static bool IsSpaceOrTab(this char ch) => ch == ' ' || ch == '\t';
}

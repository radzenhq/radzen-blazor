namespace Radzen.Documents.Pdf.Objects;

// A value type: a large PDF lexes into tens of millions of tokens, so one heap
// object per token would dominate the cost of opening a document.
internal readonly struct Token
{
    private Token(TokenKind kind, long intValue, double realValue, string text, byte[] bytes)
    {
        Kind = kind;
        IntValue = intValue;
        RealValue = realValue;
        Text = text;
        Bytes = bytes;
    }

    public TokenKind Kind { get; }

    public long IntValue { get; }

    public double RealValue { get; }

    public string Text { get; }

    public byte[] Bytes { get; }

    public static Token Integer(long value)
        => new(TokenKind.Integer, value, value, string.Empty, []);

    public static Token Real(double value)
        => new(TokenKind.Real, (long)value, value, string.Empty, []);

    public static Token Name(string text)
        => new(TokenKind.Name, 0, 0, text, []);

    public static Token Keyword(string text)
        => new(TokenKind.Keyword, 0, 0, text, []);

    public static Token String(TokenKind kind, byte[] bytes)
        => new(kind, 0, 0, string.Empty, bytes);

    public static Token Delimiter(TokenKind kind)
        => new(kind, 0, 0, string.Empty, []);
}

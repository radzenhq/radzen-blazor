namespace Radzen.Blazor.Markdown;

static class CharExtensions
{
    public static bool IsNullOrWhiteSpace(this char ch) => ch == '\0' || char.IsWhiteSpace(ch);

    public static bool IsPunctuation(this char ch) => char.IsPunctuation(ch);

    public static bool IsSpaceOrTab(this char ch) => ch == ' ' || ch == '\t';
}

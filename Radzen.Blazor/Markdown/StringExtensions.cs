namespace Radzen.Blazor.Markdown;

static class StringExtensions
{
    public static char Peek(this string line, int offset = 0)
    {
        return offset < line.Length ? line[offset] : default;
    }
}

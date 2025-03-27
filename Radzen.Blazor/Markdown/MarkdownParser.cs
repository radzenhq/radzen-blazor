namespace Radzen.Blazor.Markdown;

/// <summary>
/// Parses a Markdown document.
/// </summary>
public static class MarkdownParser
{
    /// <summary>
    /// Parses a string containing Markdown into a document.
    /// </summary>
    /// <param name="markdown">The Markdown content to parse.</param>
    /// <returns>The parsed document.</returns>
    public static Document Parse(string markdown)
    {
        return BlockParser.Parse(markdown);
    }
}
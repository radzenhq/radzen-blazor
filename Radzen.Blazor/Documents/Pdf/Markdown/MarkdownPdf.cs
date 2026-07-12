using System;

namespace Radzen.Documents.Pdf.Markdown;

/// <summary>
/// Renders Markdown content into PDF document-builder blocks, reusing the CommonMark parser
/// that backs <c>RadzenMarkdown</c>.
/// </summary>
public static class MarkdownPdf
{
    /// <summary>
    /// Parses <paramref name="markdown"/> and appends the resulting content to <paramref name="blocks"/>.
    /// </summary>
    /// <param name="blocks">The block collection to append the rendered content to.</param>
    /// <param name="markdown">The Markdown source text.</param>
    /// <param name="options">Rendering options, or <see langword="null"/> to use the defaults.</param>
    public static void Render(BlockCollection blocks, string markdown, MarkdownPdfOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        ArgumentNullException.ThrowIfNull(markdown);

        var document = Radzen.Documents.Markdown.MarkdownParser.Parse(markdown);
        var renderer = new MarkdownPdfRenderer(blocks, options ?? new MarkdownPdfOptions());
        document.Accept(renderer);
    }

    /// <summary>
    /// Parses <paramref name="markdown"/> and appends the resulting content to <paramref name="section"/>'s body.
    /// </summary>
    /// <param name="section">The section whose body blocks the rendered content is appended to.</param>
    /// <param name="markdown">The Markdown source text.</param>
    /// <param name="options">Rendering options, or <see langword="null"/> to use the defaults.</param>
    public static void Render(Section section, string markdown, MarkdownPdfOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(section);

        Render(section.Blocks, markdown, options);
    }
}

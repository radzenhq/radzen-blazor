using System.Collections.Generic;

namespace Radzen.Documents.Markdown;

internal readonly record struct TextSegment(int Start, int End, int Length);

internal sealed record MarkdownHtml(string Html, IReadOnlyList<TextSegment> Segments);

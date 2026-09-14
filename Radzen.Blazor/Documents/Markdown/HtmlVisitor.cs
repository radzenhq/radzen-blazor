using System;
using System.Collections.Generic;
using System.Text;

namespace Radzen.Documents.Markdown;

/// <summary>
/// Renders a Markdown document as an HTML string. Used by RadzenMarkdownEditor's design mode.
/// </summary>
public class HtmlVisitor : NodeVisitorBase
{
    private readonly StringBuilder html = new();
    private readonly List<TextSegment> segments = [];
    private readonly string? source;
    private int? gap;
    private const string Placeholder = "\u200B";

    private string Gap => gap is { } offset ? " data-gap=\"" + offset.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\"" : string.Empty;
    private bool suppressParagraph;
    private (string Html, int Offset)? pendingCheckbox;
    private bool inHeaderRow;

    /// <summary>Renders <paramref name="document" /> as HTML.</summary>
    public static string ToHtml(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var visitor = new HtmlVisitor();
        document.Accept(visitor);
        return visitor.html.ToString();
    }

    /// <summary>Parses <paramref name="markdown" /> and renders it as HTML.</summary>
    public static string ToHtml(string markdown) => ToHtml(MarkdownParser.Parse(markdown));

    private HtmlVisitor(string? source = null)
    {
        this.source = source;
    }

    internal static MarkdownHtml Render(string markdown) => Render(MarkdownParser.Parse(markdown), markdown);

    internal static MarkdownHtml Render(Document document, string markdown)
    {
        var visitor = new HtmlVisitor(markdown);
        document.Accept(visitor);
        return new MarkdownHtml(visitor.html.ToString(), visitor.segments);
    }

    private void AppendPlaceholder(int offset)
    {
        html.Append(Placeholder);
        segments.Add(new TextSegment(offset, offset, Placeholder.Length));
    }

    private void VisitBlocks(BlockContainer container, string placeholderTag)
    {
        var start = container is Document ? 0 : container.SourceStart;
        var end = container is Document ? source?.Length ?? container.SourceEnd : container.SourceEnd;

        if (container.Children.Count == 0 && container is ListItem)
        {
            AppendPlaceholder(end);
            return;
        }

        var index = 0;

        foreach (var child in container.Children)
        {
            var emitted = AppendBlankLines(start, child.SourceStart, index > 0, true, placeholderTag);

            if (index == 0 && !emitted && source != null && container is Document && BlankLines.IsSealed(child) && child.SourceStart == 0)
            {
                Wrap(placeholderTag, () => AppendPlaceholder(0));
            }

            if (source != null && container is Document && BlankLines.IsSealed(child))
            {
                gap = child.SourceStart >= 2 && source[child.SourceStart - 1] == '\n' && source[child.SourceStart - 2] == '\n' ? child.SourceStart - 1 : child.SourceStart;
            }

            child.Accept(this);
            gap = null;
            start = child.SourceEnd;
            index++;
        }

        var trailing = AppendBlankLines(start, end, index > 0, false, placeholderTag);

        if (!trailing && container is Document && container.LastChild is { } last && BlankLines.IsSealed(last) && source != null)
        {
            Wrap(placeholderTag, () => AppendPlaceholder(source.Length));
        }
    }

    private bool AppendBlankLines(int from, int to, bool hasPrevious, bool hasNext, string tag)
    {
        if (source == null)
        {
            return false;
        }

        var emitted = false;

        foreach (var offset in BlankLines.Placeholders(source, from, to, hasPrevious, hasNext))
        {
            Wrap(tag, () => AppendPlaceholder(offset));
            emitted = true;
        }

        return emitted;
    }

    private void WrapLeaf(string tag, Leaf leaf, Action visitChildren)
    {
        html.Append('<').Append(tag).Append('>');

        if (pendingCheckbox is { } checkbox)
        {
            pendingCheckbox = null;
            html.Append(checkbox.Html);
            segments.Add(new TextSegment(checkbox.Offset, checkbox.Offset, 1));
        }

        var length = html.Length;
        visitChildren();

        if (html.Length == length)
        {
            AppendPlaceholder(leaf.Content.ToSource(0));
        }

        html.Append("</").Append(tag).Append('>');
    }

    private void AppendText(string? value, int sourceStart, int sourceEnd)
    {
        value ??= string.Empty;
        html.Append(Escape(value));

        if (value.Length > 0)
        {
            segments.Add(new TextSegment(sourceStart, sourceEnd, value.Length));
        }
    }

    private void AppendLines(Leaf leaf)
    {
        var lines = new List<string>(leaf.Value.Split('\n'));

        // Chrome skips a <pre> whose text ends with a newline when moving the caret vertically
        if (source != null && lines.Count > 1 && lines[^1].Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        var offset = 0;

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];

            if (line.Length == 0 && index == lines.Count - 1 && source != null && index > 0)
            {
                AppendPlaceholder(leaf.Content.ToSource(offset));
                break;
            }

            AppendText(line, leaf.Content.ToSource(offset), leaf.Content.ToSourceEnd(offset + line.Length));
            offset += line.Length;

            if (index < lines.Count - 1)
            {
                html.Append('\n');
                segments.Add(new TextSegment(leaf.Content.ToSource(offset), leaf.Content.ToSourceEnd(offset + 1), 1));
                offset++;
            }
        }
    }

    private static string Escape(string? text) => (text ?? string.Empty)
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal);

    private void Wrap(string tag, Action visitChildren)
    {
        html.Append('<').Append(tag).Append('>');
        visitChildren();
        html.Append("</").Append(tag).Append('>');
    }

    /// <inheritdoc />
    public override void VisitHeading(Heading heading)
    {
        ArgumentNullException.ThrowIfNull(heading);
        WrapLeaf($"h{heading.Level}", heading, () => base.VisitHeading(heading));
    }

    /// <inheritdoc />
    public override void VisitParagraph(Paragraph paragraph)
    {
        ArgumentNullException.ThrowIfNull(paragraph);

        if (suppressParagraph || paragraph.Parent is ListItem { Parent: List { Tight: true } })
        {
            suppressParagraph = false;
            var length = html.Length;
            base.VisitParagraph(paragraph);

            if (html.Length == length)
            {
                AppendPlaceholder(paragraph.Content.ToSource(0));
            }
        }
        else
        {
            WrapLeaf("p", paragraph, () => base.VisitParagraph(paragraph));
        }
    }

    /// <inheritdoc />
    public override void VisitStrong(Strong strong) => Wrap("strong", () => base.VisitStrong(strong));

    /// <inheritdoc />
    public override void VisitEmphasis(Emphasis emphasis) => Wrap("em", () => base.VisitEmphasis(emphasis));

    /// <inheritdoc />
    public override void VisitStrikethrough(Strikethrough strikethrough) => Wrap("del", () => base.VisitStrikethrough(strikethrough));

    /// <inheritdoc />
    public override void VisitBlockQuote(BlockQuote blockQuote)
    {
        ArgumentNullException.ThrowIfNull(blockQuote);
        Wrap("blockquote", () => VisitBlocks(blockQuote, "p"));
    }

    /// <inheritdoc />
    public override void VisitDocument(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);
        VisitBlocks(document, "p");
    }

    /// <inheritdoc />
    public override void VisitText(Text text)
    {
        ArgumentNullException.ThrowIfNull(text);
        AppendText(text.Value, text.SourceStart, text.SourceEnd);
    }

    /// <inheritdoc />
    public override void VisitCode(Code code)
    {
        ArgumentNullException.ThrowIfNull(code);
        var (start, end) = CodeContent(code);
        html.Append("<code>");
        AppendText(code.Value, start, end);
        html.Append("</code>");
    }

    private (int Start, int End) CodeContent(Code code)
    {
        var start = code.SourceStart;
        var end = code.SourceEnd;

        if (source == null || end > source.Length)
        {
            return (start, end);
        }

        var ticks = 0;

        while (start + ticks < end && source[start + ticks] == '`')
        {
            ticks++;
        }

        if (end - start < 2 * ticks)
        {
            return (start, end);
        }

        start += ticks;
        end -= ticks;

        if (end - start == code.Value.Length + 2 && source[start] == ' ' && source[end - 1] == ' ')
        {
            start++;
            end--;
        }

        return string.CompareOrdinal(source, start, code.Value, 0, end - start) == 0 && end - start == code.Value.Length ? (start, end) : (code.SourceStart, code.SourceEnd);
    }

    /// <inheritdoc />
    public override void VisitLineBreak(LineBreak lineBreak) => html.Append("<br>");

    /// <inheritdoc />
    public override void VisitSoftLineBreak(SoftLineBreak softLineBreak)
    {
        ArgumentNullException.ThrowIfNull(softLineBreak);
        html.Append(' ');
        segments.Add(new TextSegment(softLineBreak.SourceStart, softLineBreak.SourceEnd, 1));
    }

    /// <inheritdoc />
    public override void VisitThematicBreak(ThematicBreak thematicBreak) => html.Append("<hr").Append(Gap).Append('>');

    /// <inheritdoc />
    public override void VisitUnorderedList(UnorderedList unorderedList)
    {
        ArgumentNullException.ThrowIfNull(unorderedList);
        Wrap("ul", () => VisitBlocks(unorderedList, "li"));
    }

    /// <inheritdoc />
    public override void VisitOrderedList(OrderedList orderedList)
    {
        ArgumentNullException.ThrowIfNull(orderedList);

        html.Append("<ol");
        if (orderedList.Start != 1)
        {
            html.Append(" start=\"").Append(orderedList.Start).Append('"');
        }
        html.Append('>');
        VisitBlocks(orderedList, "li");
        html.Append("</ol>");
    }

    /// <inheritdoc />
    public override void VisitListItem(ListItem listItem)
    {
        ArgumentNullException.ThrowIfNull(listItem);

        html.Append("<li>");

        bool tight = listItem is { Parent: List { Tight: true }, Children: [Paragraph] };

        if (listItem.Checked is { } isChecked)
        {
            var checkbox = "<input type=\"checkbox\"" + (isChecked ? " checked" : string.Empty) + "> ";
            var content = listItem.FirstChild is Leaf leaf ? leaf.Content.ToSource(0) : listItem.SourceStart;

            if (!tight && listItem.FirstChild is Paragraph)
            {
                pendingCheckbox = (checkbox, content);
            }
            else
            {
                html.Append(checkbox);
                segments.Add(new TextSegment(content, content, 1));
            }
        }

        if (tight)
        {
            suppressParagraph = true;
        }

        VisitBlocks(listItem, "p");

        html.Append("</li>");
    }

    /// <inheritdoc />
    public override void VisitLink(Link link)
    {
        ArgumentNullException.ThrowIfNull(link);

        var destination = HtmlSanitizer.IsDangerousUrl(link.Destination ?? string.Empty) ? string.Empty : link.Destination;

        html.Append("<a href=\"").Append(Escape(destination)).Append("\">");
        base.VisitLink(link);
        html.Append("</a>");
    }

    /// <inheritdoc />
    public override void VisitImage(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);

        var alt = new StringBuilder();
        AppendPlainText(alt, image.Children);

        var destination = HtmlSanitizer.IsDangerousUrl(image.Destination ?? string.Empty) ? string.Empty : image.Destination;

        html.Append("<img src=\"").Append(Escape(destination)).Append("\" alt=\"").Append(Escape(alt.ToString())).Append("\">");
    }

    private static void AppendPlainText(StringBuilder text, IReadOnlyList<Inline> nodes)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case Text plain:
                    text.Append(plain.Value);
                    break;
                case Code code:
                    text.Append(code.Value);
                    break;
                case InlineContainer container:
                    AppendPlainText(text, container.Children);
                    break;
                case SoftLineBreak or LineBreak:
                    text.Append(' ');
                    break;
            }
        }
    }

    /// <inheritdoc />
    public override void VisitFencedCodeBlock(FencedCodeBlock fencedCodeBlock)
    {
        ArgumentNullException.ThrowIfNull(fencedCodeBlock);

        html.Append("<pre").Append(Gap).Append("><code");
        if (!string.IsNullOrEmpty(fencedCodeBlock.Info))
        {
            html.Append(" class=\"language-").Append(Escape(fencedCodeBlock.Info)).Append('"');
        }
        html.Append('>');
        AppendLines(fencedCodeBlock);
        html.Append("</code></pre>");
    }

    /// <inheritdoc />
    public override void VisitIndentedCodeBlock(IndentedCodeBlock codeBlock)
    {
        ArgumentNullException.ThrowIfNull(codeBlock);
        html.Append("<pre><code>");
        AppendLines(codeBlock);
        html.Append("</code></pre>");
    }

    /// <inheritdoc />
    public override void VisitHtmlBlock(HtmlBlock htmlBlock)
    {
        ArgumentNullException.ThrowIfNull(htmlBlock);
        html.Append("<p").Append(Gap).Append('>');
        AppendLines(htmlBlock);
        html.Append("</p>");
    }

    /// <inheritdoc />
    public override void VisitHtmlInline(HtmlInline htmlInline)
    {
        ArgumentNullException.ThrowIfNull(htmlInline);
        AppendText(htmlInline.Value, htmlInline.SourceStart, htmlInline.SourceEnd);
    }

    /// <inheritdoc />
    public override void VisitTable(Table table)
    {
        html.Append("<table").Append(Gap).Append('>');
        base.VisitTable(table);
        html.Append("</tbody></table>");
    }

    /// <inheritdoc />
    public override void VisitTableHeaderRow(TableHeaderRow header)
    {
        html.Append("<thead>");
        inHeaderRow = true;
        Wrap("tr", () => base.VisitTableHeaderRow(header));
        inHeaderRow = false;
        html.Append("</thead><tbody>");
    }

    /// <inheritdoc />
    public override void VisitTableRow(TableRow row) => Wrap("tr", () => base.VisitTableRow(row));

    /// <inheritdoc />
    public override void VisitTableCell(TableCell cell)
    {
        ArgumentNullException.ThrowIfNull(cell);

        var tag = inHeaderRow ? "th" : "td";
        html.Append('<').Append(tag);
        AppendAlignment(cell.Alignment);
        html.Append('>');

        if (source != null && cell.Children.Count == 0 && cell.Content.Segments.Count > 0)
        {
            AppendPlaceholder(cell.Content.Segments[0].SourceStart);
        }
        else
        {
            base.VisitTableCell(cell);
        }

        html.Append("</").Append(tag).Append('>');
    }

    private void AppendAlignment(TableCellAlignment alignment)
    {
        var value = alignment switch
        {
            TableCellAlignment.Left => "left",
            TableCellAlignment.Center => "center",
            TableCellAlignment.Right => "right",
            _ => null
        };

        if (value != null)
        {
            html.Append(" style=\"text-align:").Append(value).Append('"');
        }
    }
}

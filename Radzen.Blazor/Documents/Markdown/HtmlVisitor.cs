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
    private readonly bool editing;
    private int position;
    private const string Placeholder = "​";

    private string? pendingCheckbox;
    private bool inHeaderRow;

    /// <summary>Renders <paramref name="document" /> as HTML.</summary>
    public static string ToHtml(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var visitor = new HtmlVisitor(editing: false);
        document.Accept(visitor);
        return visitor.html.ToString();
    }

    /// <summary>Parses <paramref name="markdown" /> and renders it as HTML.</summary>
    public static string ToHtml(string markdown) => ToHtml(MarkdownParser.Parse(markdown));

    private HtmlVisitor(bool editing)
    {
        this.editing = editing;
    }

    internal static MarkdownHtml Render(string markdown) => Render(BlankLines.Inflate(MarkdownParser.Parse(markdown), markdown));

    internal static MarkdownHtml Render(Document document)
    {
        var visitor = new HtmlVisitor(editing: true);
        document.Accept(visitor);
        return new MarkdownHtml(visitor.html.ToString(), visitor.segments);
    }

    internal static int CodeLength(Leaf code) => code.Value.EndsWith('\n') ? code.Value.Length - 1 : code.Value.Length;

    private void AppendPlaceholder()
    {
        html.Append(Placeholder);
        segments.Add(new TextSegment(position, position, Placeholder.Length));
    }

    private void EndHost() => position++;

    private void VisitBlocks(BlockContainer container)
    {
        if (container.Children.Count == 0 && container is not Document)
        {
            if (editing)
            {
                AppendPlaceholder();
                EndHost();
            }

            return;
        }

        foreach (var child in container.Children)
        {
            child.Accept(this);
        }
    }

    private void WrapLeaf(string tag, Leaf leaf, Action visitChildren)
    {
        html.Append('<').Append(tag).Append('>');
        tag = tag.Split(' ')[0];

        if (pendingCheckbox is { } checkbox)
        {
            pendingCheckbox = null;
            html.Append(checkbox);
            segments.Add(new TextSegment(position, position, 1));
        }

        AppendContent(leaf, visitChildren);
        html.Append("</").Append(tag).Append('>');
    }

    private bool afterText;

    private bool pendingAtom;

    private void AppendContent(IBlockInlineContainer content, Action visitChildren)
    {
        var length = html.Length;
        afterText = false;
        pendingAtom = false;
        visitChildren();
        FlushAtom();

        if (editing)
        {
            if (html.Length == length)
            {
                AppendPlaceholder();
            }

            EndHost();
        }
    }

    private void Note(int length)
    {
        pendingAtom = length > 0 ? false : pendingAtom;
        afterText = afterText || length > 0;
    }

    private void FlushAtom()
    {
        if (editing && pendingAtom)
        {
            AppendPlaceholder();
        }

        pendingAtom = false;
    }

    private void BeforeAtom()
    {
        FlushAtom();

        if (editing && !afterText)
        {
            AppendPlaceholder();
        }
    }

    private void AfterAtom()
    {
        afterText = false;
        position++;
        pendingAtom = true;
    }

    private void AppendText(string? value)
    {
        value ??= string.Empty;
        html.Append(Escape(value));

        if (value.Length > 0)
        {
            segments.Add(new TextSegment(position, position + value.Length, value.Length));
            position += value.Length;
        }
    }

    private void AppendLines(Leaf leaf)
    {
        var lines = new List<string>(leaf.Value.Split('\n'));

        // Chrome skips a <pre> whose text ends with a newline when moving the caret vertically
        if (editing && lines.Count > 1 && lines[^1].Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];

            if (line.Length == 0 && index == lines.Count - 1 && editing)
            {
                AppendPlaceholder();
                break;
            }

            AppendText(line);

            if (index < lines.Count - 1)
            {
                html.Append('\n');
                segments.Add(new TextSegment(position, position + 1, 1));
                position++;
            }
        }

        if (editing)
        {
            EndHost();
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

        if (paragraph.Parent is ListItem { Parent: List { Tight: true } })
        {
            AppendContent(paragraph, () => base.VisitParagraph(paragraph));
        }
        else if (paragraph is { Virtual: true, Parent: Document parent } && parent.Children[0] != paragraph && parent.LastChild != paragraph)
        {
            WrapLeaf("p class=\"rz-markdown-editor-gap\"", paragraph, () => base.VisitParagraph(paragraph));
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
        Wrap("blockquote", () => VisitBlocks(blockQuote));
    }

    /// <inheritdoc />
    public override void VisitDocument(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);
        VisitBlocks(document);
    }

    /// <inheritdoc />
    public override void VisitText(Text text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Note(text.Value.Length);
        AppendText(text.Value);
    }

    /// <inheritdoc />
    public override void VisitCode(Code code)
    {
        ArgumentNullException.ThrowIfNull(code);
        Note(code.Value.Length);
        html.Append("<code>");
        AppendText(code.Value);
        html.Append("</code>");
    }

    /// <inheritdoc />
    public override void VisitLineBreak(LineBreak lineBreak)
    {
        ArgumentNullException.ThrowIfNull(lineBreak);
        BeforeAtom();
        html.Append("<br>");
        AfterAtom();
    }

    /// <inheritdoc />
    public override void VisitSoftLineBreak(SoftLineBreak softLineBreak)
    {
        ArgumentNullException.ThrowIfNull(softLineBreak);
        pendingAtom = false;
        afterText = true;
        html.Append(' ');
        segments.Add(new TextSegment(position, position + 1, 1));
        position++;
    }

    /// <inheritdoc />
    public override void VisitThematicBreak(ThematicBreak thematicBreak)
    {
        html.Append("<hr");

        if (editing)
        {
            html.Append(" data-gap=\"").Append(position.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('"');
            EndHost();
        }

        html.Append('>');
    }

    /// <inheritdoc />
    public override void VisitUnorderedList(UnorderedList unorderedList)
    {
        ArgumentNullException.ThrowIfNull(unorderedList);
        Wrap("ul", () => VisitBlocks(unorderedList));
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
        VisitBlocks(orderedList);
        html.Append("</ol>");
    }

    /// <inheritdoc />
    public override void VisitListItem(ListItem listItem)
    {
        ArgumentNullException.ThrowIfNull(listItem);

        html.Append("<li>");

        bool tight = listItem is { Parent: List { Tight: true }, Children: [Paragraph, ..] };

        if (listItem.Checked is { } isChecked)
        {
            var checkbox = "<input type=\"checkbox\"" + (isChecked ? " checked" : string.Empty) + "> ";

            if (!tight && listItem.FirstChild is Paragraph)
            {
                pendingCheckbox = checkbox;
            }
            else
            {
                html.Append(checkbox);
                segments.Add(new TextSegment(position, position, 1));
            }
        }

        VisitBlocks(listItem);

        html.Append("</li>");
    }

    /// <inheritdoc />
    public override void VisitLink(Link link)
    {
        ArgumentNullException.ThrowIfNull(link);

        var destination = HtmlSanitizer.IsDangerousUrl(link.Destination ?? string.Empty) ? string.Empty : link.Destination;

        html.Append("<a href=\"").Append(Escape(destination)).Append("\">");
        base.VisitLink(link);
        FlushAtom();
        html.Append("</a>");
    }

    /// <inheritdoc />
    public override void VisitImage(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);

        var alt = new StringBuilder();
        AppendPlainText(alt, image.Children);

        var destination = HtmlSanitizer.IsDangerousUrl(image.Destination ?? string.Empty) ? string.Empty : image.Destination;

        BeforeAtom();
        html.Append("<img src=\"").Append(Escape(destination)).Append("\" alt=\"").Append(Escape(alt.ToString())).Append("\">");
        AfterAtom();
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

        html.Append("<pre><code");
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
        html.Append("<p>");
        AppendLines(htmlBlock);
        html.Append("</p>");
    }

    /// <inheritdoc />
    public override void VisitHtmlInline(HtmlInline htmlInline)
    {
        ArgumentNullException.ThrowIfNull(htmlInline);
        var value = htmlInline.Value ?? string.Empty;
        Note(value.Length);
        html.Append(Escape(value));

        if (value.Length > 0)
        {
            segments.Add(new TextSegment(position, position + 1, value.Length));
            position++;
        }
    }

    /// <inheritdoc />
    public override void VisitTable(Table table)
    {
        html.Append("<table>");
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
        AppendContent(cell, () => base.VisitTableCell(cell));
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

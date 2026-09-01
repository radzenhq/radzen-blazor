using System;
using System.Collections.Generic;
using System.Text;

namespace Radzen.Documents.Markdown;

/// <summary>
/// Renders a markdown document as an HTML string. Used by RadzenMarkdownEditor's design mode.
/// </summary>
public class HtmlVisitor : NodeVisitorBase
{
    private readonly StringBuilder html = new();
    private bool suppressParagraph;
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
    public static string ToHtml(string markdown) => ToHtml(MarkdownParser.Parse(markdown ?? string.Empty));

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
        Wrap($"h{heading.Level}", () => base.VisitHeading(heading));
    }

    /// <inheritdoc />
    public override void VisitParagraph(Paragraph paragraph)
    {
        if (suppressParagraph)
        {
            suppressParagraph = false;
            base.VisitParagraph(paragraph);
        }
        else
        {
            Wrap("p", () => base.VisitParagraph(paragraph));
        }
    }

    /// <inheritdoc />
    public override void VisitStrong(Strong strong) => Wrap("strong", () => base.VisitStrong(strong));

    /// <inheritdoc />
    public override void VisitEmphasis(Emphasis emphasis) => Wrap("em", () => base.VisitEmphasis(emphasis));

    /// <inheritdoc />
    public override void VisitStrikethrough(Strikethrough strikethrough) => Wrap("del", () => base.VisitStrikethrough(strikethrough));

    /// <inheritdoc />
    public override void VisitBlockQuote(BlockQuote blockQuote) => Wrap("blockquote", () => base.VisitBlockQuote(blockQuote));

    /// <inheritdoc />
    public override void VisitText(Text text)
    {
        ArgumentNullException.ThrowIfNull(text);
        html.Append(Escape(text.Value));
    }

    /// <inheritdoc />
    public override void VisitCode(Code code)
    {
        ArgumentNullException.ThrowIfNull(code);
        html.Append("<code>").Append(Escape(code.Value)).Append("</code>");
    }

    /// <inheritdoc />
    public override void VisitLineBreak(LineBreak lineBreak) => html.Append("<br>");

    /// <inheritdoc />
    public override void VisitSoftLineBreak(SoftLineBreak softLineBreak) => html.Append(' ');

    /// <inheritdoc />
    public override void VisitThematicBreak(ThematicBreak thematicBreak) => html.Append("<hr>");

    /// <inheritdoc />
    public override void VisitUnorderedList(UnorderedList unorderedList) => Wrap("ul", () => base.VisitUnorderedList(unorderedList));

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
        base.VisitOrderedList(orderedList);
        html.Append("</ol>");
    }

    /// <inheritdoc />
    public override void VisitListItem(ListItem listItem)
    {
        ArgumentNullException.ThrowIfNull(listItem);

        html.Append("<li>");

        if (listItem.Checked is bool isChecked)
        {
            html.Append("<input type=\"checkbox\"");
            if (isChecked)
            {
                html.Append(" checked");
            }
            html.Append("> ");
        }

        var tight = listItem.Parent is List list && list.Tight
            && listItem.Children.Count == 1 && listItem.Children[0] is Paragraph;

        if (tight)
        {
            suppressParagraph = true;
        }

        base.VisitListItem(listItem);

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

        html.Append("<pre><code");
        if (!string.IsNullOrEmpty(fencedCodeBlock.Info))
        {
            html.Append(" class=\"language-").Append(Escape(fencedCodeBlock.Info)).Append('"');
        }
        html.Append('>').Append(Escape(fencedCodeBlock.Value)).Append("</code></pre>");
    }

    /// <inheritdoc />
    public override void VisitIndentedCodeBlock(IndentedCodeBlock codeBlock)
    {
        ArgumentNullException.ThrowIfNull(codeBlock);
        html.Append("<pre><code>").Append(Escape(codeBlock.Value)).Append("</code></pre>");
    }

    /// <inheritdoc />
    public override void VisitHtmlBlock(HtmlBlock htmlBlock)
    {
        ArgumentNullException.ThrowIfNull(htmlBlock);
        html.Append("<p>").Append(Escape(htmlBlock.Value)).Append("</p>");
    }

    /// <inheritdoc />
    public override void VisitHtmlInline(HtmlInline htmlInline)
    {
        ArgumentNullException.ThrowIfNull(htmlInline);
        html.Append(Escape(htmlInline.Value));
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
        base.VisitTableCell(cell);
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

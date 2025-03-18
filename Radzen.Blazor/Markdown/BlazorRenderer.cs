using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Rendering;

namespace Radzen.Blazor.Markdown;

class BlazorRenderer(RenderTreeBuilder builder, Action<int> renderer) : NodeVisitorBase
{
    public const string Outlet = "<!--rz-outlet-{0}-->";
    public override void VisitHeading(Heading heading)
    {
        builder.OpenElement(0, $"h{heading.Level}");
        VisitChildren(heading.Children);
        builder.CloseElement();
    }

    public override void VisitTable(Table table)
    {
        builder.OpenElement(0, "table");
        base.VisitTable(table);
        builder.CloseElement();
    }

    public override void VisitTableRow(TableRow row)
    {
        builder.OpenElement(0, "tr");
        base.VisitTableRow(row);
        builder.CloseElement();
    }

    public override void VisitTableCell(TableCell cell)
    {
        builder.OpenElement(0, "td");
        base.VisitTableCell(cell);
        builder.CloseElement();
    }

    public override void VisitTableHeaderRow(TableHeaderRow header)
    {
        builder.OpenElement(0, "thead");
        builder.OpenElement(1, "tr");

        foreach (var cell in header.Cells)
        {
            builder.OpenElement(2, "th");
            VisitChildren(cell.Children);
            builder.CloseElement();
        }

        builder.CloseElement();
        builder.CloseElement();
    }

    public override void VisitIndentedCodeBlock(IndentedCodeBlock code)
    {
        builder.OpenElement(0, "pre");
        builder.OpenElement(1, "code");
        builder.AddContent(2, code.Value);
        builder.CloseElement();
        builder.CloseElement();
    }

    public override void VisitParagraph(Paragraph paragraph)
    {
        if (paragraph.Parent is ListItem item && item.Parent is List list && list.Tight)
        {
            VisitChildren(paragraph.Children);
        }
        else
        {
            builder.OpenElement(0, "p");
            VisitChildren(paragraph.Children);
            builder.CloseElement();
        }
    }

    public override void VisitBlockQuote(BlockQuote blockQuote)
    {
        builder.OpenElement(0, "blockquote");
        VisitChildren(blockQuote.Children);
        builder.CloseElement();
    }

    public override void VisitCode(Code code)
    {
        builder.OpenElement(0, "code");
        builder.AddContent(1, code.Value);
        builder.CloseElement();
    }

    public override void VisitStrong(Strong strong)
    {
        builder.OpenElement(0, "strong");
        VisitChildren(strong.Children);
        builder.CloseElement();
    }

    public override void VisitEmphasis(Emphasis emphasis)
    {
        builder.OpenElement(0, "em");
        VisitChildren(emphasis.Children);
        builder.CloseElement();
    }

    public override void VisitLink(Link link)
    {
        builder.OpenElement(0, "a");
        builder.AddAttribute(1, "href", link.Destination);
        VisitChildren(link.Children);
        builder.CloseElement();
    }

    public override void VisitImage(Image image)
    {
        builder.OpenElement(0, "img");
        builder.AddAttribute(1, "src", image.Destination);
        builder.AddAttribute(2, "alt", image.Title);
        builder.CloseElement();
    }

    public override void VisitOrderedList(OrderedList orderedList)
    {
        builder.OpenElement(0, "ol");
        VisitChildren(orderedList.Children);
        builder.CloseElement();
    }

    public override void VisitUnorderedList(UnorderedList unorderedList)
    {
        builder.OpenElement(0, "ul");
        VisitChildren(unorderedList.Children);
        builder.CloseElement();
    }

    public override void VisitListItem(ListItem listItem)
    {
        builder.OpenElement(0, "li");
        VisitChildren(listItem.Children);
        builder.CloseElement();
    }

    public override void VisitFencedCodeBlock(FencedCodeBlock fencedCodeBlock)
    {
        builder.OpenElement(0, "pre");
        builder.OpenElement(1, "code");
        builder.AddContent(2, fencedCodeBlock.Value);
        builder.CloseElement();
        builder.CloseElement();
    }

    public override void VisitThematicBreak(ThematicBreak thematicBreak)
    {
        builder.OpenElement(0, "hr");
        builder.CloseElement();
    }

    public override void VisitHtmlBlock(HtmlBlock htmlBlock)
    {
        VisitHtml(htmlBlock.Value);
    }

    public override void VisitLineBreak(LineBreak lineBreak)
    {
        builder.OpenElement(0, "br");
        builder.CloseElement();
    }

    public override void VisitText(Text text)
    {
        builder.AddContent(0, text.Value);
    }

    private static readonly Regex OutletRegex = new (@"<!--rz-outlet-(\d+)-->");

    private void VisitHtml(string html)
    {
        var match = OutletRegex.Match(html);

        if (match.Success)
        {
            var markerId = Convert.ToInt32(match.Groups[1].Value);

            renderer(markerId);
        }
        else
        {
            builder.AddMarkupContent(0, html);
        }
    }

    public override void VisitHtmlInline(HtmlInline html)
    {
        VisitHtml(html.Value);
    }
}
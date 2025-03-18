using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Radzen.Blazor.Markdown;

class BlazorMarkdownRenderer(RenderTreeBuilder builder, Action<RenderTreeBuilder, int> outlet) : NodeVisitorBase
{
    public const string Outlet = "<!--rz-outlet-{0}-->";

    public override void VisitHeading(Heading heading)
    {
        builder.OpenComponent<RadzenText>(0);
        builder.AddAttribute(1, nameof(RadzenText.ChildContent), RenderChildren(heading.Children));
        switch (heading.Level)
        {
            case 1:
                builder.AddAttribute(2, nameof(RadzenText.TextStyle), TextStyle.H1);
                break;
            case 2:
                builder.AddAttribute(2, nameof(RadzenText.TextStyle), TextStyle.H2);
                break;
            case 3:
                builder.AddAttribute(2, nameof(RadzenText.TextStyle), TextStyle.H3);
                break;
            case 4:
                builder.AddAttribute(2, nameof(RadzenText.TextStyle), TextStyle.H4);
                break;
            case 5:
                builder.AddAttribute(2, nameof(RadzenText.TextStyle), TextStyle.H5);
                break;
            case 6:
                builder.AddAttribute(2, nameof(RadzenText.TextStyle), TextStyle.H6);
                break;
        }
        builder.CloseComponent();
    }

    public override void VisitTable(Table table)
    {
        builder.OpenComponent<RadzenTable>(0);
        builder.AddAttribute(1, nameof(RadzenTable.ChildContent), RenderChildren(table.Rows));
        builder.CloseComponent();
    }

    public override void VisitTableRow(TableRow row)
    {
        builder.OpenComponent<RadzenTableRow>(0);
        builder.AddAttribute(1, nameof(RadzenTableRow.ChildContent), RenderChildren(row.Cells));
        builder.CloseComponent();
    }
    public override void VisitTableCell(TableCell cell)
    {
        builder.OpenComponent<RadzenTableCell>(0);
        builder.AddAttribute(1, nameof(RadzenTableCell.ChildContent), RenderChildren(cell.Children));
        builder.CloseComponent();
    }

    public override void VisitTableHeaderRow(TableHeaderRow header)
    {
        builder.OpenComponent<RadzenTableHeader>(0);
        builder.AddAttribute(1, nameof(RadzenTableHeader.ChildContent), new RenderFragment(headerBuilder =>
        {
            headerBuilder.OpenComponent<RadzenTableHeaderRow>(0);
            headerBuilder.AddAttribute(1, nameof(RadzenTableHeaderRow.ChildContent), new RenderFragment(headerRowBuilder =>
            {
                foreach (var cell in header.Cells)
                {
                    headerRowBuilder.OpenComponent<RadzenTableHeaderCell>(0);
                    headerRowBuilder.AddAttribute(1, nameof(RadzenTableHeaderCell.ChildContent), RenderChildren(cell.Children));
                    headerRowBuilder.CloseComponent();
                }
            }));
            headerBuilder.CloseComponent();
        }));
        builder.CloseComponent();
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
            builder.OpenComponent<RadzenText>(0);
            builder.AddAttribute(1, nameof(RadzenText.ChildContent), RenderChildren(paragraph.Children));
            builder.CloseComponent();
        }
    }

    private RenderFragment RenderChildren(IEnumerable<INode> children)
    {
        return innerBuilder =>
        {
            var inner = new BlazorMarkdownRenderer(innerBuilder, outlet);
            inner.VisitChildren(children);
        };
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
        builder.OpenComponent<RadzenLink>(0);
        builder.AddAttribute(1, nameof(RadzenLink.Path), link.Destination);
        builder.AddAttribute(2, nameof(RadzenLink.ChildContent), RenderChildren(link.Children));

        if (!string.IsNullOrEmpty(link.Title))
        {
            builder.AddAttribute(3, "title", link.Title);
        }

        builder.CloseComponent();
    }

    public override void VisitImage(Image image)
    {
        builder.OpenComponent<RadzenImage>(0);
        builder.AddAttribute(1, nameof(RadzenImage.Path), image.Destination);
        
        if (!string.IsNullOrEmpty(image.Title))
        {
            builder.AddAttribute(2, nameof(RadzenImage.AlternateText), image.Title);
        }

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

            outlet(builder, markerId);
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
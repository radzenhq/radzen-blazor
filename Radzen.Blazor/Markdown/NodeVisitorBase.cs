using System.Collections.Generic;

namespace Radzen.Blazor.Markdown;

/// <summary>
/// Base class for visitors that traverse a Markdown document.
/// </summary>
public abstract class NodeVisitorBase : INodeVisitor
{

    /// <summary>
    /// Visits a block quote by visiting its children.
    /// </summary>
    public virtual void VisitBlockQuote(BlockQuote blockQuote) => VisitChildren(blockQuote.Children);

    /// <summary>
    /// Visits a document by visiting its children.
    /// </summary>
    public virtual void VisitDocument(Document document) => VisitChildren(document.Children);

    /// <summary>
    /// Visits a heading by visiting its children.
    /// </summary>
    public virtual void VisitHeading(Heading heading) => VisitChildren(heading.Children);

    /// <summary>
    /// Visits a list item by visiting its children.
    /// </summary>
    public virtual void VisitListItem(ListItem listItem) => VisitChildren(listItem.Children);

    /// <summary>
    /// Visits an ordered list by visiting its children.
    /// </summary>
    public virtual void VisitOrderedList(OrderedList orderedList) => VisitChildren(orderedList.Children);

    /// <summary>
    /// Visits a paragraph by visiting its children.
    /// </summary>
    public virtual void VisitParagraph(Paragraph paragraph) => VisitChildren(paragraph.Children);

    /// <summary>
    /// Visits a thematic break.
    /// </summary>
    public virtual void VisitThematicBreak(ThematicBreak thematicBreak)
    {
    }

    /// <summary>
    /// Visits a text node.
    /// </summary>
    public virtual void VisitText(Text text)
    {
    }

    /// <summary>
    /// Visits a code node.
    /// </summary>
    public virtual void VisitCode(Code code)
    {
    }

    /// <summary>
    /// Visits an HTML block.
    /// </summary>
    public virtual void VisitHtmlInline(HtmlInline html)
    {
    }

    /// <summary>
    /// Visits a line break.
    /// </summary>
    public virtual void VisitLineBreak(LineBreak lineBreak)
    {
    }

    /// <summary>
    /// Visits a soft line break.
    /// </summary>
    public virtual void VisitSoftLineBreak(SoftLineBreak softLineBreak)
    {
    }

    /// <summary>
    /// Visits an ordered list by visiting its children.
    /// </summary>
    public virtual void VisitUnorderedList(UnorderedList unorderedList) => VisitChildren(unorderedList.Children);

    /// <summary>
    /// Visits an emphasis by visiting its children.
    /// </summary>
    public virtual void VisitEmphasis(Emphasis emphasis) => VisitChildren(emphasis.Children);

    /// <summary>
    /// Visits a strong by visiting its children.
    /// </summary>
    public virtual void VisitStrong(Strong strong) => VisitChildren(strong.Children);

    /// <summary>
    /// Visits a link by visiting its children.
    /// </summary>
    public virtual void VisitLink(Link link) => VisitChildren(link.Children);

    /// <summary>
    /// Visits an image by visiting its children.
    /// </summary>
    public virtual void VisitImage(Image image) => VisitChildren(image.Children);

    /// <summary>
    /// Visits a code block.
    /// </summary>
    public virtual void VisitIndentedCodeBlock(IndentedCodeBlock codeBlock)
    {
    }

    /// <summary>
    /// Visits a fenced code block.
    /// </summary>
    public virtual void VisitFencedCodeBlock(FencedCodeBlock fencedCodeBlock)
    {
    }

    /// <summary>
    /// Visits an HTML block.
    /// </summary>
    public virtual void VisitHtmlBlock(HtmlBlock htmlBlock)
    {
    }

    /// <summary>
    /// Visits a table.
    /// </summary>
    public virtual void VisitTable(Table table) => VisitChildren(table.Rows);

    /// <summary>
    /// Visits a table header row by visiting its children.
    /// </summary>
    public virtual void VisitTableHeaderRow(TableHeaderRow header) => VisitChildren(header.Cells);

    /// <summary>
    /// Visits a table row by visiting its children.
    /// </summary>
    public virtual void VisitTableRow(TableRow row) => VisitChildren(row.Cells);

    /// <summary>
    /// Visits a table cell by visiting its children.
    /// </summary>
    public virtual void VisitTableCell(TableCell cell) => VisitChildren(cell.Children);

    /// <summary>
    /// Visits a collection of nodes.
    /// </summary>
    protected void VisitChildren(IEnumerable<INode> children)
    {
        foreach (var node in children)
        {
            node.Accept(this);
        }
    }
}
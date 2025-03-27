namespace Radzen.Blazor.Markdown;


/// <summary>
/// Represents a visitor for Markdown AST nodes.
/// </summary>
public interface INodeVisitor
{
    /// <summary>
    /// Visits a heading node.
    /// </summary>
    void VisitHeading(Heading heading);

    /// <summary>
    /// Visits a paragraph node.
    /// </summary>
    void VisitParagraph(Paragraph paragraph);

    /// <summary>
    /// Visits a block quote node.
    /// </summary>
    void VisitBlockQuote(BlockQuote blockQuote);

    /// <summary>
    /// Visits a document node.
    /// </summary>
    void VisitDocument(Document document);

    /// <summary>
    /// Visits an unordered list node.
    /// </summary>
    void VisitUnorderedList(UnorderedList unorderedList);

    /// <summary>
    /// Visits a list item node.
    /// </summary>
    void VisitListItem(ListItem listItem);

    /// <summary>
    /// Visits a text node.
    /// </summary>
    void VisitText(Text text);

    /// <summary>
    /// Visits an ordered list node.
    /// </summary>
    void VisitOrderedList(OrderedList orderedList);

    /// <summary>
    /// Visits an emphasis node.
    /// </summary>
    void VisitEmphasis(Emphasis emphasis);

    /// <summary>
    /// Visits a strong node.
    /// </summary>
    void VisitStrong(Strong strong);

    /// <summary>
    /// Visits a code node.
    /// </summary>
    void VisitCode(Code code);

    /// <summary>
    /// Visits a link node.
    /// </summary>
    void VisitLink(Link link);

    /// <summary>
    /// Visits an image node.
    /// </summary>
    void VisitImage(Image image);

    /// <summary>
    /// Visits an HTML inline node.
    /// </summary>
    void VisitHtmlInline(HtmlInline html);

    /// <summary>
    /// Visits a line break node.
    /// </summary>
    void VisitLineBreak(LineBreak lineBreak);

    /// <summary>
    /// Visits a soft line break node.
    /// </summary>
    void VisitSoftLineBreak(SoftLineBreak softLineBreak);

    /// <summary>
    /// Visits a thematic break node.
    /// </summary>
    void VisitThematicBreak(ThematicBreak thematicBreak);

    /// <summary>
    /// Visits an indented code block node.
    /// </summary>
    void VisitIndentedCodeBlock(IndentedCodeBlock codeBlock);

    /// <summary>
    /// Visits a fenced code block node.
    /// </summary>
    void VisitFencedCodeBlock(FencedCodeBlock fencedCodeBlock);

    /// <summary>
    /// Visits an HTML block node.
    /// </summary>
    void VisitHtmlBlock(HtmlBlock htmlBlock);
    
    /// <summary>
    /// Visits a table node.
    /// </summary>
    void VisitTable(Table table);

    /// <summary>
    /// Visits a table header row node.
    /// </summary>
    void VisitTableHeaderRow(TableHeaderRow header);

    /// <summary>
    /// Visits a table row node.
    /// </summary>
    void VisitTableRow(TableRow row);

    /// <summary>
    /// Visits a table cell node.
    /// </summary>
    void VisitTableCell(TableCell cell);
}
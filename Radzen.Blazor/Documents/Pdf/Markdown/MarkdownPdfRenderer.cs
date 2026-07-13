using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Radzen.Documents.Pdf.Markdown;

/// <summary>
/// Walks a parsed Markdown document and appends the equivalent PDF blocks to a target
/// <see cref="BlockCollection"/>. Raw HTML nodes are skipped (the base visitor no-ops on them),
/// which is out of scope for this renderer.
/// </summary>
internal sealed class MarkdownPdfRenderer(BlockCollection target, MarkdownPdfOptions options) : Radzen.Documents.Markdown.NodeVisitorBase
{
    private InlineCollection? currentInlines;
    private string? currentLink;
    private int boldDepth;
    private int italicDepth;
    private double quoteIndent;

    /// <inheritdoc />
    public override void VisitHeading(Radzen.Documents.Markdown.Heading heading)
    {
        var paragraph = target.AddParagraph();
        paragraph.Font.Name = options.ResolvedHeadingFontName;
        paragraph.Font.Bold = true;
        paragraph.Font.Size = options.HeadingFontSize(heading.Level);
        paragraph.LeftIndent = Unit.FromPoint(quoteIndent);
        RenderInlines(heading.Children, paragraph.Inlines);
    }

    /// <inheritdoc />
    public override void VisitParagraph(Radzen.Documents.Markdown.Paragraph paragraph)
    {
        // A paragraph whose only content is an image (an image on its own line) becomes an
        // Image block; images mixed with text become inline images via VisitImage instead.
        if (paragraph.Children.Count == 1 && paragraph.Children[0] is Radzen.Documents.Markdown.Image image)
        {
            AddImageBlock(image);
            return;
        }

        var pdfParagraph = target.AddParagraph();
        pdfParagraph.Font.Name = options.BodyFontName;
        pdfParagraph.LeftIndent = Unit.FromPoint(quoteIndent);
        RenderInlines(paragraph.Children, pdfParagraph.Inlines);
    }

    /// <inheritdoc />
    public override void VisitBlockQuote(Radzen.Documents.Markdown.BlockQuote blockQuote)
    {
        quoteIndent += options.BlockQuoteIndent;
        VisitChildren(blockQuote.Children);
        quoteIndent -= options.BlockQuoteIndent;
    }

    /// <inheritdoc />
    public override void VisitThematicBreak(Radzen.Documents.Markdown.ThematicBreak thematicBreak)
    {
        // Paragraph has no border/background API, so the rule is drawn as a 1x1 table
        // with a top border rather than a bordered paragraph.
        var table = target.AddTable();
        table.Columns.Add();
        var row = table.Rows.Add();
        row.Cells[0].Borders.Top.Style = BorderStyle.Solid;
        row.Cells[0].Borders.Top.Width = 1;
        row.Cells[0].Text = string.Empty;
    }

    /// <inheritdoc />
    public override void VisitFencedCodeBlock(Radzen.Documents.Markdown.FencedCodeBlock fencedCodeBlock)
    {
        AddCodeParagraph(fencedCodeBlock.Value);
    }

    /// <inheritdoc />
    public override void VisitIndentedCodeBlock(Radzen.Documents.Markdown.IndentedCodeBlock codeBlock)
    {
        AddCodeParagraph(codeBlock.Value);
    }

    private void AddCodeParagraph(string text)
    {
        // No paragraph background/border is applied here (see VisitThematicBreak); a code
        // block renders as a plain monospace paragraph instead of a shaded box.
        var paragraph = target.AddParagraph();
        paragraph.Font.Name = options.ResolvedMonospaceFontName;
        paragraph.LeftIndent = Unit.FromPoint(quoteIndent);
        paragraph.Text = text.TrimEnd('\n');
    }

    /// <inheritdoc />
    public override void VisitUnorderedList(Radzen.Documents.Markdown.UnorderedList unorderedList)
    {
        var list = target.AddList(ListStyle.Bullet);
        list.Font.Name = options.BodyFontName;
        AddListItems(list, unorderedList, string.Empty);
    }

    /// <inheritdoc />
    public override void VisitOrderedList(Radzen.Documents.Markdown.OrderedList orderedList)
    {
        var list = target.AddList(ListStyle.Number);
        list.Font.Name = options.BodyFontName;
        AddListItems(list, orderedList, string.Empty);
    }

    // The PDF List type is single-level, so a nested list is flattened into the parent
    // list: its items become additional items of the same list, indented one extra level
    // per nesting depth instead of being dropped.
    private void AddListItems(List list, Radzen.Documents.Markdown.List source, string indent)
    {
        foreach (var child in source.Children)
        {
            if (child is not Radzen.Documents.Markdown.ListItem item)
            {
                continue;
            }

            var pdfItem = list.AddItem();
            var pendingIndent = indent;

            foreach (var block in item.Children)
            {
                switch (block)
                {
                    case Radzen.Documents.Markdown.Paragraph paragraph:
                        AppendIndent(pdfItem.Inlines, ref pendingIndent);
                        RenderInlines(paragraph.Children, pdfItem.Inlines);
                        break;
                    case Radzen.Documents.Markdown.List nestedList:
                        AddListItems(list, nestedList, indent + "    ");
                        break;
                    case Radzen.Documents.Markdown.FencedCodeBlock fenced:
                        AppendIndent(pdfItem.Inlines, ref pendingIndent);
                        pdfItem.Inlines.Add(fenced.Value.TrimEnd('\n')).Font.Name = options.ResolvedMonospaceFontName;
                        break;
                    case Radzen.Documents.Markdown.IndentedCodeBlock indented:
                        AppendIndent(pdfItem.Inlines, ref pendingIndent);
                        pdfItem.Inlines.Add(indented.Value.TrimEnd('\n')).Font.Name = options.ResolvedMonospaceFontName;
                        break;
                    case Radzen.Documents.Markdown.BlockQuote quote:
                        AppendIndent(pdfItem.Inlines, ref pendingIndent);
                        RenderInlines(FlattenInlines(quote), pdfItem.Inlines);
                        break;
                }
            }
        }
    }

    private static void AppendIndent(InlineCollection inlines, ref string pendingIndent)
    {
        if (pendingIndent.Length > 0)
        {
            inlines.Add(pendingIndent);
            pendingIndent = string.Empty;
        }
    }

    private static IEnumerable<Radzen.Documents.Markdown.Inline> FlattenInlines(Radzen.Documents.Markdown.BlockContainer container)
    {
        foreach (var block in container.Children)
        {
            if (block is Radzen.Documents.Markdown.Paragraph paragraph)
            {
                foreach (var inline in paragraph.Children)
                {
                    yield return inline;
                }
            }
            else if (block is Radzen.Documents.Markdown.BlockContainer nested)
            {
                foreach (var inline in FlattenInlines(nested))
                {
                    yield return inline;
                }
            }
        }
    }

    /// <inheritdoc />
    public override void VisitTable(Radzen.Documents.Markdown.Table table)
    {
        if (table.Rows.Count == 0)
        {
            return;
        }

        var pdfTable = target.AddTable();
        pdfTable.Font.Name = options.BodyFontName;

        var columnCount = table.Rows[0].Cells.Count;
        for (var i = 0; i < columnCount; i++)
        {
            pdfTable.Columns.Add();
        }

        foreach (var row in table.Rows)
        {
            var pdfRow = pdfTable.Rows.Add();
            pdfRow.IsHeader = row is Radzen.Documents.Markdown.TableHeaderRow;

            for (var i = 0; i < row.Cells.Count && i < pdfRow.Cells.Count; i++)
            {
                var cell = row.Cells[i];
                var pdfCell = pdfRow.Cells[i];
                var paragraph = pdfCell.Blocks.AddParagraph();
                paragraph.Font.Name = options.BodyFontName;
                paragraph.Font.Bold = pdfRow.IsHeader;
                pdfCell.Alignment = ToHorizontalAlignment(cell.Alignment);
                RenderInlines(cell.Children, paragraph.Inlines);
            }
        }
    }

    private static HorizontalAlignment ToHorizontalAlignment(Radzen.Documents.Markdown.TableCellAlignment alignment) => alignment switch
    {
        Radzen.Documents.Markdown.TableCellAlignment.Center => HorizontalAlignment.Center,
        Radzen.Documents.Markdown.TableCellAlignment.Right => HorizontalAlignment.Right,
        _ => HorizontalAlignment.Left,
    };

    private void RenderInlines(IEnumerable<Radzen.Documents.Markdown.Inline> children, InlineCollection inlines)
    {
        var previous = currentInlines;
        currentInlines = inlines;
        VisitChildren(children);
        currentInlines = previous;
    }

    /// <inheritdoc />
    public override void VisitText(Radzen.Documents.Markdown.Text text)
    {
        AddRun(text.Value);
    }

    /// <inheritdoc />
    public override void VisitCode(Radzen.Documents.Markdown.Code code)
    {
        var run = AddRun(code.Value);
        if (run is null)
        {
            return;
        }

        run.Font.Name = options.ResolvedMonospaceFontName;
    }

    /// <inheritdoc />
    public override void VisitSoftLineBreak(Radzen.Documents.Markdown.SoftLineBreak softLineBreak)
    {
        AddRun(" ");
    }

    /// <inheritdoc />
    public override void VisitLineBreak(Radzen.Documents.Markdown.LineBreak lineBreak)
    {
        AddRun("\n");
    }

    /// <inheritdoc />
    public override void VisitStrong(Radzen.Documents.Markdown.Strong strong)
    {
        boldDepth++;
        VisitChildren(strong.Children);
        boldDepth--;
    }

    /// <inheritdoc />
    public override void VisitEmphasis(Radzen.Documents.Markdown.Emphasis emphasis)
    {
        italicDepth++;
        VisitChildren(emphasis.Children);
        italicDepth--;
    }

    /// <inheritdoc />
    public override void VisitLink(Radzen.Documents.Markdown.Link link)
    {
        var previous = currentLink;
        currentLink = link.Destination;
        VisitChildren(link.Children);
        currentLink = previous;
    }

    /// <inheritdoc />
    public override void VisitImage(Radzen.Documents.Markdown.Image image)
    {
        if (currentInlines is null || string.IsNullOrEmpty(image.Destination))
        {
            return;
        }

        var data = options.ImageResolver?.Invoke(image.Destination);
        if (data is null)
        {
            // Render the alt text so an unresolved image does not vanish without a trace.
            VisitChildren(image.Children);
            return;
        }

        currentInlines.AddImage(new MemoryStream(data));
    }

    private void AddImageBlock(Radzen.Documents.Markdown.Image image)
    {
        if (string.IsNullOrEmpty(image.Destination))
        {
            return;
        }

        var data = options.ImageResolver?.Invoke(image.Destination);
        if (data is null)
        {
            // Fall back to the alt text as a paragraph rather than dropping the block silently.
            if (AltText(image).Length > 0)
            {
                var paragraph = target.AddParagraph();
                paragraph.Font.Name = options.BodyFontName;
                paragraph.LeftIndent = Unit.FromPoint(quoteIndent);
                RenderInlines(image.Children, paragraph.Inlines);
            }

            return;
        }

        var pdfImage = target.AddImage(new MemoryStream(data));
        var alt = AltText(image);
        if (alt.Length > 0)
        {
            pdfImage.AlternateText = alt;
        }
    }

    // Flattens an image's inline alt-text children to plain text for the Figure's alternate description.
    private static string AltText(Radzen.Documents.Markdown.Image image)
    {
        var builder = new StringBuilder();
        CollectText(image.Children, builder);
        return builder.ToString();
    }

    private static void CollectText(IEnumerable<Radzen.Documents.Markdown.Inline> nodes, StringBuilder builder)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case Radzen.Documents.Markdown.Text text:
                    builder.Append(text.Value);
                    break;
                case Radzen.Documents.Markdown.Code code:
                    builder.Append(code.Value);
                    break;
                case Radzen.Documents.Markdown.InlineContainer container:
                    CollectText(container.Children, builder);
                    break;
            }
        }
    }

    private Run? AddRun(string text)
    {
        if (currentInlines is null || text.Length == 0)
        {
            return null;
        }

        var run = currentInlines.Add(text);
        run.Font.Name = options.BodyFontName;
        run.Font.Bold = boldDepth > 0;
        run.Font.Italic = italicDepth > 0;

        if (currentLink is not null)
        {
            run.Link = currentLink;
        }

        return run;
    }
}

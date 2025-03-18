using System;
using System.Text;
using System.Xml;

namespace Radzen.Blazor.Markdown.Tests;

public class XmlVisitor : NodeVisitorBase, IDisposable
{
    private readonly XmlWriter writer;

    private XmlVisitor(StringBuilder xml)
    {
        writer = XmlWriter.Create(xml, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true, IndentChars = "    ", });
    }

    public void Dispose()
    {
        writer.Dispose();
    }

    public void Close()
    {
        writer.Close();
    }

    public static string ToXml(Document document)
    {
        var xml = new StringBuilder();

        using var visitor = new XmlVisitor(xml);

        document.Accept(visitor);

        visitor.Close();

        return xml.ToString()!;
    }

    public override void VisitBlockQuote(BlockQuote blockQuote)
    {
        writer.WriteStartElement("block_quote");
        base.VisitBlockQuote(blockQuote);
        writer.WriteEndElement();
    }

    public override void VisitDocument(Document document)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement("document");
        base.VisitDocument(document);
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    public override void VisitHeading(Heading heading)
    {
        writer.WriteStartElement($"heading");
        writer.WriteAttributeString("level", heading.Level.ToString());
        base.VisitHeading(heading);
        writer.WriteEndElement();
    }

    public override void VisitListItem(ListItem listItem)
    {
        writer.WriteStartElement("item");
        base.VisitListItem(listItem);
        writer.WriteEndElement();
    }

    public override void VisitParagraph(Paragraph paragraph)
    {
        writer.WriteStartElement("paragraph");
        base.VisitParagraph(paragraph);
        writer.WriteEndElement();
    }

    public override void VisitUnorderedList(UnorderedList unorderedList)
    {
        writer.WriteStartElement("list");
        writer.WriteAttributeString("type", "bullet");
        writer.WriteAttributeString("tight", unorderedList.Tight.ToString().ToLowerInvariant());
        base.VisitUnorderedList(unorderedList);
        writer.WriteEndElement();
    }

    public override void VisitText(Text text)
    {
        writer.WriteElementString("text", text.Value);
    }

    public override void VisitOrderedList(OrderedList orderedList)
    {
        writer.WriteStartElement("list");
        writer.WriteAttributeString("type", "ordered");
        writer.WriteAttributeString("start", orderedList.Start.ToString());
        writer.WriteAttributeString("tight", orderedList.Tight.ToString().ToLowerInvariant());
        base.VisitOrderedList(orderedList);
        writer.WriteEndElement();
    }

    public override void VisitLink(Link link)
    {
        writer.WriteStartElement("link");
        writer.WriteAttributeString("destination", link.Destination);
        writer.WriteAttributeString("title", link.Title);
        base.VisitLink(link);
        writer.WriteEndElement();
    }

    public override void VisitImage(Image image)
    {
        writer.WriteStartElement("image");
        writer.WriteAttributeString("destination", image.Destination);
        writer.WriteAttributeString("title", image.Title);
        base.VisitImage(image);
        writer.WriteEndElement();
    }

    public override void VisitEmphasis(Emphasis emphasis)
    {
        writer.WriteStartElement("emph");
        base.VisitEmphasis(emphasis);
        writer.WriteEndElement();
    }

    public override void VisitStrong(Strong strong)
    {
        writer.WriteStartElement("strong");
        base.VisitStrong(strong);
        writer.WriteEndElement();
    }

    public override void VisitCode(Code code)
    {
        writer.WriteElementString("code", code.Value);
    }

    public override void VisitHtmlInline(HtmlInline html)
    {
        writer.WriteElementString("html_inline", html.Value);
    }

    public override void VisitLineBreak(LineBreak lineBreak)
    {
        writer.WriteElementString("linebreak", string.Empty);
    }

    public override void VisitSoftLineBreak(SoftLineBreak softLineBreak)
    {
        writer.WriteElementString("softbreak", string.Empty);
    }

    public override void VisitThematicBreak(ThematicBreak thematicBreak)
    {
        writer.WriteElementString("thematic_break", string.Empty);
    }

    public override void VisitIndentedCodeBlock(IndentedCodeBlock codeBlock)
    {
        writer.WriteElementString("code_block", codeBlock.Value);
    }

    public override void VisitFencedCodeBlock(FencedCodeBlock fencedCodeBlock)
    {
        writer.WriteStartElement("code_block");
        if (!string.IsNullOrEmpty(fencedCodeBlock.Info))
        {
            writer.WriteAttributeString("info", fencedCodeBlock.Info);
        }
        writer.WriteString(fencedCodeBlock.Value);
        writer.WriteEndElement();
    }

    public override void VisitHtmlBlock(HtmlBlock htmlBlock)
    {
        writer.WriteElementString("html_block", htmlBlock.Value);
    }

    public override void VisitTable(Table table)
    {
        writer.WriteStartElement("table");
        base.VisitTable(table);
        writer.WriteEndElement();
    }

    public override void VisitTableHeaderRow(TableHeaderRow header)
    {
        writer.WriteStartElement("header");
        base.VisitTableHeaderRow(header);
        writer.WriteEndElement();
    }

    public override void VisitTableRow(TableRow row)
    {
        writer.WriteStartElement("row");
        base.VisitTableRow(row);
        writer.WriteEndElement();
    }

    public override void VisitTableCell(TableCell cell)
    {
        writer.WriteStartElement("cell");
        if (cell.Alignment != TableCellAlignment.None)
        {
            writer.WriteAttributeString("align", cell.Alignment.ToString().ToLowerInvariant());
        }
        base.VisitTableCell(cell);
        writer.WriteEndElement();
    }
}
#nullable enable
using System.Collections.Generic;
using System.Globalization;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Compact content-stream emission: generated operands are written at 1/1000-unit
// precision (at most 3 fractional digits) so coordinate, color and matrix strings
// stay short. Fidelity is the invariant - the extractable text of a compacted
// document is exactly what it was, and every text object keeps its single-Td form.
public class ContentStreamCompactionTests
{
    private static DocumentBuilder AuthorInvoice()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);

        var section = builder.Sections.Add();
        var heading = section.Blocks.AddParagraph("Invoice INV-42");
        heading.Alignment = HorizontalAlignment.Right;

        var table = section.Blocks.AddTable();
        table.Borders.Width = 0.75;
        table.Columns.Add();
        table.Columns.Add();

        var header = table.Rows.Add();
        header.IsHeader = true;
        header.Background = Color.FromRgb(64, 96, 160);
        TableLayoutSupport.Fill(header.Cells[0], "Description");
        var priceHeader = TableLayoutSupport.Fill(header.Cells[1], "Amount");
        priceHeader.Alignment = HorizontalAlignment.Right;

        foreach (var (name, amount) in new[] { ("Consulting", "1234.50"), ("Hosting", "89.99"), ("Support", "375.00") })
        {
            var row = table.Rows.Add();
            TableLayoutSupport.Fill(row.Cells[0], name);
            var price = TableLayoutSupport.Fill(row.Cells[1], amount);
            price.Alignment = HorizontalAlignment.Right;
        }

        return builder;
    }

    private static IEnumerable<ContentToken> Numbers(byte[] content)
    {
        foreach (var op in ContentStreamTokenizer.Parse(content))
        {
            foreach (var operand in op.Operands)
            {
                if (operand.Kind == ContentTokenKind.Number)
                {
                    yield return operand;
                }
            }
        }
    }

    private static int FractionDigits(string text)
    {
        var dot = text.IndexOf('.', System.StringComparison.Ordinal);
        return dot < 0 ? 0 : text.Length - dot - 1;
    }

    [Fact]
    public void GeneratedOperands_CarryAtMostThreeDecimals()
    {
        var content = ContentTestHelpers.PageContent(BuildTestSupport.Read(AuthorInvoice()), 0);

        var offenders = new List<string>();
        foreach (var number in Numbers(content))
        {
            if (FractionDigits(number.Text) > 3)
            {
                offenders.Add(number.Text);
            }
        }

        Assert.True(offenders.Count == 0, "operands over 3 decimals: " + string.Join(", ", offenders));
    }

    // A fractional right-aligned position is still emitted (proving the doc is not
    // trivially all-integer coordinates) but never with a long fractional tail.
    [Fact]
    public void FractionalPosition_IsRoundedNotIntegerized()
    {
        var content = ContentTestHelpers.PageContent(BuildTestSupport.Read(AuthorInvoice()), 0);

        var sawFraction = false;
        foreach (var number in Numbers(content))
        {
            var digits = FractionDigits(number.Text);
            Assert.True(digits <= 3, $"operand '{number.Text}' has {digits} decimals");
            sawFraction |= digits > 0;
        }

        Assert.True(sawFraction, "expected at least one fractional operand from right-aligned layout");
    }

    // One text object per drawn run: a single BT...ET enclosing exactly one Td and
    // one Tj. This pins the current text-object shape so a later BT/ET batching
    // change is a deliberate, reviewed edit rather than an accident.
    [Fact]
    public void EveryTextObject_HasSingleTdAndTj()
    {
        var content = ContentTestHelpers.PageContent(BuildTestSupport.Read(AuthorInvoice()), 0);
        var operators = ContentStreamTokenizer.Operators(content);

        var depth = 0;
        var tdInObject = 0;
        var tjInObject = 0;
        var objects = 0;
        foreach (var op in operators)
        {
            switch (op)
            {
                case "BT":
                    depth++;
                    tdInObject = 0;
                    tjInObject = 0;
                    break;
                case "Td":
                case "Tm":
                    tdInObject++;
                    break;
                case "Tj":
                    tjInObject++;
                    break;
                case "ET":
                    Assert.Equal(1, tdInObject);
                    Assert.Equal(1, tjInObject);
                    objects++;
                    depth--;
                    break;
            }
        }

        Assert.Equal(0, depth);
        Assert.True(objects > 0, "expected at least one text object");
    }

    [Fact]
    public void CompactedDocument_ExtractsSameText()
    {
        var text = BuildTestSupport.Reload(AuthorInvoice()).ExtractText();

        foreach (var expected in new[] { "Invoice INV-42", "Description", "Amount", "Consulting", "1234.50", "Hosting", "89.99", "Support", "375.00" })
        {
            Assert.True(text.Contains(expected, System.StringComparison.Ordinal), $"extracted text missing '{expected}'");
        }
    }
}

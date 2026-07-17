#nullable enable
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class TaggedMultiPageMcidTests
{
    private static DocumentBuilder AuthorManyParagraphs(int count)
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);

        var section = builder.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(300), Unit.FromPoint(160));
        section.Margin = Unit.FromPoint(20);

        for (var i = 0; i < count; i++)
        {
            BuildTestSupport.AddText(section, $"Paragraph number {i}", BuildTestSupport.Latin);
        }

        return builder;
    }

    private static List<int> McidsInOrder(DocumentReader reader, DictionaryObject page)
    {
        var result = new List<int>();
        foreach (var operation in ContentStreamTokenizer.Parse(BuildTestSupport.Content(reader, page)))
        {
            if (operation.Operator != "BDC")
            {
                continue;
            }

            for (var i = 1; i < operation.Operands.Count - 1; i++)
            {
                if (operation.Operands[i].Kind == ContentTokenKind.Name
                    && operation.Operands[i].Text == "MCID"
                    && operation.Operands[i + 1].Kind == ContentTokenKind.Number)
                {
                    result.Add((int)operation.Operands[i + 1].Number);
                    break;
                }
            }
        }

        return result;
    }

    [Fact]
    public void EachPageEmitsDenseMcidSequenceStartingAtZero()
    {
        const int Count = 40;
        var reader = BuildTestSupport.Read(AuthorManyParagraphs(Count));
        var pages = BuildTestSupport.PageLeaves(reader);

        Assert.True(pages.Count > 1, "content must span multiple pages");

        var total = 0;
        foreach (var (page, _) in pages)
        {
            var mcids = McidsInOrder(reader, page);
            Assert.NotEmpty(mcids);
            Assert.Equal(Enumerable.Range(0, mcids.Count).ToList(), mcids);
            total += mcids.Count;
        }

        Assert.Equal(Count, total);
    }
}

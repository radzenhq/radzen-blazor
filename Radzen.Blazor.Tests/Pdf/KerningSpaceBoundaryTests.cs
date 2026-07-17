#nullable enable
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class KerningSpaceBoundaryTests
{
    private static string Content(DocumentBuilder builder)
        => Encoding.Latin1.GetString(ContentTestHelpers.PageContent(BuildTestSupport.Read(builder), 0));

    private static List<(string Kind, char Glyph)> ShowTokens(string content)
    {
        var array = Regex.Match(content, @"\[(.*?)\]\s*TJ", RegexOptions.Singleline);
        Assert.True(array.Success, "a TJ show array must be emitted with kerning on");

        var tokens = new List<(string, char)>();
        foreach (Match m in Regex.Matches(array.Groups[1].Value, @"\(([^)]*)\)|(-?\d+(?:\.\d+)?)"))
        {
            if (m.Groups[1].Success)
            {
                tokens.Add(("g", m.Groups[1].Value.Length == 1 ? m.Groups[1].Value[0] : '\0'));
            }
            else
            {
                tokens.Add(("n", '\0'));
            }
        }

        return tokens;
    }

    [Fact]
    public void CoalescedRun_DoesNotKernAcrossSpace()
    {
        var builder = new DocumentBuilder { Fonts = { EnableKerning = true } };
        var section = builder.Sections.Add();
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("To Wa");
        section.Blocks.Add(paragraph);

        var tokens = ShowTokens(Content(builder));

        Assert.Contains(tokens, t => t.Kind == "n");

        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Kind == "g" && tokens[i].Glyph == ' ')
            {
                Assert.False(i > 0 && tokens[i - 1].Kind == "n", "no kern into the space glyph");
                Assert.False(i + 1 < tokens.Count && tokens[i + 1].Kind == "n", "no kern out of the space glyph");
            }
        }
    }
}

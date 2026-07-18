#nullable enable

using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Content;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ContentOperatorClassificationAgreementTests
{
    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    public static readonly TheoryData<string> StateOperators = new()
    {
        "q", "Q", "1 0 0 1 0 0 cm", "1 w", "0 0 0 rg", "0 0 0 RG", "0 g", "0 G",
        "0 0 0 0 k", "0 0 0 0 K", "/CS0 cs", "/CS0 CS", "1 0 0 scn", "1 0 0 sc",
        "1 0 0 SCN", "1 0 0 SC", "[] 0 d", "BT", "ET", "/F0 12 Tf", "10 TL",
        "0 10 TD", "0 10 Td", "1 0 0 1 0 0 Tm", "T*", "1 Tc", "1 Tw", "100 Tz",
        "0 Ts", "0 Tr", "/Tag BDC", "/Tag BMC", "EMC",
    };

    public static readonly TheoryData<string> UnknownOperators = new()
    {
        "1 J", "1 j", "10 M", "0 ri", "1 i", "/GS0 gs", "/Sh0 sh", "/Tag DP", "MP",
    };

    [Theory]
    [MemberData(nameof(StateOperators))]
    public void StateOperator_ProducesNoElement_AndMapsSafely(string stream)
    {
        var content = Ascii(stream);
        var elements = new ContentCollection();
        ContentInterpreter.Materialize(content, elements);

        Assert.DoesNotContain(elements, e => e is RawContent);
        ContentEditor.Map(content, elements);
    }

    [Theory]
    [MemberData(nameof(UnknownOperators))]
    public void UnknownOperator_ProducesRawElement_AndMapsSafely(string stream)
    {
        var content = Ascii(stream);
        var elements = new ContentCollection();
        ContentInterpreter.Materialize(content, elements);

        Assert.Single(elements.OfType<RawContent>());
        ContentEditor.Map(content, elements);
    }

    [Fact]
    public void ClassificationRolesArePartitioned()
    {
        var operators = new[]
        {
            "q", "Q", "cm", "w", "rg", "RG", "g", "G", "k", "K", "cs", "CS", "scn", "sc",
            "SCN", "SC", "d", "BT", "ET", "Tf", "TL", "TD", "Td", "Tm", "T*", "Tc", "Tw",
            "Tz", "Ts", "Tr", "BDC", "BMC", "EMC",
            "m", "l", "c", "v", "y", "re", "h", "W", "W*", "S", "s", "f", "F", "f*", "B",
            "B*", "b", "b*", "n", "Do", "Tj", "TJ", "'", "\"",
            "J", "j", "M", "ri", "i", "gs", "sh", "DP", "MP",
        };

        foreach (var op in operators)
        {
            var roles = 0;
            if (ContentOperatorClass.IsStateOperator(op)) roles++;
            if (ContentOperatorClass.IsElementProducing(op)) roles++;
            if (ContentOperatorClass.IsUnknown(op)) roles++;
            Assert.Equal(1, roles);
        }
    }
}

#nullable enable
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Xunit;

using TokenKind = Radzen.Documents.Pdf.Objects.TokenKind;

namespace Radzen.Blazor.Pdf.Tests;

public class LexerAllocationTests
{
    [Fact]
    public void EscapedName_StillDecodes()
    {
        var token = new Lexer(Encoding.Latin1.GetBytes("/A#20B#23C"), 0).Next();

        Assert.Equal(TokenKind.Name, token.Kind);
        Assert.Equal("A B#C", token.Text);
    }

}

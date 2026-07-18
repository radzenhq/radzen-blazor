using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts;
using Token = Radzen.Documents.Pdf.Content.ContentTokenizer.Token;

namespace Radzen.Documents.Pdf.Content;

internal sealed class ContentTextWalker
{
    private readonly ContentStateMachine machine;

    private ContentTextWalker(ContentStateMachine machine) => this.machine = machine;

    public Token Operator { get; private set; }

    public int OperandStart { get; private set; }

    public int ArrayStart { get; private set; }

    public delegate double ShowHandler(ContentTextWalker walker, string op, List<Token> operands, List<Token> array, int operatorIndex);

    public Matrix Ctm => machine.Ctm;

    public Matrix TextMatrix => machine.TextMatrix;

    public ReverseFont? Font => machine.Text.Font;

    public string? FontName => machine.Text.FontName;

    public double FontSize => machine.Text.FontSize;

    public double HorizontalScale => machine.Text.Spacing.HorizontalScale;

    public double CharSpacing => machine.Text.Spacing.CharSpacing;

    public double WordSpacing => machine.Text.Spacing.WordSpacing;

    public double Rise => machine.Text.Rise;

    public static void Walk(byte[] content, IReadOnlyDictionary<string, ReverseFont>? fonts, ShowHandler show, ContentTokenizer.Cache? cache = null)
    {
        var tokens = ContentTokenizer.Tokenize(content, cache);
        var machine = new ContentStateMachine(fonts, ReverseFont.WinAnsi);
        var walker = new ContentTextWalker(machine);
        var operatorIndex = 0;

        foreach (var frame in ContentOperandScan.Scan(tokens))
        {
            if (frame.IsInlineImage)
            {
                continue;
            }

            var op = frame.Operator.Text;
            if (!machine.Apply(op, frame.Operands) && ContentShows.IsShow(op))
            {
                walker.Operator = frame.Operator;
                walker.OperandStart = frame.OperandStart;
                walker.ArrayStart = frame.ArrayStart;
                machine.Advance(show(walker, op!, frame.Operands, frame.Array, operatorIndex++));
            }
        }
    }
}

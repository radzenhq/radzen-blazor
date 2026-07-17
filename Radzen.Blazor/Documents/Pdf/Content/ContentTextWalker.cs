using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts;
using Token = Radzen.Documents.Pdf.Content.ContentTokenizer.Token;
using TokenKind = Radzen.Documents.Pdf.Content.ContentTokenizer.TokenKind;

namespace Radzen.Documents.Pdf.Content;

// Walks a content stream's token grammar over the shared ContentStateMachine and hands each
// text-show operator to a consumer. The consumer decodes the shown string however it needs
// and returns the horizontal text-space advance the pen moved, which steps the text matrix.
internal sealed class ContentTextWalker
{
    private readonly ContentStateMachine machine;

    private ContentTextWalker(ContentStateMachine machine) => this.machine = machine;

    // The show operator token being handled: its End is where the operator's bytes stop, past
    // any comment or whitespace between the operand and the operator that the tokenizer skipped.
    public Token Operator { get; private set; }

    // op is the show operator ("Tj", "TJ", "'" or "\""); array holds the flattened
    // string/number elements of the last TJ array and is only meaningful when op is "TJ".
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
        var operands = new List<Token>();
        var array = new List<Token>();

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            switch (token.Kind)
            {
                case TokenKind.Number:
                case TokenKind.Name:
                case TokenKind.String:
                    operands.Add(token);
                    continue;
                case TokenKind.ArrayStart:
                    array.Clear();
                    for (i++; i < tokens.Count && tokens[i].Kind != TokenKind.ArrayEnd; i++)
                    {
                        if (tokens[i].Kind is TokenKind.String or TokenKind.Number)
                        {
                            array.Add(tokens[i]);
                        }
                    }

                    continue;
                case TokenKind.ArrayEnd:
                    continue;
                case TokenKind.DictStart:
                    for (var depth = 1; depth > 0 && ++i < tokens.Count;)
                    {
                        if (tokens[i].Kind == TokenKind.DictStart)
                        {
                            depth++;
                        }
                        else if (tokens[i].Kind == TokenKind.DictEnd)
                        {
                            depth--;
                        }
                    }

                    continue;
                case TokenKind.DictEnd:
                    continue;
                case TokenKind.Operator:
                    break;
            }

            if (!machine.Apply(token.Text, operands) && ContentShows.IsShow(token.Text))
            {
                walker.Operator = token;
                machine.Advance(show(walker, token.Text!, operands, array, operatorIndex++));
            }

            operands.Clear();
        }
    }
}

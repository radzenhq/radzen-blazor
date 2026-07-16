using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts;
using static Radzen.Documents.Pdf.Content.ContentOperands;
using Token = Radzen.Documents.Pdf.Content.ContentTokenizer.Token;
using TokenKind = Radzen.Documents.Pdf.Content.ContentTokenizer.TokenKind;

namespace Radzen.Documents.Pdf.Content;

// Walks a content stream's token grammar tracking the graphics CTM stack and the text
// state (matrices, font, leading, spacing, scale, rise), and hands each text-show
// operator to a consumer. The consumer decodes the shown string however it needs and
// returns the horizontal text-space advance the pen moved, which steps the text matrix.
internal sealed class ContentTextWalker
{
    // op is the show operator ("Tj", "TJ", "'" or "\""); array holds the flattened
    // string/number elements of the last TJ array and is only meaningful when op is "TJ".
    public delegate double ShowHandler(ContentTextWalker walker, string op, List<Token> operands, List<Token> array, int operatorIndex);

    private ContentTextWalker()
    {
    }

    public Matrix Ctm { get; private set; } = Matrix.Identity;

    public Matrix TextMatrix { get; private set; } = Matrix.Identity;

    public ReverseFont? Font { get; private set; }

    public double FontSize { get; private set; }

    public double HorizontalScale { get; private set; } = 1.0;

    public double CharSpacing { get; private set; }

    public double WordSpacing { get; private set; }

    public double Rise { get; private set; }

    public static void Walk(byte[] content, IReadOnlyDictionary<string, ReverseFont>? fonts, ShowHandler show, ContentTokenizer.Cache? cache = null)
    {
        var tokens = ContentTokenizer.Tokenize(content, cache);
        var walker = new ContentTextWalker();
        var ctmStack = new Stack<Matrix>();
        var lineMatrix = Matrix.Identity;
        var leading = 0.0;
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

            switch (token.Text)
            {
                case "q":
                    ctmStack.Push(walker.Ctm);
                    break;
                case "Q":
                    if (ctmStack.Count > 0)
                    {
                        walker.Ctm = ctmStack.Pop();
                    }

                    break;
                case "cm":
                    walker.Ctm = Components(operands) * walker.Ctm;
                    break;
                case "BT":
                    walker.TextMatrix = Matrix.Identity;
                    lineMatrix = Matrix.Identity;
                    break;
                case "Tf":
                    walker.Font = LastName(operands) is { } key && fonts is not null && fonts.TryGetValue(key, out var resolved)
                        ? resolved
                        : ReverseFont.WinAnsi;
                    walker.FontSize = LastNumber(operands);
                    break;
                case "Tc":
                    walker.CharSpacing = LastNumber(operands);
                    break;
                case "Tw":
                    walker.WordSpacing = LastNumber(operands);
                    break;
                case "Tz":
                    walker.HorizontalScale = LastNumber(operands) / 100.0;
                    break;
                case "Ts":
                    walker.Rise = LastNumber(operands);
                    break;
                case "TD":
                    leading = -Number(operands, 1);
                    goto case "Td";
                case "Td":
                    lineMatrix = Matrix.Translate(Number(operands, 0), Number(operands, 1)) * lineMatrix;
                    walker.TextMatrix = lineMatrix;
                    break;
                case "TL":
                    leading = Number(operands, 0);
                    break;
                case "Tm":
                    lineMatrix = Components(operands);
                    walker.TextMatrix = lineMatrix;
                    break;
                case "T*":
                    lineMatrix = Matrix.Translate(0, -leading) * lineMatrix;
                    walker.TextMatrix = lineMatrix;
                    break;
                case "Tj":
                case "TJ":
                    walker.Show(show, token.Text, operands, array, operatorIndex++);
                    break;
                case "'":
                    lineMatrix = Matrix.Translate(0, -leading) * lineMatrix;
                    walker.TextMatrix = lineMatrix;
                    walker.Show(show, token.Text, operands, array, operatorIndex++);
                    break;
                // " sets word spacing (aw) and character spacing (ac) from its first two
                // operands, then advances the line and shows.
                case "\"":
                    walker.WordSpacing = Number(operands, 0);
                    walker.CharSpacing = Number(operands, 1);
                    lineMatrix = Matrix.Translate(0, -leading) * lineMatrix;
                    walker.TextMatrix = lineMatrix;
                    walker.Show(show, token.Text, operands, array, operatorIndex++);
                    break;
            }

            operands.Clear();
        }
    }

    private void Show(ShowHandler show, string op, List<Token> operands, List<Token> array, int operatorIndex)
        => TextMatrix = Matrix.Translate(show(this, op, operands, array, operatorIndex), 0) * TextMatrix;
}

using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts;
using static Radzen.Documents.Pdf.Content.ContentOperands;
using Token = Radzen.Documents.Pdf.Content.ContentTokenizer.Token;

namespace Radzen.Documents.Pdf.Content;

internal static class ContentShows
{
    public static bool IsShow(string? op) => op is "Tj" or "TJ" or "'" or "\"";
}

// ISO 32000-1 8.4: the text state is part of the graphics state, saved by q and restored by Q.
internal struct ContentTextState()
{
    public ReverseFont? Font = null;

    public string? FontName = null;

    public double FontSize = 0.0;

    public double Leading = 0.0;

    public double Rise = 0.0;

    public double RenderMode = 0.0;

    public TextSpacing Spacing = new();
}

internal class ContentGraphicsState
{
    public Matrix Ctm = Matrix.Identity;

    public ContentTextState Text = new();

    public ContentGraphicsState Clone() => (ContentGraphicsState)MemberwiseClone();
}

internal sealed class ContentStateMachine(IReadOnlyDictionary<string, ReverseFont>? fonts = null,
    ReverseFont? fallbackFont = null, ContentGraphicsState? state = null)
{
    private readonly Stack<ContentGraphicsState> stack = new();
    private ContentGraphicsState state = state ?? new ContentGraphicsState();

    public ContentGraphicsState State => state;

    public Matrix Ctm => state.Ctm;

    public ref ContentTextState Text => ref state.Text;

    public Matrix TextMatrix { get; private set; } = Matrix.Identity;

    public Matrix LineMatrix { get; private set; } = Matrix.Identity;

    // ISO 32000-1 9.4.1 forbids nesting text objects.
    public int TextObjectDepth { get; private set; }

    public bool Apply(string? op, List<Token> operands)
    {
        if (ContentShows.IsShow(op))
        {
            if (op is "'" or "\"")
            {
                state.Text.Spacing.Apply(op, operands);
                NextLine();
            }

            return false;
        }

        if (state.Text.Spacing.Apply(op, operands))
        {
            return true;
        }

        switch (op)
        {
            case "q":
                stack.Push(state.Clone());
                return true;

            // ISO 32000-1 8.4.4: Q restores the most recently saved state.
            case "Q":
                if (stack.Count > 0)
                {
                    state = stack.Pop();
                }

                return true;

            case "cm":
                state.Ctm = Components(operands) * state.Ctm;
                return true;

            case "BT":
                TextObjectDepth++;
                TextMatrix = LineMatrix = Matrix.Identity;
                return true;

            case "ET":
                if (TextObjectDepth > 0)
                {
                    TextObjectDepth--;
                }

                return true;

            case "Tf":
                state.Text.FontName = LastName(operands);
                state.Text.Font = state.Text.FontName is { } key && fonts is not null && fonts.TryGetValue(key, out var resolved)
                    ? resolved
                    : fallbackFont;
                state.Text.FontSize = LastNumber(operands);
                return true;

            case "TL":
                state.Text.Leading = LastNumber(operands);
                return true;

            case "Ts":
                state.Text.Rise = LastNumber(operands);
                return true;

            case "Tr":
                state.Text.RenderMode = LastNumber(operands);
                return true;

            case "TD":
                state.Text.Leading = -Number(operands, 1);
                goto case "Td";

            case "Td":
                LineMatrix = Matrix.RawTranslate(Number(operands, 0), Number(operands, 1)) * LineMatrix;
                TextMatrix = LineMatrix;
                return true;

            case "Tm":
                LineMatrix = Components(operands);
                TextMatrix = LineMatrix;
                return true;

            case "T*":
                NextLine();
                return true;

            default:
                return false;
        }
    }

    public void Advance(double amount) => TextMatrix = Matrix.RawTranslate(amount, 0) * TextMatrix;

    private void NextLine()
    {
        LineMatrix = Matrix.RawTranslate(0, -state.Text.Leading) * LineMatrix;
        TextMatrix = LineMatrix;
    }
}

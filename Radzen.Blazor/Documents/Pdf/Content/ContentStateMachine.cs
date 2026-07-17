using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts;
using static Radzen.Documents.Pdf.Content.ContentOperands;
using Token = Radzen.Documents.Pdf.Content.ContentTokenizer.Token;

namespace Radzen.Documents.Pdf.Content;

// Which operators paint text, and the index every consumer numbers them by. Redaction
// correlates its edits against the index search produced, so the set and the counting rule
// have to be one thing: a show operator added here reaches all of them at once.
internal static class ContentShows
{
    public static bool IsShow(string? op) => op is "Tj" or "TJ" or "'" or "\"";
}

// The text state, which ISO 32000-1 8.4 makes part of the graphics state: q saves it and Q
// restores it along with the CTM. Tz/Tc/Tw live in the nested TextSpacing because the
// advance formula consumes them together.
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

// The graphics state q/Q saves. Consumers that track more (colors, dash, clip) derive and
// add fields; Clone copies the runtime type, so the stack discipline stays here.
internal class ContentGraphicsState
{
    public Matrix Ctm = Matrix.Identity;

    public ContentTextState Text = new();

    public ContentGraphicsState Clone() => (ContentGraphicsState)MemberwiseClone();
}

// The content-stream state machine: the CTM and its q/Q stack, the text state q/Q saves,
// and the text/line matrices BT scopes. Materialization, walking, editing, replacement and
// redaction all read a stream through this, so they cannot disagree about what an operator
// means. Callers keep their own element/emit concerns and read the state they need.
// fallbackFont is what an unresolvable Tf selects. Materialization passes null, so an edited
// run re-encodes leniently through WinAnsi substitution; search and replacement pass
// ReverseFont.WinAnsi, so a missing glyph width fails loudly instead of silently.
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

    // Applies op's effect on the shared state and reports whether that is all op means.
    // False leaves op to the caller: a show operator (already advanced to its line and
    // carrying its own spacing), or an operator this machine does not model at all.
    public bool Apply(string? op, List<Token> operands)
    {
        if (ContentShows.IsShow(op))
        {
            if (op is "'" or "\"")
            {
                // " sets word/char spacing from its own operands before showing.
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

            // ISO 32000-1 8.4.4: Q restores the most recently saved state. An unbalanced Q
            // has nothing to restore, so the state in effect stands rather than resetting to
            // a default that was never saved.
            case "Q":
                if (stack.Count > 0)
                {
                    state = stack.Pop();
                }

                return true;

            case "cm":
                state.Ctm = Components(operands) * state.Ctm;
                return true;

            // BT/ET only scope the text matrices; ET leaves no state behind.
            case "BT":
                TextMatrix = LineMatrix = Matrix.Identity;
                return true;

            case "ET":
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
                LineMatrix = Matrix.Translate(Number(operands, 0), Number(operands, 1)) * LineMatrix;
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

    // Steps the text matrix by a shown string's horizontal text-space advance.
    public void Advance(double amount) => TextMatrix = Matrix.Translate(amount, 0) * TextMatrix;

    private void NextLine()
    {
        LineMatrix = Matrix.Translate(0, -state.Text.Leading) * LineMatrix;
        TextMatrix = LineMatrix;
    }
}

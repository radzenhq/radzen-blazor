using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf.Fonts;
using static Radzen.Documents.Pdf.Content.ContentOperands;
using Token = Radzen.Documents.Pdf.Content.ContentTokenizer.Token;
using TokenKind = Radzen.Documents.Pdf.Content.ContentTokenizer.TokenKind;

using Radzen.Documents.Pdf.Content;
namespace Radzen.Documents.Pdf;

// Resource-aware text extraction: re-walks a page content stream tracking the text
// and graphics matrices and the active font, reversing each shown char code to
// Unicode through the font's /ToUnicode, /Differences or WinAnsi encoding. Runs are
// emitted in reading order (descending Y, then ascending X).
internal static class TextExtractor
{
    public static string Extract(byte[]? content, IReadOnlyDictionary<string, ReverseFont>? fonts)
    {
        if (content is null || content.Length == 0)
        {
            return string.Empty;
        }

        var fragments = new List<Fragment>();
        ContentTextWalker.Walk(content, fonts, (walker, op, operands, array, _) => op == "TJ"
            ? ShowArray(fragments, array, walker)
            : Show(fragments, operands, walker));
        return Compose(fragments);
    }

    // Emits one fragment and returns the text-space horizontal advance the pen moved,
    // so the caller can step the text matrix and same-line fragments abut correctly.
    private static double Show(List<Fragment> fragments, List<Token> operands, ContentTextWalker walker)
    {
        var bytes = LastString(operands);
        if (bytes is null || bytes.Length == 0)
        {
            return 0.0;
        }

        var text = (walker.Font ?? ReverseFont.WinAnsi).Decode(bytes);
        if (text.Length == 0)
        {
            return 0.0;
        }

        var fontSize = walker.FontSize;
        var advance = text.Length * TextComposition.AverageGlyphEm * fontSize;
        AddFragment(fragments, walker.TextMatrix * walker.Ctm, text, advance, fontSize);
        return advance;
    }

    private static double ShowArray(List<Fragment> fragments, List<Token> array, ContentTextWalker walker)
    {
        var fontSize = walker.FontSize;
        var reverse = walker.Font ?? ReverseFont.WinAnsi;
        var builder = new StringBuilder();
        var glyphEms = 0.0;
        var adjustEms = 0.0;
        foreach (var element in array)
        {
            if (element.Kind == TokenKind.String)
            {
                if (element.Bytes is { Length: > 0 } bytes)
                {
                    var decoded = reverse.Decode(bytes);
                    builder.Append(decoded);
                    glyphEms += decoded.Length * TextComposition.AverageGlyphEm;
                }
            }
            else
            {
                adjustEms += element.Number / 1000.0;
                if (element.Number <= -TextComposition.TjSpaceThreshold)
                {
                    builder.Append(' ');
                }
            }
        }

        if (builder.Length == 0)
        {
            return 0.0;
        }

        var advance = (glyphEms - adjustEms) * fontSize;
        AddFragment(fragments, walker.TextMatrix * walker.Ctm, builder.ToString(), advance, fontSize);
        return advance;
    }

    private static void AddFragment(List<Fragment> fragments, Matrix matrix, string text, double textAdvance, double fontSize)
        => fragments.Add(new Fragment(TextComposition.Place(matrix, textAdvance, fontSize), text));

    private static string Compose(List<Fragment> fragments)
    {
        if (fragments.Count == 0)
        {
            return string.Empty;
        }

        fragments.Sort(static (a, b) => TextComposition.Compare(a.Placement, b.Placement));

        var builder = new StringBuilder();
        Fragment? previous = null;
        foreach (var fragment in fragments)
        {
            if (previous is { } prev
                && TextComposition.Separator(prev.Placement, prev.Text, fragment.Placement, fragment.Text) is { } separator)
            {
                builder.Append(separator);
            }

            builder.Append(fragment.Text);
            previous = fragment;
        }

        return builder.ToString();
    }

    private readonly record struct Fragment(TextComposition.Placement Placement, string Text);
}

using Radzen.Documents.Pdf.Fonts;
using System;
using System.Collections.Generic;

using Radzen.Documents.Pdf.Content;
namespace Radzen.Documents.Pdf;


/// <summary>
/// A run of text drawn at a fixed baseline position using a base-14 font and
/// WinAnsi encoding.
/// </summary>
/// <param name="text">The text to draw.</param>
/// <param name="x">The baseline X position.</param>
/// <param name="y">The baseline Y position.</param>
public sealed class TextContent(string text, Unit x, Unit y) : ContentElement
{
    /// <summary>Gets or sets the text to draw.</summary>
    public string Text { get; set; } = text;

    /// <summary>Gets or sets the font.</summary>
    public Font Font { get; set; } = new();

    /// <summary>Gets or sets the fill color of the text. Defaults to black.</summary>
    public Color Color { get; set; } = Color.Black;

    // Resource name captured when materializing a loaded page; when set, re-emission
    // keeps the original /Font reference instead of registering a base-14 face.
    internal string? FontResourceName { get; set; }

    // Original show-string bytes captured when materializing a loaded page. A Type0/CID
    // run carries 2-byte codes that a WinAnsi re-encode would corrupt, so it is re-emitted
    // verbatim. The plain generate path leaves this null and encodes via WinAnsi.
    internal byte[]? SourceBytes { get; set; }

    // The decoded text as materialized. When the caller has edited Text away from this,
    // SourceBytes no longer describes it and the run is re-encoded through WinAnsi.
    internal string? SourceText { get; set; }

    internal ReverseFont? SourceFont { get; set; }

    // The TJ show array a loaded run carried (interleaved string chunks and numeric
    // displacements). Re-emitted verbatim so kerning/inter-word gaps survive a re-encode;
    // null for an authored run or an edited one, which re-encode through the Tj path.
    internal IReadOnlyList<TextAdjustment>? SourceAdjustments { get; set; }

    // Non-RGB fill (CMYK/gray/named color space) captured when materializing a loaded run.
    // When set it overrides Color so a re-encode preserves the original color space instead
    // of collapsing to the last rg color or black. Null for authored runs (which use Color).
    internal DeviceColor? FillPaint { get; set; }

    // Word spacing (Tw) and character spacing (Tc) captured from a loaded run (the "
    // operator or a preceding Tc/Tw). Zero for authored runs, which keep the defaults.
    internal double WordSpacing { get; set; }

    internal double CharSpacing { get; set; }

    /// <inheritdoc/>
    protected override void EmitBody(ContentWriter writer)
    {
        var key = FontResourceName ?? writer.RegisterFont(Font);

        // A translucent colour paints through a constant-alpha /ExtGState; gs persists in the
        // graphics state, so it is scoped by q..Q to keep the alpha off later elements. A
        // device fill paint replaces Color, and carries no alpha of its own.
        var alpha = FillPaint is null ? Color.A / 255.0 : 1;
        if (alpha < 1)
        {
            writer.WriteRaw("q\n");
            writer.WriteName(writer.RegisterOpacity(alpha));
            writer.WriteRaw(" gs\n");
        }

        writer.WriteRaw("BT\n");
        if (FillPaint is { } fillPaint)
        {
            ContentEmitter.WriteDeviceColor(writer, fillPaint, stroke: false);
        }
        else
        {
            writer.WriteColor(Color, "rg");
        }

        writer.WriteName(key);
        writer.WriteRaw(" ");
        writer.WriteNumber(Font.Size);
        writer.WriteRaw(" Tf\n");
        if (CharSpacing != 0)
        {
            writer.WriteNumber(CharSpacing);
            writer.WriteRaw(" Tc\n");
        }

        if (WordSpacing != 0)
        {
            writer.WriteNumber(WordSpacing);
            writer.WriteRaw(" Tw\n");
        }
        writer.WriteNumber(x.Point);
        writer.WriteRaw(" ");
        writer.WriteNumber(y.Point);
        writer.WriteRaw(" Td\n");

        if (SourceAdjustments is { } segments && Text == SourceText)
        {
            writer.WriteRaw("[");
            foreach (var segment in segments)
            {
                if (segment.Text is not null)
                {
                    writer.WriteString(segment.Text);
                }
                else
                {
                    writer.WriteNumber(segment.Adjustment);
                    writer.WriteRaw(" ");
                }
            }

            writer.WriteRaw("] TJ\n");
        }
        else
        {
            writer.WriteString(EncodeText());
            writer.WriteRaw(" Tj\n");
        }

        writer.WriteRaw("ET\n");
        if (alpha < 1)
        {
            writer.WriteRaw("Q\n");
        }
    }

    private byte[] EncodeText()
    {
        if (SourceBytes is not null && Text == SourceText)
        {
            return SourceBytes;
        }

        if (SourceFont is not null)
        {
            if (SourceFont.TryEncode(Text, out var encoded))
            {
                return encoded;
            }

            throw new NotSupportedException("The source font does not contain every glyph required by the edited text.");
        }

        return Encode(Text);
    }

    // A character outside WinAnsi is drawn as a visible '?' rather than dropped, matching the
    // main text pipeline (EmitBase14Fragment) and honoring the fail-loud invariant.
    private static byte[] Encode(string text)
    {
        WinAnsiEncoding.TryGetCode('?', out var question);
        var bytes = new List<byte>(text.Length);
        foreach (var c in text)
        {
            bytes.Add(WinAnsiEncoding.TryGetCode(c, out var code) ? code : question);
        }

        return [.. bytes];
    }
}

using Radzen.Documents.Pdf.Fonts;
using System;
using System.Collections.Immutable;

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
    private string textValue = text;
    private Font font = new();
    private Color color = Color.Black;
    private string? fontResourceName;
    private ReadOnlyMemory<byte>? sourceBytes;
    private string? sourceText;
    private ReverseFont? sourceFont;
    private ImmutableArray<TextAdjustment>? sourceAdjustments;
    private DeviceColor? fillPaint;
    private double wordSpacing;
    private double charSpacing;
    private bool insideTextObject;

    /// <summary>Gets or sets the text to draw.</summary>
    public string Text
    {
        get => textValue;
        set => Set(ref textValue, value);
    }

    /// <summary>Gets or sets the font.</summary>
    public Font Font
    {
        get => font;
        set => Set(ref font, value);
    }

    /// <summary>Gets or sets the fill color of the text. Defaults to black.</summary>
    public Color Color
    {
        get => color;
        set => Set(ref color, value);
    }

    // A run owns its font, but Font is settable, so one instance can be shared by two runs;
    // asking the font rather than having it push back keeps that from misfiring.
    /// <inheritdoc/>
    public override bool IsModified => base.IsModified || Font.IsModified;

    internal override void AcceptChanges()
    {
        base.AcceptChanges();
        Font.AcceptChanges();
    }

    // Resource name captured when materializing a loaded page; when set, re-emission
    // keeps the original /Font reference instead of registering a base-14 face.
    internal string? FontResourceName
    {
        get => fontResourceName;
        set => Set(ref fontResourceName, value);
    }

    // Original show-string bytes captured when materializing a loaded page. A Type0/CID
    // run carries 2-byte codes that a WinAnsi re-encode would corrupt, so it is re-emitted
    // verbatim. The plain generate path leaves this null and encodes via WinAnsi.
    internal ReadOnlyMemory<byte>? SourceBytes
    {
        get => sourceBytes;
        set => Set(ref sourceBytes, value);
    }

    // The decoded text as materialized. When the caller has edited Text away from this,
    // SourceBytes no longer describes it and the run is re-encoded through WinAnsi.
    internal string? SourceText
    {
        get => sourceText;
        set => Set(ref sourceText, value);
    }

    internal ReverseFont? SourceFont
    {
        get => sourceFont;
        set => Set(ref sourceFont, value);
    }

    // The TJ show array a loaded run carried (interleaved string chunks and numeric
    // displacements). Re-emitted verbatim so kerning/inter-word gaps survive a re-encode;
    // null for an authored run or an edited one, which re-encode through the Tj path.
    internal ImmutableArray<TextAdjustment>? SourceAdjustments
    {
        get => sourceAdjustments;
        set => Set(ref sourceAdjustments, value);
    }

    // Non-RGB fill (CMYK/gray/named color space) captured when materializing a loaded run.
    // When set it overrides Color so a re-encode preserves the original color space instead
    // of collapsing to the last rg color or black. Null for authored runs (which use Color).
    internal DeviceColor? FillPaint
    {
        get => fillPaint;
        set => Set(ref fillPaint, value);
    }

    // Word spacing (Tw) and character spacing (Tc) captured from a loaded run (the "
    // operator or a preceding Tc/Tw). Zero for authored runs, which keep the defaults.
    internal double WordSpacing
    {
        get => wordSpacing;
        set => Set(ref wordSpacing, value);
    }

    internal double CharSpacing
    {
        get => charSpacing;
        set => Set(ref charSpacing, value);
    }

    // Set by the editor when this run splices back inside a source BT..ET that is still open.
    // It moves the emitted bytes, so it tracks like any other member; the editor only ever
    // sets it on a run it has already decided to re-emit, so the door it opens is never the
    // one that decides.
    internal bool InsideTextObject
    {
        get => insideTextObject;
        set => Set(ref insideTextObject, value);
    }

    /// <inheritdoc/>
    protected override void EmitBody(ContentWriter writer)
    {
        var key = FontResourceName ?? writer.RegisterFont(Font);

        // A translucent colour paints through a constant-alpha /ExtGState; gs persists in the
        // graphics state, so it is scoped by q..Q to keep the alpha off later elements. A
        // device fill paint replaces Color, and carries no alpha of its own.
        // Splicing into a live text object also needs the scope: the colour and text state
        // set here would otherwise leak onto the runs that follow inside the same BT..ET,
        // which are copied verbatim and expect the state the source left them.
        var alpha = FillPaint is null ? Color.A / 255.0 : 1;
        var scoped = alpha < 1 || InsideTextObject;
        if (scoped)
        {
            writer.WriteRaw("q\n");
        }

        if (alpha < 1)
        {
            writer.WriteName(writer.RegisterOpacity(alpha));
            writer.WriteRaw(" gs\n");
        }

        var adjustments = SourceAdjustments is { } segments && Text == SourceText ? segments : (ImmutableArray<TextAdjustment>?)null;
        ContentEmitter.WriteTextShow(writer, new TextShowOp
        {
            FontKey = key,
            Size = Font.Size,
            X = x.Point,
            Baseline = y.Point,
            Color = Color,
            FillPaint = FillPaint,
            CharSpacing = CharSpacing,
            WordSpacing = WordSpacing,
            Bytes = adjustments is null ? EncodeText() : default,
            Adjustments = adjustments,
            InsideTextObject = InsideTextObject,
        });

        if (scoped)
        {
            writer.WriteRaw("Q\n");
        }
    }

    private ReadOnlyMemory<byte> EncodeText()
    {
        if (SourceBytes is { } source && Text == SourceText)
        {
            return source;
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
    private static byte[] Encode(string text) => WinAnsiText.Encode(text, OnUnencodable.Substitute);
}

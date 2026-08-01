using Radzen.Documents.Pdf.Fonts;
using System;
using System.Collections.Immutable;

using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Fonts;
using Radzen.Documents.Core;
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

    /// <inheritdoc/>
    public override bool IsModified => base.IsModified || Font.IsModified;

    internal override void AcceptChanges()
    {
        base.AcceptChanges();
        Font.AcceptChanges();
    }

    internal override void OwnedBy(System.Action? changed)
    {
        base.OwnedBy(changed);
        Font.OwnedBy(changed);
    }

    internal override ContentElement DeepClone()
    {
        var clone = CopyStateTo(new TextContent(Text, x, y)
        {
            Font = ContentClone.CopyFont(Font),
            Color = Color,
        });
        clone.FontResourceName = FontResourceName;
        clone.SourceBytes = SourceBytes is { } bytes ? new ReadOnlyMemory<byte>(bytes.ToArray()) : null;
        clone.SourceText = SourceText;
        clone.SourceFont = SourceFont;
        clone.SourceAdjustments = CopyAdjustments(SourceAdjustments);
        clone.FillPaint = ContentClone.CopyDeviceColor(FillPaint);
        clone.WordSpacing = WordSpacing;
        clone.CharSpacing = CharSpacing;
        clone.InsideTextObject = InsideTextObject;
        return clone;
    }

    private static ImmutableArray<TextAdjustment>? CopyAdjustments(ImmutableArray<TextAdjustment>? source)
    {
        if (source is not { } adjustments)
        {
            return null;
        }

        var result = ImmutableArray.CreateBuilder<TextAdjustment>(adjustments.Length);
        foreach (var adjustment in adjustments)
        {
            result.Add(new TextAdjustment(adjustment.Text is { } bytes ? [.. bytes] : null, adjustment.Adjustment));
        }

        return result.MoveToImmutable();
    }

    internal string? FontResourceName
    {
        get => fontResourceName;
        set => Set(ref fontResourceName, value);
    }

    internal ReadOnlyMemory<byte>? SourceBytes
    {
        get => sourceBytes;
        set => Set(ref sourceBytes, value);
    }

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

    internal ImmutableArray<TextAdjustment>? SourceAdjustments
    {
        get => sourceAdjustments;
        set => Set(ref sourceAdjustments, value);
    }

    internal DeviceColor? FillPaint
    {
        get => fillPaint;
        set => Set(ref fillPaint, value);
    }

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

    internal bool InsideTextObject
    {
        get => insideTextObject;
        set => Set(ref insideTextObject, value);
    }

    /// <inheritdoc/>
    protected override void EmitBody(ContentWriter writer)
    {
        var key = FontResourceName ?? writer.RegisterFont(Font);

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
            Size = Font.EffectiveSize.Point,
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

    private static byte[] Encode(string text) => WinAnsiText.Encode(text, OnUnencodable.Substitute);
}

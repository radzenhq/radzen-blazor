namespace Radzen.Documents.Fonts;


/// <summary>
/// A text font: family name, size in points and style attributes.
/// </summary>
public sealed class Font : ITracksChanges
{
    private string? family;
    private Unit? size;
    private bool? bold;
    private bool? italic;
    private bool? underline;
    private bool? strikethrough;
    private Color? color;
    private ChangeTracker tracker;

    /// <summary>Gets or sets the font family name set directly on this font, or <see langword="null"/> when
    /// none is set and the family is inherited. Setting <see langword="null"/> resets it and is tracked as a change.</summary>
    public string? Family
    {
        get => family;
        set => tracker.Set(ref family, value);
    }

    /// <summary>Gets or sets the font size set directly on this font, or <see langword="null"/> when none is
    /// set and the size is inherited. Setting <see langword="null"/> resets it and is tracked as a change.</summary>
    public Unit? Size
    {
        get => size;
        set => tracker.Set(ref size, value);
    }

    /// <summary>Gets or sets the bold flag set directly on this font, or <see langword="null"/> when none is
    /// set and the flag is inherited. Setting <see langword="null"/> resets it and is tracked as a change.</summary>
    public bool? Bold
    {
        get => bold;
        set => tracker.Set(ref bold, value);
    }

    /// <summary>Gets or sets the italic flag set directly on this font, or <see langword="null"/> when none
    /// is set and the flag is inherited. Setting <see langword="null"/> resets it and is tracked as a change.</summary>
    public bool? Italic
    {
        get => italic;
        set => tracker.Set(ref italic, value);
    }

    /// <summary>Gets or sets the underline flag set directly on this font, or <see langword="null"/> when
    /// none is set and the flag is inherited. Setting <see langword="null"/> resets it and is tracked as a change.</summary>
    public bool? Underline
    {
        get => underline;
        set => tracker.Set(ref underline, value);
    }

    /// <summary>Gets or sets the strikethrough flag set directly on this font, or <see langword="null"/>
    /// when none is set and the flag is inherited. Setting <see langword="null"/> resets it and is tracked as a change.</summary>
    public bool? Strikethrough
    {
        get => strikethrough;
        set => tracker.Set(ref strikethrough, value);
    }

    /// <summary>Gets or sets the text color set directly on this font, or <see langword="null"/> when none
    /// is set and the color is inherited. Setting <see langword="null"/> resets it and is tracked as a change.</summary>
    public Color? Color
    {
        get => color;
        set => tracker.Set(ref color, value);
    }

    internal const string DefaultFamily = "Helvetica";

    internal static Unit DefaultSize => Unit.FromPoint(10);

    internal string EffectiveFamily => family ?? DefaultFamily;

    internal Unit EffectiveSize => size ?? DefaultSize;

    internal bool EffectiveBold => bold ?? false;

    internal bool EffectiveItalic => italic ?? false;

    internal bool EffectiveUnderline => underline ?? false;

    internal bool EffectiveStrikethrough => strikethrough ?? false;

    internal Color EffectiveColor => color ?? Radzen.Documents.Color.Black;

    internal FontValues Effective() => new()
    {
        Family = EffectiveFamily,
        Size = EffectiveSize,
        Bold = EffectiveBold,
        Italic = EffectiveItalic,
        Underline = EffectiveUnderline,
        Strikethrough = EffectiveStrikethrough,
        Color = EffectiveColor,
    };

    internal bool IsModified => tracker.IsModified;

    internal void AcceptChanges() => tracker.AcceptChanges();

    bool ITracksChanges.IsModified => IsModified;

    void ITracksChanges.AcceptChanges() => AcceptChanges();

    internal void InheritFrom(Font parent)
    {
        family ??= parent.family;
        size ??= parent.size;
        bold ??= parent.bold;
        italic ??= parent.italic;
        underline ??= parent.underline;
        strikethrough ??= parent.strikethrough;
        color ??= parent.color;
    }
}

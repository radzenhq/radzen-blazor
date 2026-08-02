using Radzen.Documents.Core;

namespace Radzen.Documents.Pdf;

/// <summary>
/// Describes an interactive annotation placed on a PDF page. The hierarchy is closed: the set of
/// annotation kinds is fixed by this library and cannot be extended from outside it.
/// </summary>
public abstract class Annotation : ITracksChanges
{
    private protected Annotation(PdfRect bounds) => this.bounds = bounds;

    private ChangeTracker tracker;
    private PdfRect bounds;
    private Color color = Color.Yellow;
    private double opacity = 1;
    private AnnotationFlags flags = AnnotationFlags.Print;
    private string? contents;
    private string? title;
    private AnnotationAppearance? appearance;

    /// <summary>Gets or sets the annotation bounds in PDF page coordinates.</summary>
    public PdfRect Bounds
    {
        get => bounds;
        set => Set(ref bounds, value);
    }

    /// <summary>Gets or sets the annotation color.</summary>
    public Color Color
    {
        get => color;
        set => Set(ref color, value);
    }

    /// <summary>Gets or sets the annotation opacity from 0 to 1.</summary>
    public double Opacity
    {
        get => opacity;
        set => Set(ref opacity, value);
    }

    /// <summary>Gets or sets the annotation flags.</summary>
    public AnnotationFlags Flags
    {
        get => flags;
        set => Set(ref flags, value);
    }

    /// <summary>Gets or sets the annotation text.</summary>
    public string? Contents
    {
        get => contents;
        set => Set(ref contents, value);
    }

    /// <summary>Gets or sets the annotation title or author.</summary>
    public string? Title
    {
        get => title;
        set => Set(ref title, value);
    }

    /// <summary>Gets or sets a custom appearance.</summary>
    public AnnotationAppearance? Appearance
    {
        get => appearance;
        set => Set(ref appearance, value);
    }

    internal virtual bool IsModified => tracker.IsModified || Appearance?.IsModified == true;

    internal abstract string Subtype { get; }

    private protected void Set<T>(ref T field, T value) => tracker.Set(ref field, value);

    private protected void Touch() => tracker.Touch();

    internal virtual void AcceptChanges()
    {
        tracker.AcceptChanges();
        Appearance?.AcceptChanges();
    }

    bool ITracksChanges.IsModified => IsModified;

    void ITracksChanges.AcceptChanges() => AcceptChanges();

    internal virtual void OwnedBy(System.Action? changed)
    {
        tracker.OwnedBy(changed);
        Appearance?.OwnedBy(changed);
    }

    void ITracksChanges.OwnedBy(System.Action? changed) => OwnedBy(changed);
}

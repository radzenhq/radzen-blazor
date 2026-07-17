namespace Radzen.Documents.Pdf;

/// <summary>Describes an interactive annotation placed on a PDF page.</summary>
/// <param name="bounds">The annotation bounds in PDF page coordinates.</param>
public abstract class Annotation(PdfRect bounds) : ITracksChanges
{
    private ChangeTracker tracker;
    private PdfRect bounds = bounds;
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

    /// <summary>
    /// Gets a value indicating whether this annotation has been modified since it was loaded.
    /// A loaded page re-emits only the annotations that report true, so an untouched one keeps
    /// its original dictionary.
    /// </summary>
    public virtual bool IsModified => tracker.IsModified || Appearance?.IsModified == true;

    internal abstract string Subtype { get; }

    /// <summary>Assigns a tracked backing field and marks this annotation modified.</summary>
    /// <typeparam name="T">The field type.</typeparam>
    /// <param name="field">The backing field to assign.</param>
    /// <param name="value">The value to assign.</param>
    protected void Set<T>(ref T field, T value) => tracker.Set(ref field, value);

    /// <summary>Marks this annotation modified without assigning a tracked field.</summary>
    protected void Touch() => tracker.Touch();

    // Called once over a loaded page's annotations after reading, which builds them through
    // these same setters and would otherwise leave every loaded annotation born dirty.
    internal virtual void AcceptChanges()
    {
        tracker.AcceptChanges();
        Appearance?.AcceptChanges();
    }

    // Explicit so the overridable public IsModified/AcceptChanges do not themselves implement an
    // internal interface member (CA2119); subclass overrides are still reached through them.
    bool ITracksChanges.IsModified => IsModified;

    void ITracksChanges.AcceptChanges() => AcceptChanges();
}

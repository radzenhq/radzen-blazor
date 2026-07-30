using Radzen.Documents.Pdf.Content;
namespace Radzen.Documents.Pdf;


/// <summary>
/// The base class for a single drawable element in a page content stream.
/// Elements are painted in the order they are added to <see cref="Page.Content"/>.
/// </summary>
public abstract class ContentElement : ITracksChanges
{
    internal ContentElement()
    {
    }

    private ChangeTracker tracker;
    private Matrix transform = Matrix.Identity;
    private bool isArtifact;

    /// <summary>
    /// Gets or sets the transform applied to this element. Defaults to
    /// <see cref="Matrix.Identity"/>, in which case no transform is emitted.
    /// </summary>
    public Matrix Transform
    {
        get => transform;
        set => Set(ref transform, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this element is an artifact
    /// (decorative, non-content). Artifacts are wrapped in an
    /// <c>/Artifact BDC ... EMC</c> marked-content sequence.
    /// </summary>
    public bool IsArtifact
    {
        get => isArtifact;
        set => Set(ref isArtifact, value);
    }

    /// <summary>
    /// Gets a value indicating whether this element has been modified since it was
    /// materialized from a loaded content stream. A loaded page re-emits only the
    /// elements that report true, so an untouched one keeps its original bytes.
    /// </summary>
    public virtual bool IsModified => tracker.IsModified;

    /// <summary>Assigns a tracked backing field and marks this element modified.</summary>
    /// <typeparam name="T">The field type.</typeparam>
    /// <param name="field">The backing field to assign.</param>
    /// <param name="value">The value to assign.</param>
    protected void Set<T>(ref T field, T value) => tracker.Set(ref field, value);

    /// <summary>Marks this element modified without assigning a tracked field.</summary>
    protected void Touch() => tracker.Touch();

    internal virtual void AcceptChanges() => tracker.AcceptChanges();

    internal virtual ContentElement DeepClone() => (ContentElement)MemberwiseClone();

    internal T CopyStateTo<T>(T target) where T : ContentElement
    {
        target.Transform = Transform;
        target.IsArtifact = IsArtifact;
        return target;
    }

    bool ITracksChanges.IsModified => IsModified;

    void ITracksChanges.AcceptChanges() => AcceptChanges();

    internal virtual void OwnedBy(System.Action? changed) => tracker.OwnedBy(changed);

    void ITracksChanges.OwnedBy(System.Action? changed) => OwnedBy(changed);

    internal void Emit(ContentWriter writer) => Emit(writer, Transform);

    internal void Emit(ContentWriter writer, Matrix transform)
    {
        if (IsArtifact)
        {
            writer.WriteName("Artifact");
            writer.WriteRaw(" BMC\n");
        }

        var transformed = transform != Matrix.Identity;
        if (transformed)
        {
            writer.WriteRaw("q\n");
            ContentEmitter.WriteTransform(writer, transform);
        }

        EmitBody(writer);

        if (transformed)
        {
            writer.WriteRaw("Q\n");
        }

        if (IsArtifact)
        {
            writer.WriteRaw("EMC\n");
        }
    }

    /// <summary>
    /// Emits this element's content-stream body into <paramref name="writer"/>. The transform
    /// and artifact/marked-content wrapping are applied by the base class around this call, so an
    /// override writes only the element's own operators.
    /// </summary>
    /// <param name="writer">The content-stream writer to emit into.</param>
    protected abstract void EmitBody(ContentWriter writer);
}

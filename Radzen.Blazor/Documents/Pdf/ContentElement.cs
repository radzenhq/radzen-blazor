using Radzen.Documents.Pdf.Content;
namespace Radzen.Documents.Pdf;


/// <summary>
/// The base class for a single drawable element in a page content stream.
/// Elements are painted in the order they are added to <see cref="Page.Content"/>.
/// </summary>
public abstract class ContentElement
{
    private bool touched;
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
    public virtual bool IsModified => touched;

    /// <summary>Assigns a tracked backing field and marks this element modified.</summary>
    /// <typeparam name="T">The field type.</typeparam>
    /// <param name="field">The backing field to assign.</param>
    /// <param name="value">The value to assign.</param>
    // Unconditional: whether the new value is "equal" to the old is not the same question as
    // whether it emits the same bytes, and for a mutable Font or a float it is not even stable.
    protected void Set<T>(ref T field, T value)
    {
        field = value;
        touched = true;
    }

    /// <summary>Marks this element modified without assigning a tracked field.</summary>
    protected void Touch() => touched = true;

    // Called once over a loaded page's original elements after materialization, which builds
    // them through these same setters and would otherwise leave every page born dirty.
    internal virtual void AcceptChanges() => touched = false;

    internal void Emit(ContentWriter writer) => Emit(writer, Transform);

    // A re-emitted element is spliced back where the source graphics state is still in
    // effect, so the caller passes the transform to emit relative to that ambient state
    // instead of this element's absolute one.
    internal void Emit(ContentWriter writer, Matrix transform)
    {
        if (IsArtifact)
        {
            writer.WriteName("Artifact");
            writer.WriteRaw(" BDC\n");
        }

        var transformed = transform != Matrix.Identity;
        if (transformed)
        {
            writer.WriteRaw("q\n");
            writer.WriteNumber(transform.A);
            writer.WriteRaw(" ");
            writer.WriteNumber(transform.B);
            writer.WriteRaw(" ");
            writer.WriteNumber(transform.C);
            writer.WriteRaw(" ");
            writer.WriteNumber(transform.D);
            writer.WriteRaw(" ");
            writer.WriteNumber(transform.E);
            writer.WriteRaw(" ");
            writer.WriteNumber(transform.F);
            writer.WriteRaw(" cm\n");
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

namespace Radzen.Documents.Pdf;


/// <summary>
/// The base class for a single drawable element in a page content stream.
/// Elements are painted in the order they are added to <see cref="Page.Content"/>.
/// </summary>
public abstract class ContentElement
{
    /// <summary>
    /// Gets or sets the transform applied to this element. Defaults to
    /// <see cref="Matrix.Identity"/>, in which case no transform is emitted.
    /// </summary>
    public Matrix Transform { get; set; } = Matrix.Identity;

    /// <summary>
    /// Gets or sets a value indicating whether this element is an artifact
    /// (decorative, non-content). Artifacts are wrapped in an
    /// <c>/Artifact BDC ... EMC</c> marked-content sequence.
    /// </summary>
    public bool IsArtifact { get; set; }

    /// <summary>Gets or sets the optional structure tag for this element.</summary>
    public Tag? Tag { get; set; }

    internal void Emit(ContentWriter writer)
    {
        if (IsArtifact)
        {
            writer.WriteName("Artifact");
            writer.WriteRaw(" BDC\n");
        }

        var transformed = Transform != Matrix.Identity;
        if (transformed)
        {
            writer.WriteRaw("q\n");
            writer.WriteNumber(Transform.A);
            writer.WriteRaw(" ");
            writer.WriteNumber(Transform.B);
            writer.WriteRaw(" ");
            writer.WriteNumber(Transform.C);
            writer.WriteRaw(" ");
            writer.WriteNumber(Transform.D);
            writer.WriteRaw(" ");
            writer.WriteNumber(Transform.E);
            writer.WriteRaw(" ");
            writer.WriteNumber(Transform.F);
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

    internal abstract void EmitBody(ContentWriter writer);
}

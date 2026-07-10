namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// A single physical page in a <see cref="Document"/>. Holds the page
/// dimensions and, optionally, a raw content stream.
/// </summary>
public sealed class Page
{
    private byte[]? content;

    internal Page(Unit width, Unit height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>Gets the page width in points.</summary>
    public Unit Width { get; }

    /// <summary>Gets the page height in points.</summary>
    public Unit Height { get; }

    /// <summary>
    /// Sets the raw content stream for this page. The bytes are stored verbatim
    /// and written without a compression filter.
    /// </summary>
    /// <param name="value">The raw content stream bytes.</param>
    public void SetContent(byte[] value)
    {
        System.ArgumentNullException.ThrowIfNull(value);
        content = value;
    }

    /// <summary>
    /// Gets the raw content stream previously set with <see cref="SetContent"/>,
    /// or <c>null</c> when no content has been set.
    /// </summary>
    /// <returns>The raw content bytes, or <c>null</c>.</returns>
    public byte[]? GetContent() => content;
}

namespace Radzen.Documents.Pdf;


/// <summary>
/// A single physical page in a <see cref="Document"/>. Holds the page
/// dimensions and, optionally, a raw content stream.
/// </summary>
public sealed class Page
{
    private readonly ContentCollection elements = [];
    private byte[]? content;
    private bool materialized;
    private int materializedCount;
    private byte[]? snapshot;
    private System.Collections.Generic.IReadOnlyDictionary<string, Fonts.ReverseFont>? textFonts;

    internal Page(Unit width, Unit height)
    {
        Width = width;
        Height = height;
    }

    // Pre-generated content and resources produced by DocumentBuilder.Build; when set,
    // the document writer emits these bytes and resources directly (see Document.SaveToStream).
    internal GeneratedPage? Generated { get; set; }

    /// <summary>Gets the page width in points.</summary>
    public Unit Width { get; }

    /// <summary>Gets the page height in points.</summary>
    public Unit Height { get; }

    /// <summary>
    /// Gets the ordered collection of content elements. For a loaded page the raw
    /// content stream is parsed into elements on first access; an untouched page
    /// still re-serializes byte-for-byte from its retained raw bytes.
    /// </summary>
    public ContentCollection Content
    {
        get
        {
            EnsureMaterialized();
            return elements;
        }
    }

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

    /// <summary>
    /// Extracts the visible text of this page in reading order: top to bottom, then
    /// left to right. Char codes are reversed to Unicode through each font's
    /// <c>/ToUnicode</c> CMap, <c>/Differences</c> array or standard WinAnsi encoding.
    /// </summary>
    /// <returns>The page text, or an empty string when the page has no text.</returns>
    public string ExtractText() => TextExtractor.Extract(content, textFonts);

    internal void SetTextFonts(System.Collections.Generic.IReadOnlyDictionary<string, Fonts.ReverseFont> fonts)
    {
        textFonts = fonts;
    }

    // Resolves the content-stream bytes to write. An untouched loaded page reuses its
    // retained raw bytes. A loaded page whose original elements are intact but that
    // gained new elements keeps its raw bytes untouched and returns the additions as a
    // separate overlay stream. Any other modification (or a freshly authored page)
    // re-encodes from elements; the emitters carry the resources each stream needs.
    internal byte[]? BuildContent(out ContentWriter? emitter, out byte[]? overlay, out ContentWriter? overlayEmitter)
    {
        emitter = null;
        overlay = null;
        overlayEmitter = null;
        if (elements.Count == 0)
        {
            return content;
        }

        if (content is not null && snapshot is not null && elements.Count >= materializedCount
            && OriginalElementsIntact())
        {
            if (elements.Count == materializedCount)
            {
                return content;
            }

            var appended = new ContentWriter("SF", "SIm");
            for (var i = materializedCount; i < elements.Count; i++)
            {
                elements[i].Emit(appended);
            }

            overlay = appended.ToArray();
            appended.Dispose();
            overlayEmitter = appended;
            return content;
        }

        var writer = new ContentWriter();
        foreach (var element in elements)
        {
            element.Emit(writer);
        }

        var bytes = writer.ToArray();
        writer.Dispose();
        emitter = writer;
        return bytes;
    }

    private bool OriginalElementsIntact()
    {
        using var writer = new ContentWriter();
        for (var i = 0; i < materializedCount; i++)
        {
            elements[i].Emit(writer);
        }

        return snapshot is not null && Same(writer.ToArray(), snapshot);
    }

    // A built page keeps the generator's bytes as its base; Content holds only the
    // user's additions, emitted here as a second content stream appended on save.
    internal byte[]? BuildOverlay(out ContentWriter? emitter)
    {
        emitter = null;
        if (elements.Count == 0)
        {
            return null;
        }

        var writer = new ContentWriter("SF", "SIm");
        foreach (var element in elements)
        {
            element.Emit(writer);
        }

        var bytes = writer.ToArray();
        writer.Dispose();
        emitter = writer;
        return bytes;
    }

    private void EnsureMaterialized()
    {
        if (materialized)
        {
            return;
        }

        materialized = true;
        if (content is null || Generated is not null)
        {
            return;
        }

        ContentInterpreter.Materialize(content, elements);
        materializedCount = elements.Count;

        using var writer = new ContentWriter();
        foreach (var element in elements)
        {
            element.Emit(writer);
        }

        snapshot = writer.ToArray();
    }

    private static bool Same(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        for (var i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }

        return true;
    }
}

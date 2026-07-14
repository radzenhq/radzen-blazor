using System;
using System.Collections.Generic;

using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Emit;
namespace Radzen.Documents.Pdf;


/// <summary>
/// A single physical page in a <see cref="Document"/>. Holds the page
/// dimensions and, optionally, a raw content stream.
/// </summary>
public sealed class Page
{
    private readonly ContentCollection elements = [];
    private readonly AnnotationCollection annotations = [];
    private byte[]? content;
    private bool materialized;
    private int materializedCount;
    private byte[]? snapshot;
    private int rotate;
    private Unit width;
    private Unit height;
    private Rect mediaBox;
    private Rect? cropBox;
    private IReadOnlyDictionary<string, Fonts.ReverseFont>? textFonts;

    internal Page(Unit width, Unit height)
    {
        this.width = width;
        this.height = height;
        mediaBox = new Rect(0, 0, width.Point, height.Point);
    }

    // Pre-generated content and resources produced by DocumentBuilder.Build; when set,
    // the document writer emits these bytes and resources directly (see Document.SaveToStream).
    internal GeneratedPage? Generated { get; set; }

    /// <summary>Gets the page width in points.</summary>
    public Unit Width => width;

    /// <summary>Gets the page height in points.</summary>
    public Unit Height => height;

    /// <summary>
    /// Gets or sets the media box (<c>/MediaBox</c>) in PDF user-space coordinates.
    /// Changing the box changes the page dimensions but does not scale, translate or
    /// otherwise modify existing content coordinates.
    /// </summary>
    public Rect MediaBox
    {
        get => mediaBox;
        set
        {
            ValidateBox(value, nameof(value));
            SetMediaBox(value, true);
        }
    }

    /// <summary>
    /// Gets or sets the crop box (<c>/CropBox</c>) in PDF user-space coordinates.
    /// Existing content keeps its original coordinates. Set to <see langword="null"/>
    /// to remove an explicit or preserved crop box.
    /// </summary>
    public Rect? CropBox
    {
        get => cropBox;
        set
        {
            if (value is { } box)
            {
                ValidateBox(box, nameof(value));
            }

            cropBox = value;
            CropBoxSet = true;
        }
    }

    internal bool MediaBoxSet { get; private set; }

    internal bool CropBoxSet { get; private set; }

    internal void SetPreservedMediaBox(Rect value) => SetMediaBox(value, false);

    internal void SetPreservedCropBox(Rect value) => cropBox = value;

    private void SetMediaBox(Rect value, bool explicitlySet)
    {
        mediaBox = value;
        width = Unit.FromPoint(value.Width);
        height = Unit.FromPoint(value.Height);
        MediaBoxSet = explicitlySet;
    }

    private static void ValidateBox(Rect value, string parameterName)
    {
        if (!double.IsFinite(value.X) || !double.IsFinite(value.Y)
            || !double.IsFinite(value.Width) || !double.IsFinite(value.Height)
            || value.Width <= 0 || value.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Page boxes must have finite coordinates and positive dimensions.");
        }
    }

    /// <summary>
    /// Gets or sets the bleed box (<c>/BleedBox</c>): the region to which page contents
    /// are clipped when output in a production environment. The rectangle is in PDF user
    /// space, where <c>X, Y</c> is the lower-left corner and the box spans to
    /// <c>X + Width, Y + Height</c>. When <see langword="null"/> no bleed box is written
    /// unless one was preserved from a loaded page.
    /// </summary>
    public Rect? BleedBox { get; set; }

    /// <summary>
    /// Gets or sets the trim box (<c>/TrimBox</c>): the intended finished dimensions of
    /// the page after trimming. Coordinates follow the same convention as
    /// <see cref="BleedBox"/>. When <see langword="null"/> no trim box is written unless
    /// one was preserved from a loaded page.
    /// </summary>
    public Rect? TrimBox { get; set; }

    /// <summary>
    /// Gets or sets the art box (<c>/ArtBox</c>): the extent of the page's meaningful
    /// content as intended by its creator. Coordinates follow the same convention as
    /// <see cref="BleedBox"/>. When <see langword="null"/> no art box is written unless
    /// one was preserved from a loaded page.
    /// </summary>
    public Rect? ArtBox { get; set; }

    /// <summary>
    /// Gets or sets the clockwise viewing rotation of the page in degrees.
    /// Must be 0, 90, 180 or 270; the default 0 emits no <c>/Rotate</c> key.
    /// </summary>
    public int Rotate
    {
        get => rotate;
        set
        {
            if (value is not (0 or 90 or 180 or 270))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Page rotation must be 0, 90, 180 or 270 degrees.");
            }

            rotate = value;
        }
    }

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

    /// <summary>Gets the ordered collection of interactive annotations on this page.</summary>
    public AnnotationCollection Annotations => annotations;

    /// <summary>
    /// Sets the raw content stream for this page. The bytes are stored verbatim
    /// and written without a compression filter.
    /// </summary>
    /// <param name="value">The raw content stream bytes.</param>
    public void SetContent(byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value);
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

    /// <summary>
    /// Extracts decoded text-show runs in reading order with transformed em-box
    /// geometry and source operator ordinals.
    /// </summary>
    /// <remarks>
    /// Geometry is an estimated em box because PDF content streams do not always
    /// expose glyph outlines or shaping clusters. Form XObject text is not included.
    /// </remarks>
    /// <returns>The positioned text-show runs, or an empty list when the page has no text.</returns>
    public IReadOnlyList<PositionedTextRun> ExtractPositionedText() => TextSearch.Extract(content, textFonts);

    /// <summary>Finds text in this page across adjacent text-show operators.</summary>
    /// <remarks>
    /// Hits use a page index of -1 when this method is called directly. Use
    /// <see cref="Document.FindText(string, TextSearchOptions?)"/> to obtain document page indexes.
    /// Form XObject text and complex shaping or ligature cluster mapping are not included.
    /// </remarks>
    /// <param name="text">The non-empty text to find.</param>
    /// <param name="options">The matching options, or <c>null</c> for defaults.</param>
    /// <returns>The matches in reading order.</returns>
    public IReadOnlyList<TextHit> FindText(string text, TextSearchOptions? options = null)
        => TextSearch.Find(content, textFonts, text, options, -1);

    internal IReadOnlyList<TextHit> FindText(string text, TextSearchOptions? options, int pageIndex)
        => TextSearch.Find(content, textFonts, text, options, pageIndex);

    internal void SetTextFonts(IReadOnlyDictionary<string, Fonts.ReverseFont> fonts)
    {
        textFonts = fonts;
    }

    // The reverse char-code -> Unicode maps used by ExtractText, exposed so Document.Append
    // can carry them onto a copied page (a Type0/Identity-H stream is not reversible without them).
    internal IReadOnlyDictionary<string, Fonts.ReverseFont>? TextFonts => textFonts;

    // Resolves the content-stream bytes to write. An untouched loaded page reuses its
    // retained raw bytes. A loaded page whose original elements are intact but that
    // gained new elements keeps its raw bytes untouched and returns the additions as a
    // separate overlay stream. Any other modification (or a freshly authored page)
    // re-encodes from elements; the emitters carry the resources each stream needs.
    internal ContentEmissionResult BuildContent(IReadOnlyCollection<string>? reservedNames = null)
    {
        if (elements.Count == 0)
        {
            return new ContentEmissionResult(content, ContentResourceManifest.Empty);
        }

        if (content is not null && snapshot is not null && elements.Count >= materializedCount
            && OriginalElementsIntact())
        {
            if (elements.Count == materializedCount)
            {
                return new ContentEmissionResult(content, ContentResourceManifest.Empty);
            }

            using var appended = new ContentWriter(
                SafePrefix("SF", reservedNames),
                SafePrefix("SIm", reservedNames),
                SafePrefix("SGS", reservedNames));
            for (var i = materializedCount; i < elements.Count; i++)
            {
                elements[i].Emit(appended);
            }

            return new ContentEmissionResult(content, ContentResourceManifest.Empty, appended.DetachResult());
        }

        // A full re-emit registers fresh base-14 fonts and image XObjects; its keys must
        // dodge the loaded page's resource names so MergeResources cannot overwrite them.
        using var writer = new ContentWriter(
            SafePrefix("F", reservedNames),
            SafePrefix("Im", reservedNames),
            SafePrefix("GS", reservedNames));
        foreach (var element in elements)
        {
            element.Emit(writer);
        }

        return writer.DetachResult();
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
    internal ContentEmissionResult? BuildOverlay()
    {
        if (elements.Count == 0)
        {
            return null;
        }

        using var writer = new ContentWriter("SF", "SIm", "SGS");
        foreach (var element in elements)
        {
            element.Emit(writer);
        }

        return writer.DetachResult();
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

        ContentInterpreter.Materialize(content, elements, textFonts);
        materializedCount = elements.Count;

        using var writer = new ContentWriter();
        foreach (var element in elements)
        {
            element.Emit(writer);
        }

        snapshot = writer.ToArray();
    }

    // Emitter keys are prefix+index; a prefix that no reserved name begins with can never
    // equal one, so extend it with a non-digit until it is disjoint from every loaded name.
    private static string SafePrefix(string baseName, IReadOnlyCollection<string>? reserved)
    {
        if (reserved is null || reserved.Count == 0)
        {
            return baseName;
        }

        var prefix = baseName;
        while (StartsWithAny(reserved, prefix))
        {
            prefix += "z";
        }

        return prefix;
    }

    private static bool StartsWithAny(IReadOnlyCollection<string> names, string prefix)
    {
        foreach (var name in names)
        {
            if (name.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
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

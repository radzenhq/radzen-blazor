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
    private readonly List<ContentElement> pendingAppends = [];
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
    private IReadOnlyList<ContentEditor.SourceElement>? sourceElements;
    private ContentResourceManifest editedResources = ContentResourceManifest.Empty;
    private IReadOnlyCollection<string>? reservedResourceNames;

    internal Page(Unit width, Unit height)
    {
        this.width = width;
        this.height = height;
        mediaBox = new Rect(0, 0, width.Point, height.Point);
    }

    // Pre-generated content and resources produced by DocumentBuilder.Build; when set,
    // the document writer emits these bytes and resources directly (see Document.SaveToStream).
    internal GeneratedPage? Generated { get; set; }

    // The document this page was first added to, and whose LoadedState therefore holds the
    // page's source-derived entries. Set once by PageCollection and never reassigned, so a
    // page inserted into further documents still carries from the document that owns its state.
    internal Document? Owner { get; set; }

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
    /// A page loaded from a source reports that page's rotation, and setting 0
    /// removes it.
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

            // An explicit rotation supersedes the source page's, which the saver would
            // otherwise re-emit whenever this value is 0.
            Owner?.Loaded?.SourceRotations.Remove(this);
        }
    }

    // A source /Rotate is any multiple of 90 (including negative and over-360 values) and
    // is normalized to the 0-359 range the public setter accepts; a non-conforming value
    // is kept verbatim so it still round-trips.
    internal void SetLoadedRotate(int degrees)
        => rotate = degrees % 90 == 0 ? ((degrees % 360) + 360) % 360 : degrees;

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
    /// and written without a compression filter. They replace the page's content
    /// entirely, so <see cref="Content"/> discards any elements parsed from the
    /// previous bytes and re-parses these on next access.
    /// </summary>
    /// <param name="value">The raw content stream bytes.</param>
    public void SetContent(byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value);
        content = value;
        ResetMaterialization();
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

    /// <summary>Replaces every matching text occurrence using the source font encoding.</summary>
    /// <remarks>Matches may span contiguous <c>Tj</c> operators with the same font and text state. Unsupported show operators or incompatible text states cause an exception.</remarks>
    /// <param name="search">The non-empty text to find.</param>
    /// <param name="replacement">The replacement text.</param>
    /// <param name="options">The matching and layout options, or <c>null</c> for defaults.</param>
    /// <returns>The number of replacements.</returns>
    public int ReplaceText(string search, string replacement, ReplaceTextOptions? options = null)
        => TextReplacer.Replace(this, search, replacement, options);

    /// <summary>Irreversibly removes page content intersecting the specified regions.</summary>
    /// <param name="areas">The redaction regions in PDF user-space coordinates.</param>
    /// <param name="options">The redaction appearance options, or <c>null</c> for no fill.</param>
    public void Redact(IEnumerable<Rect> areas, RedactionOptions? options = null)
        => Redactor.Redact(this, areas, options);

    /// <summary>Finds text and irreversibly redacts the bounds of every match.</summary>
    /// <param name="text">The non-empty text to redact.</param>
    /// <param name="searchOptions">The text matching options, or <c>null</c> for defaults.</param>
    /// <param name="redactionOptions">The redaction appearance options, or <c>null</c> for no fill.</param>
    /// <returns>The number of redacted matches.</returns>
    public int RedactText(string text, TextSearchOptions? searchOptions = null, RedactionOptions? redactionOptions = null)
        => Redactor.RedactText(this, text, searchOptions, redactionOptions);

    internal IReadOnlyList<TextHit> FindText(string text, TextSearchOptions? options, int pageIndex)
        => TextSearch.Find(content, textFonts, text, options, pageIndex);

    internal void SetTextFonts(IReadOnlyDictionary<string, Fonts.ReverseFont> fonts)
    {
        textFonts = fonts;
    }

    // The reverse char-code -> Unicode maps used by ExtractText, exposed so Document.Append
    // can carry them onto a copied page (a Type0/Identity-H stream is not reversible without them).
    internal IReadOnlyDictionary<string, Fonts.ReverseFont>? TextFonts => textFonts;

    internal byte[]? RawContent => content;

    // Whether the retained bytes are still the whole story: no elements were materialized
    // (and so none can have been edited) and no overlay is queued. Read by PageOperations to
    // decide whether a copy can take the bytes verbatim.
    internal bool ContentIsIntact => !materialized && pendingAppends.Count == 0;

    // Stamping a constant-size overlay onto a loaded page must not pay to parse and re-emit
    // the whole content stream. Queuing the element leaves the raw bytes untouched, so
    // BuildContent emits exactly the overlay the intact-append path would have produced;
    // materializing later folds the queue in where Content.Add would have placed it.
    internal void AppendContent(ContentElement element)
    {
        if (materialized || content is null || Generated is not null)
        {
            Content.Add(element);
            return;
        }

        pendingAppends.Add(element);
    }

    internal void SetReservedResourceNames(IReadOnlyCollection<string> names) => reservedResourceNames = names;

    internal void ApplyPendingContentEdits()
    {
        if (pendingAppends.Count > 0)
        {
            EnsureMaterialized();
        }

        if (!materialized)
        {
            return;
        }

        if (content is null || sourceElements is null)
        {
            throw new NotSupportedException("Content collection edits cannot be composed with raw content editing because the page has no safely mapped serialized content stream.");
        }

        var reserved = new HashSet<string>(reservedResourceNames ?? []);
        AddResourceNames(reserved, editedResources);
        var emission = ContentEditor.Reemit(content, elements, sourceElements,
            SafePrefix("F", reserved), SafePrefix("Im", reserved), SafePrefix("GS", reserved));
        if (emission.Resources.Patterns.Count > 0)
        {
            throw new NotSupportedException("Inserted gradient content cannot be composed with raw content editing because pattern resource names cannot be allocated safely.");
        }

        editedResources = ContentResourceManifest.Combine(editedResources, emission.Resources);
        if (emission.Resources.Fonts.Count > 0)
        {
            var fonts = textFonts is null
                ? new Dictionary<string, Fonts.ReverseFont>(StringComparer.Ordinal)
                : new Dictionary<string, Fonts.ReverseFont>(textFonts, StringComparer.Ordinal);
            foreach (var font in emission.Resources.Fonts)
            {
                fonts[font.Value] = Fonts.ReverseFont.FromBase14(font.Key);
            }

            textFonts = fonts;
        }

        content = emission.Bytes ?? throw new InvalidOperationException("Content re-emission did not produce a serialized stream.");
        ResetMaterialization();
    }

    internal void ApplyEditedContent(byte[] value)
    {
        content = value;
        ResetMaterialization();
    }

    // Resolves the content-stream bytes to write. A loaded page that was never materialized
    // reuses its retained raw bytes. A loaded page whose original elements are intact but
    // that gained new elements keeps its raw bytes untouched and returns the additions as a
    // separate overlay stream. Any other modification (or a freshly authored page)
    // re-encodes from elements; the emitters carry the resources each stream needs.
    internal ContentEmissionResult BuildContent(IReadOnlyCollection<string>? reservedNames = null)
    {
        if (!editedResources.IsEmpty)
        {
            var combinedNames = new HashSet<string>(reservedNames ?? []);
            AddResourceNames(combinedNames, editedResources);
            reservedNames = combinedNames;
        }

        // Queued appends imply a loaded page that was never materialized, so its raw bytes
        // are still intact and only the additions need emitting.
        if (pendingAppends.Count > 0)
        {
            using var pending = new ContentWriter(
                SafePrefix("SF", reservedNames),
                SafePrefix("SIm", reservedNames),
                SafePrefix("SGS", reservedNames));
            foreach (var element in pendingAppends)
            {
                element.Emit(pending);
            }

            var pendingOverlay = pending.DetachResult();
            return new ContentEmissionResult(content,
                ContentResourceManifest.Combine(editedResources, pendingOverlay.Resources),
                new ContentEmissionResult(pendingOverlay.Bytes, ContentResourceManifest.Empty, isEmitted: true));
        }

        // An empty collection means "reuse the raw bytes" only when nothing was ever
        // materialized from them; once it was, empty means the caller removed everything
        // and reusing the raw bytes would restore the removed content.
        if (elements.Count == 0 && materializedCount == 0)
        {
            return new ContentEmissionResult(content, editedResources);
        }

        if (content is not null && snapshot is not null && elements.Count >= materializedCount
            && OriginalElementsIntact())
        {
            if (elements.Count == materializedCount)
            {
                return new ContentEmissionResult(content, editedResources);
            }

            using var appended = new ContentWriter(
                SafePrefix("SF", reservedNames),
                SafePrefix("SIm", reservedNames),
                SafePrefix("SGS", reservedNames));
            for (var i = materializedCount; i < elements.Count; i++)
            {
                elements[i].Emit(appended);
            }

            var overlay = appended.DetachResult();
            return new ContentEmissionResult(content,
                ContentResourceManifest.Combine(editedResources, overlay.Resources),
                new ContentEmissionResult(overlay.Bytes, ContentResourceManifest.Empty, isEmitted: true));
        }

        if (content is not null && sourceElements is not null)
        {
            var emission = ContentEditor.Reemit(content, elements, sourceElements,
                SafePrefix("F", reservedNames), SafePrefix("Im", reservedNames), SafePrefix("GS", reservedNames));
            return new ContentEmissionResult(emission.Bytes,
                ContentResourceManifest.Combine(editedResources, emission.Resources), isEmitted: true);
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

        var authored = writer.DetachResult();
        return new ContentEmissionResult(authored.Bytes,
            ContentResourceManifest.Combine(editedResources, authored.Resources), isEmitted: true);
    }

    private bool OriginalElementsIntact()
    {
        using var writer = new ContentWriter();
        for (var i = 0; i < materializedCount; i++)
        {
            elements[i].Emit(writer);
        }

        return snapshot is not null && writer.ToArray().AsSpan().SequenceEqual(snapshot);
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
            FlushPendingAppends();
            return;
        }

        ContentInterpreter.Materialize(content, elements, textFonts);
        materializedCount = elements.Count;
        sourceElements = ContentEditor.Map(content, elements);

        using var writer = new ContentWriter();
        foreach (var element in elements)
        {
            element.Emit(writer);
        }

        snapshot = writer.ToArray();
        FlushPendingAppends();
    }

    // Queued appends join the elements only after materializedCount and the snapshot are
    // fixed, so they count as additions rather than as original content.
    private void FlushPendingAppends()
    {
        foreach (var element in pendingAppends)
        {
            elements.Add(element);
        }

        pendingAppends.Clear();
    }

    private void ResetMaterialization()
    {
        elements.Clear();
        materialized = false;
        materializedCount = 0;
        snapshot = null;
        sourceElements = null;
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

    private static void AddResourceNames(HashSet<string> names, ContentResourceManifest resources)
    {
        foreach (var item in resources.Fonts)
        {
            names.Add(item.Value);
        }

        foreach (var item in resources.Images)
        {
            names.Add(item.Key);
        }

        foreach (var item in resources.ExtGStates)
        {
            names.Add(item.Key);
        }

        foreach (var item in resources.Patterns)
        {
            names.Add(item.Key);
        }
    }
}

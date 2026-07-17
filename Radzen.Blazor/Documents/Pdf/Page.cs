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
    private int rotate;
    private Unit width;
    private Unit height;
    private PdfRect mediaBox;
    private PdfRect? cropBox;
    private IReadOnlyDictionary<string, Fonts.ReverseFont>? textFonts;
    private IReadOnlyList<ContentEditor.SourceElement>? sourceElements;
    private ContentResourceManifest editedResources = ContentResourceManifest.Empty;
    private IReadOnlyCollection<string>? reservedResourceNames;

    internal Page(Unit width, Unit height)
    {
        this.width = width;
        this.height = height;
        mediaBox = PdfRect.FromSize(0, 0, width.Point, height.Point);
    }

    internal GeneratedPage? Generated { get; set; }

    internal Document? Owner { get; set; }

    internal Fonts.FontScope FontScope => Owner?.FontScope ?? default;

    /// <summary>Gets the page width in points.</summary>
    public Unit Width => width;

    /// <summary>Gets the page height in points.</summary>
    public Unit Height => height;

    /// <summary>
    /// Gets or sets the media box (<c>/MediaBox</c>) in PDF user-space coordinates.
    /// Changing the box changes the page dimensions but does not scale, translate or
    /// otherwise modify existing content coordinates.
    /// </summary>
    public PdfRect MediaBox
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
    public PdfRect? CropBox
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

    internal void SetPreservedMediaBox(PdfRect value) => SetMediaBox(value, false);

    internal void SetPreservedCropBox(PdfRect value) => cropBox = value;

    private void SetMediaBox(PdfRect value, bool explicitlySet)
    {
        mediaBox = value;
        width = Unit.FromPoint(value.Width);
        height = Unit.FromPoint(value.Height);
        MediaBoxSet = explicitlySet;
    }

    private static void ValidateBox(PdfRect value, string parameterName)
    {
        if (!double.IsFinite(value.Left) || !double.IsFinite(value.Bottom)
            || !double.IsFinite(value.Right) || !double.IsFinite(value.Top)
            || value.Width <= 0 || value.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Page boxes must have finite coordinates and positive dimensions.");
        }
    }

    /// <summary>
    /// Gets or sets the bleed box (<c>/BleedBox</c>): the region to which page contents
    /// are clipped when output in a production environment. When <see langword="null"/> no
    /// bleed box is written unless one was preserved from a loaded page.
    /// </summary>
    public PdfRect? BleedBox { get; set; }

    /// <summary>
    /// Gets or sets the trim box (<c>/TrimBox</c>): the intended finished dimensions of
    /// the page after trimming. When <see langword="null"/> no trim box is written unless
    /// one was preserved from a loaded page.
    /// </summary>
    public PdfRect? TrimBox { get; set; }

    /// <summary>
    /// Gets or sets the art box (<c>/ArtBox</c>): the extent of the page's meaningful
    /// content as intended by its creator. When <see langword="null"/> no art box is written
    /// unless one was preserved from a loaded page.
    /// </summary>
    public PdfRect? ArtBox { get; set; }

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

            Owner?.Loaded?.SourceRotations.Remove(this);
        }
    }

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
        SetContentCore(value);

        ContentReplaced = true;
    }

    internal void SetLoadedContent(byte[] value) => SetContentCore(value);

    private void SetContentCore(byte[] value)
    {
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
    public string ExtractText() => TextSearch.ExtractText(CurrentContent, textFonts);

    /// <summary>
    /// Extracts decoded text-show runs in reading order with transformed em-box
    /// geometry and source operator ordinals.
    /// </summary>
    /// <remarks>
    /// Geometry is an estimated em box because PDF content streams do not always
    /// expose glyph outlines or shaping clusters. Form XObject text is not included.
    /// </remarks>
    /// <returns>The positioned text-show runs, or an empty list when the page has no text.</returns>
    public IReadOnlyList<PositionedTextRun> ExtractPositionedText() => ExtractPositionedText(null);

    internal IReadOnlyList<PositionedTextRun> ExtractPositionedText(ContentTokenizer.Cache? cache)
        => TextSearch.Extract(CurrentContent, textFonts, cache);

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
        => FindText(text, options, -1);

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
    public void Redact(IEnumerable<PdfRect> areas, RedactionOptions? options = null)
        => Redactor.Redact(this, areas, options);

    /// <summary>Finds text and irreversibly redacts the bounds of every match.</summary>
    /// <param name="text">The non-empty text to redact.</param>
    /// <param name="searchOptions">The text matching options, or <c>null</c> for defaults.</param>
    /// <param name="redactionOptions">The redaction appearance options, or <c>null</c> for no fill.</param>
    /// <returns>The number of redacted matches.</returns>
    public int RedactText(string text, TextSearchOptions? searchOptions = null, RedactionOptions? redactionOptions = null)
        => Redactor.RedactText(this, text, searchOptions, redactionOptions);

    internal IReadOnlyList<TextHit> FindText(string text, TextSearchOptions? options, int pageIndex, ContentTokenizer.Cache? cache = null)
        => TextSearch.Find(CurrentContent, textFonts, text, options, pageIndex, cache);

    internal void SetTextFonts(IReadOnlyDictionary<string, Fonts.ReverseFont> fonts)
    {
        textFonts = fonts;
    }

    internal IReadOnlyDictionary<string, Fonts.ReverseFont>? TextFonts => textFonts;

    internal byte[]? CurrentContent
    {
        get
        {
            ApplyPendingContentEdits();
            return content;
        }
    }

    internal bool ContentIsIntact => !ContentReplaced && !materialized && pendingAppends.Count == 0;

    internal bool ContentReplaced { get; private set; }

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

    private void ApplyPendingContentEdits()
    {
        if (pendingAppends.Count > 0)
        {
            EnsureMaterialized();
        }

        if (!materialized)
        {
            return;
        }

        if (OriginalElementsIntact() && elements.Count == materializedCount)
        {
            ResetMaterialization();
            return;
        }

        if (content is null || sourceElements is null)
        {
            throw new NotSupportedException("Content collection edits cannot be composed with raw content editing because the page has no safely mapped serialized content stream.");
        }

        var reserved = new HashSet<string>(reservedResourceNames ?? []);
        AddResourceNames(reserved, editedResources);
        var emission = ContentEditor.Reemit(content, elements, sourceElements, FontScope,
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
        ContentReplaced = true;
    }

    internal void ApplyEditedContent(byte[] value)
    {
        SetContentCore(value);
        ContentReplaced = true;
    }

    internal ContentEmissionResult BuildContent(IReadOnlyCollection<string>? reservedNames = null)
    {
        if (!editedResources.IsEmpty)
        {
            var combinedNames = new HashSet<string>(reservedNames ?? []);
            AddResourceNames(combinedNames, editedResources);
            reservedNames = combinedNames;
        }

        if (pendingAppends.Count > 0)
        {
            using var pending = new ContentWriter(
                FontScope,
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

        if (elements.Count == 0 && materializedCount == 0)
        {
            return new ContentEmissionResult(content, editedResources);
        }

        if (content is not null && OriginalElementsIntact())
        {
            if (elements.Count == materializedCount)
            {
                return new ContentEmissionResult(content, editedResources);
            }

            using var appended = new ContentWriter(
                FontScope,
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
            var emission = ContentEditor.Reemit(content, elements, sourceElements, FontScope,
                SafePrefix("F", reservedNames), SafePrefix("Im", reservedNames), SafePrefix("GS", reservedNames));
            return new ContentEmissionResult(emission.Bytes,
                ContentResourceManifest.Combine(editedResources, emission.Resources), isEmitted: true);
        }

        using var writer = new ContentWriter(
            FontScope,
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
        if (sourceElements is null || elements.Count < materializedCount)
        {
            return false;
        }

        for (var i = 0; i < materializedCount; i++)
        {
            if (elements[i].IsModified || !ReferenceEquals(elements[i], sourceElements[i].Element))
            {
                return false;
            }
        }

        return true;
    }

    internal ContentEmissionResult? BuildOverlay()
    {
        if (elements.Count == 0)
        {
            return null;
        }

        using var writer = new ContentWriter(FontScope, "SF", "SIm", "SGS");
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

        var cache = new ContentTokenizer.Cache();
        ContentInterpreter.Materialize(content, elements, textFonts, cache);
        materializedCount = elements.Count;
        sourceElements = ContentEditor.Map(content, elements, cache);

        foreach (var element in elements)
        {
            element.AcceptChanges();
        }

        FlushPendingAppends();
    }

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
        sourceElements = null;
    }

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

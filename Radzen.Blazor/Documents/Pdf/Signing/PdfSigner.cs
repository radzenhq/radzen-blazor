using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Radzen.Documents.Pdf.Objects;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Documents.Pdf.Signing;

/// <summary>
/// Adds an approval signature to an existing PDF as an incremental update
/// (ISO 32000-1 section 12.8): the original bytes are preserved verbatim as a
/// prefix, a signature field and dictionary are appended, and a detached
/// PKCS#7/CMS signature produced by a caller-supplied <see cref="ISigner"/>
/// is embedded in <c>/Contents</c>.
/// </summary>
/// <remarks>
/// The <c>/ByteRange</c> convention: with <c>gapStart</c> the offset of the
/// <c>&lt;</c> that opens the <c>/Contents</c> hex string and <c>gapEnd</c>
/// the offset just after the closing <c>&gt;</c>, the array is
/// <c>[0 gapStart gapEnd length-gapEnd]</c> - the two signed segments are
/// <c>[0, gapStart)</c> and <c>[gapEnd, length)</c>, i.e. everything except
/// the <c>&lt;...&gt;</c> token including both angle brackets. Output is
/// deterministic: the library reads no clock and uses no randomness, so
/// identical inputs (including <see cref="SignatureOptions.SigningTime"/> and
/// the signer's output) produce identical bytes.
/// </remarks>
public static class PdfSigner
{
    internal const int ByteRangeInteriorWidth = 40;

    // The fixed-width /ByteRange and /Contents placeholders, shared with the
    // document time-stamper so the in-place patch logic sees an identical shape.
    internal static DocumentObject ByteRangePlaceholder()
        => new RawTokenObject("[" + "0 0 0 0".PadRight(ByteRangeInteriorWidth) + "]");

    internal static DocumentObject ContentsPlaceholder(int reservedBytes)
        => new RawTokenObject("<" + new string('0', reservedBytes * 2) + ">");

    /// <summary>
    /// Signs <paramref name="pdf"/> and returns the signed document. The
    /// original bytes are an exact prefix of the result.
    /// </summary>
    /// <param name="pdf">The complete bytes of the document to sign.</param>
    /// <param name="options">Signature appearance and sizing options.</param>
    /// <param name="signer">Produces the detached CMS signature. See <see cref="ISigner"/>.</param>
    /// <returns>The bytes of the signed document.</returns>
    /// <exception cref="InvalidOperationException">The signer returned a blob
    /// larger than <see cref="SignatureOptions.SignatureMaxSizeBytes"/>.</exception>
    public static byte[] Sign(byte[] pdf, SignatureOptions options, ISigner signer)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(signer);
        // Upper bound keeps SignatureMaxSizeBytes * 2 (the reserved hex-digit
        // count) well inside int range; real CMS blobs are a few KB.
        const int maxReservation = 16 * 1024 * 1024;
        if (options.SignatureMaxSizeBytes < 1 || options.SignatureMaxSizeBytes > maxReservation)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.SignatureMaxSizeBytes,
                $"SignatureMaxSizeBytes must be between 1 and {maxReservation}.");
        }

        if (string.IsNullOrEmpty(options.SubFilter))
        {
            throw new ArgumentException("SubFilter must not be empty.", nameof(options));
        }

        if (options.Appearance is { } appearanceOptions)
        {
            ValidateAppearance(appearanceOptions);
        }

        var signature = BuildSignatureDictionary(options);
        var appearance = options.Appearance is not null ? BuildAppearanceStream(options) : null;
        var (bytes, sigStart, sigEnd) = AppendSignatureField(
            pdf, signature, appearance, SignatureRect(options.Appearance), options.Appearance?.PageIndex ?? 0);

        return Embed(bytes, sigStart, sigEnd, options.SignatureMaxSizeBytes,
            content => signer.Sign(content) ?? throw new InvalidOperationException("The signer returned null."),
            "Increase SignatureOptions.SignatureMaxSizeBytes.");
    }

    // Registers a caller-built signature dictionary as a signature field/widget
    // via an incremental update: adds the sig dictionary, the widget annotation
    // (with an optional appearance stream), merges the AcroForm and the page
    // /Annots, serializes, and reports where the sig dictionary starts/ends so
    // its /ByteRange and /Contents placeholders can be patched in isolation.
    // Shared by ordinary signing and the document time-stamper.
    internal static (byte[] Bytes, int SigStart, int SigEnd) AppendSignatureField(
        byte[] pdf, DictionaryObject signature, StreamObject? appearanceStream, ArrayObject rect, int pageIndex)
    {
        var reader = DocumentReader.Parse(pdf);
        if (reader.IsEncrypted)
        {
            throw new NotSupportedException("Signing encrypted documents is not supported.");
        }

        if (!(reader.Trailer.TryGetValue("Root", out var root) && root is ReferenceObject rootRef
            && reader.Resolve(rootRef) is DictionaryObject catalog))
        {
            throw new DocumentParseException("The trailer /Root must reference the document catalog.", -1);
        }

        var writer = new IncrementalUpdateWriter(pdf, reader);
        var (pageRef, page) = FindPage(reader, catalog, pageIndex);

        var signatureRef = writer.Add(signature);

        catalog.TryGetValue("AcroForm", out var acroFormValue);
        var existingAcroForm = acroFormValue is null ? null : reader.AsDictionary(acroFormValue);
        var fieldName = UniqueFieldName(reader, existingAcroForm);

        var field = new DictionaryObject
        {
            ["Type"] = new NameObject("Annot"),
            ["Subtype"] = new NameObject("Widget"),
            ["FT"] = new NameObject("Sig"),
            ["T"] = new StringObject(fieldName),
            ["V"] = signatureRef,
            ["Rect"] = rect,
            ["F"] = new NumberObject(132),
            ["P"] = pageRef,
        };

        // A visible signature carries a generated appearance; the invisible default
        // keeps the zero rect and no /AP so unchanged callers stay byte-identical.
        if (appearanceStream is not null)
        {
            field["AP"] = new DictionaryObject { ["N"] = writer.Add(appearanceStream) };
        }

        var fieldRef = writer.Add(field);

        var acroForm = BuildAcroForm(reader, existingAcroForm, fieldRef);
        if (acroFormValue is ReferenceObject acroFormRef)
        {
            writer.Override(acroFormRef.ObjectNumber, acroForm);
        }
        else
        {
            var newCatalog = catalog.Copy();
            newCatalog["AcroForm"] = writer.Add(acroForm);
            writer.Override(rootRef.ObjectNumber, newCatalog);
        }

        AppendAnnotation(reader, writer, pageRef, page, fieldRef);
        var bytes = writer.ToArray();

        // Bound the placeholder scan to the signature dictionary's own serialized
        // bytes. The sig dict is the first appended object and the field is the
        // next; every object copied from the (possibly hostile) input is an
        // override with a lower number, so it is written before sigStart and
        // cannot be mistaken for the real /Contents or /ByteRange placeholder.
        var sigStart = checked((int)writer.OffsetOf(signatureRef));
        var sigEnd = checked((int)writer.OffsetOf(fieldRef));
        return (bytes, sigStart, sigEnd);
    }

    private static DictionaryObject BuildSignatureDictionary(SignatureOptions options)
    {
        var signature = new DictionaryObject
        {
            ["Type"] = new NameObject("Sig"),
            ["Filter"] = new NameObject("Adobe.PPKLite"),
            ["SubFilter"] = new NameObject(options.SubFilter),
            ["ByteRange"] = ByteRangePlaceholder(),
            ["Contents"] = ContentsPlaceholder(options.SignatureMaxSizeBytes),
        };

        if (options.SignerName is not null)
        {
            signature["Name"] = new StringObject(options.SignerName);
        }

        if (options.SigningTime is { } time)
        {
            var utc = time.ToUniversalTime();
            signature["M"] = new StringObject(
                "D:" + utc.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + "+00'00'");
        }

        if (options.Reason is not null)
        {
            signature["Reason"] = new StringObject(options.Reason);
        }

        if (options.Location is not null)
        {
            signature["Location"] = new StringObject(options.Location);
        }

        if (options.ContactInfo is not null)
        {
            signature["ContactInfo"] = new StringObject(options.ContactInfo);
        }

        return signature;
    }

    // A merged copy of the existing AcroForm (never dropping /Fields) with the
    // new field appended and /SigFlags carrying SignaturesExist | AppendOnly.
    private static DictionaryObject BuildAcroForm(DocumentReader reader, DictionaryObject? existing, ReferenceObject fieldRef)
    {
        var acroForm = existing is null ? new DictionaryObject() : existing.Copy();

        var fields = new ArrayObject();
        if (existing is not null && reader.GetArray(existing, "Fields") is { } existingFields)
        {
            foreach (var item in existingFields)
            {
                fields.Add(item);
            }
        }

        fields.Add(fieldRef);
        acroForm["Fields"] = fields;

        var sigFlags = 3;
        if (existing is not null && reader.GetInt(existing, "SigFlags") is { } flags)
        {
            sigFlags |= flags;
        }

        acroForm["SigFlags"] = new NumberObject(sigFlags);
        return acroForm;
    }

    private static void AppendAnnotation(DocumentReader reader, IncrementalUpdateWriter writer,
        ReferenceObject pageRef, DictionaryObject page, ReferenceObject fieldRef)
    {
        page.TryGetValue("Annots", out var annotsValue);

        var annots = new ArrayObject();
        if (annotsValue is not null && reader.AsArray(annotsValue) is { } existing)
        {
            foreach (var item in existing)
            {
                annots.Add(item);
            }
        }

        annots.Add(fieldRef);

        if (annotsValue is ReferenceObject annotsRef)
        {
            writer.Override(annotsRef.ObjectNumber, annots);
        }
        else
        {
            var newPage = page.Copy();
            newPage["Annots"] = annots;
            writer.Override(pageRef.ObjectNumber, newPage);
        }
    }

    // A visible signature's rectangle and appearance BBox derive directly from
    // these numbers, so reject values that would yield a degenerate, inverted or
    // non-finite widget the viewer renders as nothing or garbage.
    private static void ValidateAppearance(SignatureAppearance appearance)
    {
        RequireFinite(appearance.X, nameof(appearance.X));
        RequireFinite(appearance.Y, nameof(appearance.Y));
        RequirePositive(appearance.Width, nameof(appearance.Width));
        RequirePositive(appearance.Height, nameof(appearance.Height));
    }

    private static void RequireFinite(double value, string name)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(name, value, "The signature appearance coordinate must be a finite number.");
        }
    }

    private static void RequirePositive(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, value, "The signature appearance dimension must be a finite positive number.");
        }
    }

    // The signature widget's rectangle: the visible appearance rectangle, or the
    // historic invisible [0 0 0 0] when no appearance is requested.
    private static ArrayObject SignatureRect(SignatureAppearance? appearance)
    {
        if (appearance is null)
        {
            return [new NumberObject(0), new NumberObject(0), new NumberObject(0), new NumberObject(0)];
        }

        return
        [
            new NumberObject(appearance.X),
            new NumberObject(appearance.Y),
            new NumberObject(appearance.X + appearance.Width),
            new NumberObject(appearance.Y + appearance.Height),
        ];
    }

    // Draws the signer name, reason and signing time into the appearance box.
    private static StreamObject BuildAppearanceStream(SignatureOptions options)
    {
        var appearance = options.Appearance!;
        var lines = new List<string>();
        if (!string.IsNullOrEmpty(options.SignerName))
        {
            lines.Add(options.SignerName);
        }

        if (!string.IsNullOrEmpty(options.Reason))
        {
            lines.Add("Reason: " + options.Reason);
        }

        if (options.SigningTime is { } time)
        {
            lines.Add(time.ToUniversalTime().ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture));
        }

        var font = new Font { Name = "Helvetica", Size = 9 };
        // Signing works on finished bytes, not a Document, so no registry or conformance level
        // is knowable here; the appearance can only ever use a base-14 face.
        return FieldAppearances.BuildSignatureAppearance(
            lines, appearance.Width, appearance.Height, font, scope: default);
    }

    // Returns the page leaf at the given zero-based index in document order, so a
    // visible signature can target a specific page (index 0 is the first page).
    private static (ReferenceObject Reference, DictionaryObject Page) FindPage(DocumentReader reader, DictionaryObject catalog, int index)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "The signature page index must not be negative.");
        }

        if (!catalog.TryGetValue("Pages", out var pages) || pages is not ReferenceObject rootRef)
        {
            throw new DocumentParseException("The catalog /Pages must be an indirect reference.", -1);
        }

        var counter = 0;
        var found = FindLeaf(reader, rootRef, index, ref counter, 0);
        return found ?? throw new ArgumentOutOfRangeException(
            nameof(index), index, $"The signature page index is past the last page ({counter} pages).");
    }

    // Depth-first in-order walk of the page tree; returns the (ref, dict) when the
    // running leaf counter reaches the target index.
    private static (ReferenceObject, DictionaryObject)? FindLeaf(
        DocumentReader reader, ReferenceObject nodeRef, int target, ref int counter, int depth)
    {
        // The same tree the loader accepted must not be rejected here, so the cap is the
        // reader's configured page-tree limit rather than a constant private to signing.
        if (depth > reader.Limits.MaxPageTreeDepth)
        {
            throw new DocumentParseException("Maximum page tree depth exceeded.", -1);
        }

        if (reader.AsDictionary(nodeRef) is not { } node)
        {
            throw new DocumentParseException("A page tree node is not a dictionary.", -1);
        }

        if (!node.TryGetValue("Kids", out var kidsValue) || kidsValue is null)
        {
            if (counter == target)
            {
                return (nodeRef, node);
            }

            counter++;
            return null;
        }

        if (reader.AsArray(kidsValue) is not { } kids)
        {
            throw new DocumentParseException("The page tree /Kids must be an array.", -1);
        }

        foreach (var kid in kids)
        {
            if (kid is not ReferenceObject kidRef)
            {
                throw new DocumentParseException("The page tree /Kids must hold references.", -1);
            }

            var found = FindLeaf(reader, kidRef, target, ref counter, depth + 1);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static string UniqueFieldName(DocumentReader reader, DictionaryObject? acroForm)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        if (acroForm is not null && reader.GetArray(acroForm, "Fields") is { } fields)
        {
            foreach (var item in fields)
            {
                if (reader.AsDictionary(item) is { } field && reader.GetString(field, "T") is { } name)
                {
                    names.Add(name);
                }
            }
        }

        for (var i = 1; ; i++)
        {
            var candidate = "Signature" + i.ToString(CultureInfo.InvariantCulture);
            if (!names.Contains(candidate))
            {
                return candidate;
            }
        }
    }

    internal delegate byte[] BlobProducer(SignedContent content);

    // Patches /ByteRange in place (length-preserving), hands the two covered
    // ranges to <paramref name="produceBlob"/> as views into the document itself,
    // and hex-encodes the returned blob into the /Contents gap. Shared by ordinary
    // signing (the blob is a CMS SignedData) and document time-stamping (an RFC
    // 3161 token).
    internal static byte[] Embed(byte[] bytes, int sigStart, int sigEnd, int reservedBytes,
        BlobProducer produceBlob, string reservedHint)
    {
        var hexDigits = reservedBytes * 2;
        var (gapStart, gapEnd) = FindContentsGap(bytes, sigStart, sigEnd, hexDigits);
        PatchByteRange(bytes, sigStart, sigEnd, gapStart, gapEnd);

        if (bytes[gapStart] != (byte)'<' || bytes[gapEnd - 1] != (byte)'>')
        {
            throw new InvalidOperationException("The /Contents placeholder moved while patching /ByteRange.");
        }

        var content = new SignedContent(bytes.AsSpan(0, gapStart), bytes.AsSpan(gapEnd));

        var blob = produceBlob(content);
        if (blob.Length > reservedBytes)
        {
            throw new InvalidOperationException(
                $"The signature is {blob.Length} bytes but only {reservedBytes} bytes are reserved. " + reservedHint);
        }

        HexCodec.Encode(blob, bytes.AsSpan(gapStart + 1, blob.Length * 2), HexCase.Lower);

        return bytes;
    }

    // The placeholder is the only run of exactly 2N zero hex digits following a
    // /Contents key WITHIN the signature dictionary's own bytes [from, end). The
    // scan is bounded to that object so a /Contents string an attacker planted in
    // a copied input object (always written before the sig dict) cannot match.
    private static (int Start, int End) FindContentsGap(byte[] bytes, int from, int end, int hexDigits)
    {
        const string key = "/Contents";
        var index = from;
        while ((index = IndexOf(bytes, index, end, key)) >= 0)
        {
            var i = index + key.Length;
            while (i < end && Lexer.IsWhitespace(bytes[i]))
            {
                i++;
            }

            if (i + hexDigits + 1 < end && bytes[i] == (byte)'<' && bytes[i + hexDigits + 1] == (byte)'>'
                && AllZeros(bytes, i + 1, hexDigits))
            {
                return (i, i + hexDigits + 2);
            }

            index += key.Length;
        }

        throw new InvalidOperationException("Could not locate the /Contents placeholder.");
    }

    private static void PatchByteRange(byte[] bytes, int from, int end, int gapStart, int gapEnd)
    {
        const string key = "/ByteRange";
        var index = IndexOf(bytes, from, end, key);
        while (index >= 0)
        {
            var open = index + key.Length;
            while (open < end && Lexer.IsWhitespace(bytes[open]))
            {
                open++;
            }

            if (open + ByteRangeInteriorWidth + 1 < end && bytes[open] == (byte)'['
                && bytes[open + ByteRangeInteriorWidth + 1] == (byte)']'
                && IsByteRangePlaceholder(bytes, open + 1))
            {
                var text = FormattableString.Invariant($"0 {gapStart} {gapEnd} {bytes.Length - gapEnd}");
                if (text.Length > ByteRangeInteriorWidth)
                {
                    throw new InvalidOperationException("The /ByteRange values exceed the reserved width.");
                }

                text = text.PadRight(ByteRangeInteriorWidth);
                for (var i = 0; i < text.Length; i++)
                {
                    bytes[open + 1 + i] = (byte)text[i];
                }

                return;
            }

            index = IndexOf(bytes, index + key.Length, end, key);
        }

        throw new InvalidOperationException("Could not locate the /ByteRange placeholder.");
    }

    private static bool IsByteRangePlaceholder(byte[] bytes, int start)
    {
        const string prefix = "0 0 0 0";
        for (var i = 0; i < prefix.Length; i++)
        {
            if (bytes[start + i] != (byte)prefix[i])
            {
                return false;
            }
        }

        for (var i = prefix.Length; i < ByteRangeInteriorWidth; i++)
        {
            if (bytes[start + i] != (byte)' ')
            {
                return false;
            }
        }

        return true;
    }

    private static bool AllZeros(byte[] bytes, int start, int count)
    {
        for (var i = start; i < start + count; i++)
        {
            if (bytes[i] != (byte)'0')
            {
                return false;
            }
        }

        return true;
    }

    private static int IndexOf(byte[] bytes, int from, int end, string pattern)
    {
        var last = Math.Min(end, bytes.Length) - pattern.Length;
        for (var i = Math.Max(from, 0); i <= last; i++)
        {
            var match = true;
            for (var j = 0; j < pattern.Length; j++)
            {
                if (bytes[i + j] != (byte)pattern[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }
        }

        return -1;
    }

    // Emits a pre-formatted token verbatim; used for the fixed-width
    // /ByteRange and /Contents placeholders that are patched in place later.
    private sealed class RawTokenObject(string token) : DocumentObject
    {
        internal override void Write(Stream stream, WriteContext context)
        {
            PdfBytes.WriteAscii(stream, token);
        }
    }
}

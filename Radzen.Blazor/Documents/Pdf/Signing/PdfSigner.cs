using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Radzen.Documents.Pdf.Objects;

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
    private const int ByteRangeInteriorWidth = 40;

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
        if (options.SignatureMaxSizeBytes < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.SignatureMaxSizeBytes,
                "SignatureMaxSizeBytes must be positive.");
        }

        if (string.IsNullOrEmpty(options.SubFilter))
        {
            throw new ArgumentException("SubFilter must not be empty.", nameof(options));
        }

        var reader = DocumentReader.Parse(pdf);
        if (reader.IsEncrypted)
        {
            throw new NotSupportedException("Signing encrypted documents is not supported.");
        }

        var writer = new IncrementalUpdateWriter(pdf, reader);

        if (reader.Trailer.TryGetValue("Root", out var root) && root is ReferenceObject rootRef
            && reader.Resolve(rootRef) is DictionaryObject catalog)
        {
            var bytes = Append(reader, writer, catalog, rootRef, options);
            return EmbedSignature(bytes, pdf.Length, options, signer);
        }

        throw new DocumentParseException("The trailer /Root must reference the document catalog.", -1);
    }

    // Registers the signature dictionary, field/widget, AcroForm and page
    // updates with the incremental writer and serializes them.
    private static byte[] Append(DocumentReader reader, IncrementalUpdateWriter writer,
        DictionaryObject catalog, ReferenceObject rootRef, SignatureOptions options)
    {
        var (pageRef, page) = FindFirstPage(reader, catalog);

        var signature = BuildSignatureDictionary(options);
        var signatureRef = writer.Add(signature);

        catalog.TryGetValue("AcroForm", out var acroFormValue);
        var existingAcroForm = acroFormValue is null ? null : reader.Resolve(acroFormValue) as DictionaryObject;
        var fieldName = UniqueFieldName(reader, existingAcroForm);

        var field = new DictionaryObject
        {
            ["Type"] = new NameObject("Annot"),
            ["Subtype"] = new NameObject("Widget"),
            ["FT"] = new NameObject("Sig"),
            ["T"] = new StringObject(fieldName),
            ["V"] = signatureRef,
            ["Rect"] = new ArrayObject
            {
                new NumberObject(0), new NumberObject(0), new NumberObject(0), new NumberObject(0),
            },
            ["F"] = new NumberObject(132),
            ["P"] = pageRef,
        };
        var fieldRef = writer.Add(field);

        var acroForm = BuildAcroForm(reader, existingAcroForm, fieldRef);
        if (acroFormValue is ReferenceObject acroFormRef)
        {
            writer.Override(acroFormRef.ObjectNumber, acroForm);
        }
        else
        {
            var newCatalog = Copy(catalog);
            newCatalog["AcroForm"] = writer.Add(acroForm);
            writer.Override(rootRef.ObjectNumber, newCatalog);
        }

        AppendAnnotation(reader, writer, pageRef, page, fieldRef);
        return writer.ToArray();
    }

    private static DictionaryObject BuildSignatureDictionary(SignatureOptions options)
    {
        var signature = new DictionaryObject
        {
            ["Type"] = new NameObject("Sig"),
            ["Filter"] = new NameObject("Adobe.PPKLite"),
            ["SubFilter"] = new NameObject(options.SubFilter),
            ["ByteRange"] = new RawTokenObject("[" + "0 0 0 0".PadRight(ByteRangeInteriorWidth) + "]"),
            ["Contents"] = new RawTokenObject("<" + new string('0', options.SignatureMaxSizeBytes * 2) + ">"),
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
        var acroForm = existing is null ? new DictionaryObject() : Copy(existing);

        var fields = new ArrayObject();
        if (existing is not null && existing.TryGetValue("Fields", out var fieldsValue)
            && fieldsValue is not null && reader.Resolve(fieldsValue) is ArrayObject existingFields)
        {
            foreach (var item in existingFields)
            {
                fields.Add(item);
            }
        }

        fields.Add(fieldRef);
        acroForm["Fields"] = fields;

        var sigFlags = 3;
        if (existing is not null && existing.TryGetValue("SigFlags", out var flagsValue)
            && flagsValue is not null && reader.Resolve(flagsValue) is NumberObject flags)
        {
            sigFlags |= flags.IntValue;
        }

        acroForm["SigFlags"] = new NumberObject(sigFlags);
        return acroForm;
    }

    private static void AppendAnnotation(DocumentReader reader, IncrementalUpdateWriter writer,
        ReferenceObject pageRef, DictionaryObject page, ReferenceObject fieldRef)
    {
        page.TryGetValue("Annots", out var annotsValue);

        var annots = new ArrayObject();
        if (annotsValue is not null && reader.Resolve(annotsValue) is ArrayObject existing)
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
            var newPage = Copy(page);
            newPage["Annots"] = annots;
            writer.Override(pageRef.ObjectNumber, newPage);
        }
    }

    private static (ReferenceObject Reference, DictionaryObject Page) FindFirstPage(DocumentReader reader, DictionaryObject catalog)
    {
        if (!catalog.TryGetValue("Pages", out var pages) || pages is not ReferenceObject nodeRef)
        {
            throw new DocumentParseException("The catalog /Pages must be an indirect reference.", -1);
        }

        if (reader.Resolve(nodeRef) is not DictionaryObject node)
        {
            throw new DocumentParseException("The page tree root is not a dictionary.", -1);
        }

        var depth = 0;
        while (node.TryGetValue("Kids", out var kidsValue) && kidsValue is not null)
        {
            if (++depth > 64)
            {
                throw new DocumentParseException("The page tree is too deep.", -1);
            }

            if (reader.Resolve(kidsValue) is not ArrayObject kids || kids.Count == 0 || kids[0] is not ReferenceObject kidRef)
            {
                throw new DocumentParseException("The page tree /Kids must be a non-empty array of references.", -1);
            }

            nodeRef = kidRef;
            node = reader.Resolve(kidRef) as DictionaryObject
                ?? throw new DocumentParseException("A page tree node is not a dictionary.", -1);
        }

        return (nodeRef, node);
    }

    private static string UniqueFieldName(DocumentReader reader, DictionaryObject? acroForm)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        if (acroForm is not null && acroForm.TryGetValue("Fields", out var fieldsValue)
            && fieldsValue is not null && reader.Resolve(fieldsValue) is ArrayObject fields)
        {
            foreach (var item in fields)
            {
                if (reader.Resolve(item) is DictionaryObject field
                    && field.TryGetValue("T", out var title) && title is not null
                    && reader.Resolve(title) is StringObject name)
                {
                    names.Add(name.Value);
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

    // Patches /ByteRange in place (length-preserving), collects the two byte
    // ranges, obtains the CMS blob and hex-encodes it into the /Contents gap.
    private static byte[] EmbedSignature(byte[] bytes, int originalLength, SignatureOptions options, ISigner signer)
    {
        var hexDigits = options.SignatureMaxSizeBytes * 2;
        var (gapStart, gapEnd) = FindContentsGap(bytes, originalLength, hexDigits);
        PatchByteRange(bytes, originalLength, gapStart, gapEnd);

        if (bytes[gapStart] != (byte)'<' || bytes[gapEnd - 1] != (byte)'>')
        {
            throw new InvalidOperationException("The /Contents placeholder moved while patching /ByteRange.");
        }

        var content = new byte[gapStart + bytes.Length - gapEnd];
        Array.Copy(bytes, 0, content, 0, gapStart);
        Array.Copy(bytes, gapEnd, content, gapStart, bytes.Length - gapEnd);

        var cms = signer.Sign(content)
            ?? throw new InvalidOperationException("The signer returned null.");
        if (cms.Length > options.SignatureMaxSizeBytes)
        {
            throw new InvalidOperationException(
                $"The signature is {cms.Length} bytes but only {options.SignatureMaxSizeBytes} bytes are reserved. " +
                "Increase SignatureOptions.SignatureMaxSizeBytes.");
        }

        const string hex = "0123456789abcdef";
        for (var i = 0; i < cms.Length; i++)
        {
            bytes[gapStart + 1 + 2 * i] = (byte)hex[cms[i] >> 4];
            bytes[gapStart + 2 + 2 * i] = (byte)hex[cms[i] & 0xF];
        }

        return bytes;
    }

    // The placeholder is the only run of exactly 2N zero hex digits following
    // a /Contents key in the appended section, so a pattern check is unambiguous.
    private static (int Start, int End) FindContentsGap(byte[] bytes, int from, int hexDigits)
    {
        const string key = "/Contents";
        var index = from;
        while ((index = IndexOf(bytes, index, key)) >= 0)
        {
            var i = index + key.Length;
            while (i < bytes.Length && Lexer.IsWhitespace(bytes[i]))
            {
                i++;
            }

            if (i + hexDigits + 1 < bytes.Length && bytes[i] == (byte)'<' && bytes[i + hexDigits + 1] == (byte)'>'
                && AllZeros(bytes, i + 1, hexDigits))
            {
                return (i, i + hexDigits + 2);
            }

            index += key.Length;
        }

        throw new InvalidOperationException("Could not locate the /Contents placeholder.");
    }

    private static void PatchByteRange(byte[] bytes, int from, int gapStart, int gapEnd)
    {
        const string key = "/ByteRange";
        var index = IndexOf(bytes, from, key);
        while (index >= 0)
        {
            var open = index + key.Length;
            while (open < bytes.Length && Lexer.IsWhitespace(bytes[open]))
            {
                open++;
            }

            if (open + ByteRangeInteriorWidth + 1 < bytes.Length && bytes[open] == (byte)'['
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

            index = IndexOf(bytes, index + key.Length, key);
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

    private static int IndexOf(byte[] bytes, int from, string pattern)
    {
        for (var i = Math.Max(from, 0); i <= bytes.Length - pattern.Length; i++)
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

    private static DictionaryObject Copy(DictionaryObject source)
    {
        var copy = new DictionaryObject();
        foreach (var pair in source)
        {
            copy[pair.Key] = pair.Value;
        }

        return copy;
    }

    // Emits a pre-formatted token verbatim; used for the fixed-width
    // /ByteRange and /Contents placeholders that are patched in place later.
    private sealed class RawTokenObject(string token) : DocumentObject
    {
        public override void Write(Stream stream)
        {
            PdfBytes.WriteAscii(stream, token);
        }
    }
}

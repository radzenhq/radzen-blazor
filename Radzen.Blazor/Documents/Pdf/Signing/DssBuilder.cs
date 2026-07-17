using System;
using System.Collections.Generic;
using Radzen.Documents.Crypto;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Signing;

/// <summary>
/// Writes a Document Security Store (the PAdES B-LT building block, ISO 32000-2
/// section 12.8.4.3) into an existing PDF as an incremental update: a catalog
/// <c>/DSS</c> dictionary whose <c>/Certs</c>, <c>/OCSPs</c> and <c>/CRLs</c>
/// arrays reference indirect streams holding caller-supplied DER validation
/// material, optionally indexed per signature by a <c>/VRI</c> sub-dictionary.
/// </summary>
/// <remarks>
/// The library stores the certificate, OCSP-response and CRL bytes verbatim and
/// never parses X.509, OCSP or CRL structures - mirroring the crypto-delegation
/// contract of <see cref="ISigner"/> and <see cref="ITimestampProvider"/>. The
/// <c>/VRI</c> key is the uppercase base-16 SHA-1 digest of the signature's
/// <c>/Contents</c> value, computed with the pure-managed <see cref="Sha1"/> so
/// the code stays WASM-safe. Output is deterministic and the original bytes are
/// an exact prefix of the result. Combine with
/// <see cref="PdfTimestamper.Timestamp(byte[], ITimestampProvider)"/> afterwards
/// to produce a B-LTA (long-term archival) document. Any existing <c>/DSS</c> is
/// merged with rather than replaced: its <c>/Certs</c>, <c>/OCSPs</c>, <c>/CRLs</c>
/// arrays and <c>/VRI</c> entries are carried over so earlier validation material
/// survives when a document is signed or augmented more than once.
/// </remarks>
public static class DssBuilder
{
    /// <summary>
    /// Adds a <c>/DSS</c> with the given validation material and returns the
    /// augmented document (PAdES B-LT, no per-signature index).
    /// </summary>
    /// <param name="pdf">The complete bytes of the document to augment.</param>
    /// <param name="certs">DER-encoded certificates, or <c>null</c>.</param>
    /// <param name="ocsps">DER-encoded OCSP responses, or <c>null</c>.</param>
    /// <param name="crls">DER-encoded CRLs, or <c>null</c>.</param>
    /// <returns>The bytes of the augmented document.</returns>
    /// <exception cref="ArgumentException">All three collections are empty.</exception>
    public static byte[] AddValidationData(byte[] pdf, byte[][]? certs, byte[][]? ocsps, byte[][]? crls)
        => Build(pdf, certs, ocsps, crls, signatureContents: null);

    /// <summary>
    /// Adds a <c>/DSS</c> with the given validation material plus a <c>/VRI</c>
    /// entry indexing it under the signature identified by
    /// <paramref name="signatureContents"/>, and returns the augmented document.
    /// </summary>
    /// <param name="pdf">The complete bytes of the document to augment.</param>
    /// <param name="certs">DER-encoded certificates, or <c>null</c>.</param>
    /// <param name="ocsps">DER-encoded OCSP responses, or <c>null</c>.</param>
    /// <param name="crls">DER-encoded CRLs, or <c>null</c>.</param>
    /// <param name="signatureContents">The value of the signature's
    /// <c>/Contents</c> entry (the raw signature bytes). Its uppercase SHA-1
    /// digest becomes the <c>/VRI</c> key.</param>
    /// <returns>The bytes of the augmented document.</returns>
    /// <exception cref="ArgumentException">All three collections are empty.</exception>
    public static byte[] AddValidationData(
        byte[] pdf, byte[][]? certs, byte[][]? ocsps, byte[][]? crls, byte[] signatureContents)
    {
        ArgumentNullException.ThrowIfNull(signatureContents);
        return Build(pdf, certs, ocsps, crls, signatureContents);
    }

    private static byte[] Build(byte[] pdf, byte[][]? certs, byte[][]? ocsps, byte[][]? crls, byte[]? signatureContents)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        certs ??= [];
        ocsps ??= [];
        crls ??= [];
        if (certs.Length == 0 && ocsps.Length == 0 && crls.Length == 0)
        {
            throw new ArgumentException("At least one of certs, ocsps or crls must be non-empty.", nameof(certs));
        }

        var reader = DocumentReader.Parse(pdf);
        if (reader.IsEncrypted)
        {
            throw new NotSupportedException("Augmenting encrypted documents is not supported.");
        }

        if (!(reader.Trailer.TryGetValue("Root", out var root) && root is ReferenceObject rootRef
            && reader.Resolve(rootRef) is DictionaryObject catalog))
        {
            throw new DocumentParseException("The trailer /Root must reference the document catalog.", -1);
        }

        var writer = new IncrementalUpdateWriter(pdf, reader);

        // Merge with any existing /DSS so a multi-signature document (or a later
        // re-augmentation) keeps the earlier validation material instead of dropping it.
        var existingDss = reader.GetDictionary(catalog, "DSS");

        // Start from a copy of the prior /DSS so any keys beyond the four managed
        // arrays (e.g. proprietary entries from another tool) survive augmentation.
        var dss = existingDss is not null ? existingDss.Copy() : new DictionaryObject();
        dss["Type"] = new NameObject("DSS");

        var certRefs = MergeStreams(writer, reader, existingDss, dss, "Certs", certs);
        var ocspRefs = MergeStreams(writer, reader, existingDss, dss, "OCSPs", ocsps);
        var crlRefs = MergeStreams(writer, reader, existingDss, dss, "CRLs", crls);

        DictionaryObject? vri = existingDss is not null && reader.GetDictionary(existingDss, "VRI") is { } priorVri
                ? priorVri.Copy()
                : null;
        if (signatureContents is not null)
        {
            vri ??= new DictionaryObject();
            var key = Sha1.ComputeHashHex(signatureContents);
            // Merge into any prior entry for this same signature rather than
            // replacing it, so references gathered in earlier passes survive.
            var entry = reader.GetDictionary(vri, key) is { } priorEntry
                    ? priorEntry.Copy()
                    : new DictionaryObject { ["Type"] = new NameObject("VRI") };
            UnionArray(entry, "Cert", reader, certRefs);
            UnionArray(entry, "OCSP", reader, ocspRefs);
            UnionArray(entry, "CRL", reader, crlRefs);
            vri[key] = entry;
        }

        if (vri is not null && vri.Count > 0)
        {
            dss["VRI"] = vri;
        }

        var dssRef = writer.Add(dss);

        var newCatalog = catalog.Copy();
        newCatalog["DSS"] = dssRef;
        writer.Override(rootRef.ObjectNumber, newCatalog);

        return writer.ToArray();
    }

    // Carries the existing /DSS array for <paramref name="key"/> over into
    // <paramref name="target"/>, appends a stream for each new item, and returns
    // the reference for every item. A new item whose bytes match a stream already
    // present is not stored again - its existing reference is reused - so repeated
    // B-LTA refreshes with the same material do not grow the file without bound.
    private static ReferenceObject[] MergeStreams(
        IncrementalUpdateWriter writer, DocumentReader reader, DictionaryObject? existing,
        DictionaryObject target, string key, byte[][] items)
    {
        var array = new ArrayObject();
        var byContent = new Dictionary<string, ReferenceObject>(StringComparer.Ordinal);
        if (existing is not null && reader.GetArray(existing, key) is { } prior)
        {
            foreach (var item in prior)
            {
                array.Add(item);
                if (item is ReferenceObject priorRef && reader.AsStream(priorRef) is { } stream)
                {
                    byContent.TryAdd(Sha1.ComputeHashHex(reader.DecodeStream(stream)), priorRef);
                }
            }
        }

        var refs = new ReferenceObject[items.Length];
        for (var i = 0; i < items.Length; i++)
        {
            ArgumentNullException.ThrowIfNull(items[i]);
            var digest = Sha1.ComputeHashHex(items[i]);
            if (byContent.TryGetValue(digest, out var reused))
            {
                refs[i] = reused;
                continue;
            }

            var added = writer.Add(new StreamObject(items[i]));
            byContent[digest] = added;
            array.Add(added);
            refs[i] = added;
        }

        if (array.Count > 0)
        {
            target[key] = array;
        }

        return refs;
    }

    // Appends the references to a /VRI entry's array, carrying any prior entries
    // and skipping references already present so re-runs stay idempotent.
    private static void UnionArray(DictionaryObject entry, string key, DocumentReader reader, ReferenceObject[] refs)
    {
        var array = new ArrayObject();
        var seen = new HashSet<(int, int)>();
        if (reader.GetArray(entry, key) is { } prior)
        {
            foreach (var item in prior)
            {
                array.Add(item);
                if (item is ReferenceObject r)
                {
                    seen.Add((r.ObjectNumber, r.Generation));
                }
            }
        }

        foreach (var reference in refs)
        {
            if (seen.Add((reference.ObjectNumber, reference.Generation)))
            {
                array.Add(reference);
            }
        }

        if (array.Count > 0)
        {
            entry[key] = array;
        }
    }
}

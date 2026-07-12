using System;
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

        var certRefs = AddStreams(writer, certs);
        var ocspRefs = AddStreams(writer, ocsps);
        var crlRefs = AddStreams(writer, crls);

        // Merge with any existing /DSS so a multi-signature document (or a later
        // re-augmentation) keeps the earlier validation material instead of dropping it.
        var existingDss = catalog.TryGetValue("DSS", out var dssObj) && dssObj is not null && reader.Resolve(dssObj) is DictionaryObject prior ? prior : null;

        var dss = new DictionaryObject { ["Type"] = new NameObject("DSS") };
        MergeArray(dss, "Certs", existingDss, reader, certRefs);
        MergeArray(dss, "OCSPs", existingDss, reader, ocspRefs);
        MergeArray(dss, "CRLs", existingDss, reader, crlRefs);

        DictionaryObject? vri = existingDss is not null && existingDss.TryGetValue("VRI", out var vriObj) && vriObj is not null
            && reader.Resolve(vriObj) is DictionaryObject priorVri
                ? Copy(priorVri)
                : null;
        if (signatureContents is not null)
        {
            var entry = new DictionaryObject { ["Type"] = new NameObject("VRI") };
            AddArray(entry, "Cert", certRefs);
            AddArray(entry, "OCSP", ocspRefs);
            AddArray(entry, "CRL", crlRefs);
            vri ??= new DictionaryObject();
            vri[Sha1.HexUpper(signatureContents)] = entry;
        }

        if (vri is not null && vri.Count > 0)
        {
            dss["VRI"] = vri;
        }

        var dssRef = writer.Add(dss);

        var newCatalog = Copy(catalog);
        newCatalog["DSS"] = dssRef;
        writer.Override(rootRef.ObjectNumber, newCatalog);

        return writer.ToArray();
    }

    private static ReferenceObject[] AddStreams(IncrementalUpdateWriter writer, byte[][] items)
    {
        var refs = new ReferenceObject[items.Length];
        for (var i = 0; i < items.Length; i++)
        {
            ArgumentNullException.ThrowIfNull(items[i]);
            refs[i] = writer.Add(new StreamObject(items[i]));
        }

        return refs;
    }

    private static void AddArray(DictionaryObject target, string key, ReferenceObject[] refs)
    {
        if (refs.Length == 0)
        {
            return;
        }

        var array = new ArrayObject();
        foreach (var reference in refs)
        {
            array.Add(reference);
        }

        target[key] = array;
    }

    // Union the existing /DSS array (its entries stay valid indirect references
    // in the prior revision) with the freshly added streams, preserving order.
    private static void MergeArray(DictionaryObject target, string key, DictionaryObject? existing, DocumentReader reader, ReferenceObject[] refs)
    {
        var array = new ArrayObject();
        if (existing is not null && existing.TryGetValue(key, out var priorObj) && priorObj is not null && reader.Resolve(priorObj) is ArrayObject prior)
        {
            foreach (var item in prior)
            {
                array.Add(item);
            }
        }

        foreach (var reference in refs)
        {
            array.Add(reference);
        }

        if (array.Count > 0)
        {
            target[key] = array;
        }
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
}

using System;
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
/// replaced.
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

        var dss = new DictionaryObject { ["Type"] = new NameObject("DSS") };
        AddArray(dss, "Certs", certRefs);
        AddArray(dss, "OCSPs", ocspRefs);
        AddArray(dss, "CRLs", crlRefs);

        if (signatureContents is not null)
        {
            var entry = new DictionaryObject { ["Type"] = new NameObject("VRI") };
            AddArray(entry, "Cert", certRefs);
            AddArray(entry, "OCSP", ocspRefs);
            AddArray(entry, "CRL", crlRefs);
            dss["VRI"] = new DictionaryObject { [Sha1.HexUpper(signatureContents)] = entry };
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

    private static DictionaryObject Copy(DictionaryObject source)
    {
        var copy = new DictionaryObject();
        foreach (var pair in source)
        {
            copy[pair.Key] = pair.Value;
        }

        return copy;
    }

    // Pure-managed SHA-1 (FIPS 180-4). SHA-1 is used only as the /VRI index key
    // (a non-cryptographic lookup in this context, matching the PAdES/Adobe
    // convention); the BCL implementation is unavailable under Blazor WebAssembly.
    private static class Sha1
    {
        public static string HexUpper(byte[] data)
        {
            var digest = Hash(data);
            const string hex = "0123456789ABCDEF";
            var chars = new char[digest.Length * 2];
            for (var i = 0; i < digest.Length; i++)
            {
                chars[i * 2] = hex[digest[i] >> 4];
                chars[i * 2 + 1] = hex[digest[i] & 0xF];
            }

            return new string(chars);
        }

        private static byte[] Hash(byte[] data)
        {
            uint h0 = 0x67452301, h1 = 0xEFCDAB89, h2 = 0x98BADCFE, h3 = 0x10325476, h4 = 0xC3D2E1F0;

            var padded = Pad(data);
            Span<uint> w = stackalloc uint[80];
            for (var offset = 0; offset < padded.Length; offset += 64)
            {
                for (var i = 0; i < 16; i++)
                {
                    w[i] = ((uint)padded[offset + i * 4] << 24)
                        | ((uint)padded[offset + i * 4 + 1] << 16)
                        | ((uint)padded[offset + i * 4 + 2] << 8)
                        | padded[offset + i * 4 + 3];
                }

                for (var i = 16; i < 80; i++)
                {
                    w[i] = RotL(w[i - 3] ^ w[i - 8] ^ w[i - 14] ^ w[i - 16], 1);
                }

                uint a = h0, b = h1, c = h2, d = h3, e = h4;
                for (var i = 0; i < 80; i++)
                {
                    uint f, k;
                    if (i < 20)
                    {
                        f = (b & c) | (~b & d);
                        k = 0x5A827999;
                    }
                    else if (i < 40)
                    {
                        f = b ^ c ^ d;
                        k = 0x6ED9EBA1;
                    }
                    else if (i < 60)
                    {
                        f = (b & c) | (b & d) | (c & d);
                        k = 0x8F1BBCDC;
                    }
                    else
                    {
                        f = b ^ c ^ d;
                        k = 0xCA62C1D6;
                    }

                    var temp = RotL(a, 5) + f + e + k + w[i];
                    e = d;
                    d = c;
                    c = RotL(b, 30);
                    b = a;
                    a = temp;
                }

                h0 += a;
                h1 += b;
                h2 += c;
                h3 += d;
                h4 += e;
            }

            var result = new byte[20];
            Write(result, 0, h0);
            Write(result, 4, h1);
            Write(result, 8, h2);
            Write(result, 12, h3);
            Write(result, 16, h4);
            return result;
        }

        private static byte[] Pad(byte[] data)
        {
            var bitLength = (ulong)data.Length * 8;
            var total = data.Length + 1;
            var padZeros = (56 - (total % 64) + 64) % 64;
            var padded = new byte[total + padZeros + 8];
            Array.Copy(data, padded, data.Length);
            padded[data.Length] = 0x80;
            for (var i = 0; i < 8; i++)
            {
                padded[padded.Length - 1 - i] = (byte)(bitLength >> (8 * i));
            }

            return padded;
        }

        private static uint RotL(uint value, int bits) => (value << bits) | (value >> (32 - bits));

        private static void Write(byte[] data, int offset, uint value)
        {
            data[offset] = (byte)(value >> 24);
            data[offset + 1] = (byte)(value >> 16);
            data[offset + 2] = (byte)(value >> 8);
            data[offset + 3] = (byte)value;
        }
    }
}

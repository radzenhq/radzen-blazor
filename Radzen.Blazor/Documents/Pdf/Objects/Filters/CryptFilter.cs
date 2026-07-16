using System;

namespace Radzen.Documents.Pdf.Objects.Filters;

/// <summary>
/// Implements the ISO 32000-1 7.4.10 <c>/Crypt</c> filter. The <c>/Name</c> decode
/// parameter selects the crypt filter that decrypts the stream in place of
/// <c>/StmF</c>; <c>/Identity</c> (the default) leaves the data untouched.
/// </summary>
internal sealed class CryptStreamFilter : IStreamFilter
{
    public string Name => "Crypt";

    public byte[] Decode(byte[] data, DictionaryObject? parms, long maxOutput)
    {
        ArgumentNullException.ThrowIfNull(data);

        var name = parms is not null && parms.TryGetValue("Name", out var selected) && selected is NameObject chosen
            ? chosen.Value
            : "Identity";

        // Only /Identity is a no-op. A named crypt filter would have to be decrypted with
        // its /CF entry, which this decoder cannot reach; returning the data unchanged
        // would hand back ciphertext as if it were plaintext, so fail loud instead.
        return string.Equals(name, "Identity", StringComparison.Ordinal)
            ? data
            : throw new DocumentParseException($"Unsupported /Crypt filter '{name}'; only /Identity is supported.", -1);
    }
}

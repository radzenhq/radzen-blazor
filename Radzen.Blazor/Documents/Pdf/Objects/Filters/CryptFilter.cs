using System;

namespace Radzen.Documents.Pdf.Objects.Filters;

// ISO 32000-1 7.4.10: /Crypt filter; /Name selects the crypt filter, /Identity leaves data untouched.
internal sealed class CryptStreamFilter : IStreamFilter
{
    public string Name => "Crypt";

    public byte[] Decode(byte[] data, DictionaryObject? parms, long maxOutput)
    {
        ArgumentNullException.ThrowIfNull(data);

        var name = parms is not null && parms.TryGetValue("Name", out var selected) && selected is NameObject chosen
            ? chosen.Value
            : "Identity";

        return string.Equals(name, "Identity", StringComparison.Ordinal)
            ? data
            : throw new DocumentParseException($"Unsupported /Crypt filter '{name}'; only /Identity is supported.", -1);
    }
}

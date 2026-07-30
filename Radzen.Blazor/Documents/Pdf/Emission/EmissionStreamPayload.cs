using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Emission;

internal sealed class EmissionStreamPayload
{
    private readonly byte[] data;
    private readonly ImmutableArray<KeyValuePair<string, DocumentObject>> entries;

    private EmissionStreamPayload(byte[] data, ImmutableArray<KeyValuePair<string, DocumentObject>> entries)
    {
        this.data = data;
        this.entries = entries;
    }

    public static EmissionStreamPayload Capture(StreamObject stream)
        => new(stream.Data.ToArray(), [.. stream.Dictionary]);

    public bool TryGetValue(string key, out DocumentObject? value)
    {
        foreach (var entry in entries)
        {
            if (string.Equals(entry.Key, key, StringComparison.Ordinal))
            {
                value = entry.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    public StreamObject CreateStream()
    {
        var stream = new StreamObject(data);
        foreach (var entry in entries)
        {
            stream.Dictionary[entry.Key] = entry.Value;
        }

        return stream;
    }
}

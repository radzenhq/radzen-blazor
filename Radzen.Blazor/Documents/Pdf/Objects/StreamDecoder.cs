using Radzen.Documents.Pdf.Objects.Filters;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Objects;

/// <summary>
/// Applies a stream's <c>/Filter</c> chain (with <c>/DecodeParms</c> predictors) in
/// order and enforces the decoded-length and expansion-ratio limits. References inside
/// the filter/parameters dictionaries are resolved through the supplied resolver.
/// </summary>
internal sealed class StreamDecoder(ReaderLimits limits, Func<DocumentObject, DocumentObject> resolve)
{
    private readonly ReaderLimits limits = limits;
    private readonly Func<DocumentObject, DocumentObject> resolve = resolve;

    public byte[] Decode(DictionaryObject dictionary, byte[] data)
    {
        var filter = dictionary.TryGetValue("Filter", out var filterObject) && filterObject is not null
            ? resolve(filterObject)
            : null;
        var names = FilterNames(filter);
        if (names.Count == 0)
        {
            return data;
        }

        if (names.Count > limits.MaxFilterChainLength)
        {
            throw new DocumentParseException("Filter chain exceeds the maximum length.", -1);
        }

        var parms = FilterParms(dictionary, names.Count);
        var result = data;
        var inputLength = data.Length;
        for (var i = 0; i < names.Count; i++)
        {
            result = ApplyFilter(names[i], result, parms[i], limits.MaxDecodedStreamBytes);

            // Cumulative per-stream cap plus a secondary expansion-ratio check that
            // only engages once output clears the floor, so small streams are never
            // rejected for a high ratio on tiny input.
            if (result.LongLength > limits.MaxDecodedStreamBytes)
            {
                throw new DocumentParseException("Decoded stream exceeds the maximum allowed size.", -1);
            }

            if (result.LongLength > limits.ExpansionRatioFloorBytes
                && inputLength > 0
                && result.LongLength / inputLength > limits.MaxDecodeExpansionRatio)
            {
                throw new DocumentParseException("Decoded stream expansion ratio exceeds the maximum.", -1);
            }
        }

        return result;
    }

    private static byte[] ApplyFilter(string name, byte[] data, DictionaryObject? parms, long maxOutput)
        => StreamFilterRegistry.Get(name).Decode(data, parms, maxOutput);

    private List<string> FilterNames(DocumentObject? filter)
    {
        var names = new List<string>();
        if (filter is NameObject name)
        {
            names.Add(name.Value);
        }
        else if (filter is ArrayObject array)
        {
            foreach (var item in array)
            {
                if (resolve(item) is NameObject entryName)
                {
                    names.Add(entryName.Value);
                }
            }
        }

        return names;
    }

    private List<DictionaryObject?> FilterParms(DictionaryObject dictionary, int count)
    {
        var parms = new List<DictionaryObject?>(count);
        DocumentObject? source = null;
        if (dictionary.TryGetValue("DecodeParms", out var direct))
        {
            source = direct;
        }
        else if (dictionary.TryGetValue("DP", out var abbreviated))
        {
            source = abbreviated;
        }

        if (source is not null)
        {
            source = resolve(source);
        }

        if (source is ArrayObject array)
        {
            for (var i = 0; i < count; i++)
            {
                parms.Add(i < array.Count ? resolve(array[i]) as DictionaryObject : null);
            }
        }
        else
        {
            parms.Add(source as DictionaryObject);
            for (var i = 1; i < count; i++)
            {
                parms.Add(null);
            }
        }

        return parms;
    }
}

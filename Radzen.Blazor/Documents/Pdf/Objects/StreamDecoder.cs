using Radzen.Documents.Pdf.Objects.Filters;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Objects;

internal sealed class StreamDecoder(ReaderLimits limits, Func<DocumentObject, DocumentObject> resolve)
{
    private readonly ReaderLimits limits = limits;
    private readonly Func<DocumentObject, DocumentObject> resolve = resolve;

    public byte[] Decode(DictionaryObject dictionary, ReadOnlyMemory<byte> data)
    {
        var hasFilter = dictionary.TryGetValue("Filter", out var filterObject);
        var filter = hasFilter && filterObject is not null ? resolve(filterObject) : null;
        if (hasFilter && filter is not NameObject && filter is not ArrayObject)
        {
            throw new DocumentParseException("Stream /Filter must be a name or an array of names.", -1);
        }

        var names = FilterNames(filter);
        if (names.Count == 0)
        {
            if (data.Length > limits.MaxDecodedStreamBytes)
            {
                throw new DocumentParseException("Decoded stream exceeds the maximum allowed size.", -1);
            }

            return data.ToArray();
        }

        if (names.Count > limits.MaxFilterChainLength)
        {
            throw new DocumentParseException("Filter chain exceeds the maximum length.", -1);
        }

        var parms = FilterParms(dictionary, names.Count);
        var result = data.ToArray();
        var inputLength = data.Length;
        for (var i = 0; i < names.Count; i++)
        {
            result = ApplyFilter(names[i], result, parms[i], limits.MaxDecodedStreamBytes);

            if (result.LongLength > limits.MaxDecodedStreamBytes)
            {
                throw new DocumentParseException("Decoded stream exceeds the maximum allowed size.", -1);
            }

            if (ExceedsExpansionRatio(result.LongLength, inputLength, limits))
            {
                throw new DocumentParseException("Decoded stream expansion ratio exceeds the maximum.", -1);
            }
        }

        return result;
    }

    internal static bool ExceedsExpansionRatio(long decodedLength, long encodedLength, ReaderLimits limits)
        => decodedLength > limits.ExpansionRatioFloorBytes
            && encodedLength > 0
            && decodedLength / encodedLength > limits.MaxDecodeExpansionRatio;

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
                else
                {
                    throw new DocumentParseException("Every stream /Filter array member must be a name.", -1);
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

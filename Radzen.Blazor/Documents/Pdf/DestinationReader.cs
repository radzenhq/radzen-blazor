using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf;

internal readonly record struct DestinationResult(
    OutlineTarget? Target,
    string? Name,
    bool NameIsName,
    bool WasNamed);

internal static class DestinationReader
{
    internal static DestinationResult Read(
        DocumentReader reader,
        DocumentObject destination,
        Func<DictionaryObject, int?> pageIndex,
        IReadOnlyDictionary<string, DocumentObject> namedDestinations,
        bool retainAllFitTypes)
    {
        var resolved = reader.Resolve(destination);
        if (resolved is StringObject text)
        {
            return Named(reader, FormField.DecodeTextString(text.Value), false, pageIndex, namedDestinations, retainAllFitTypes);
        }

        if (resolved is NameObject name)
        {
            return Named(reader, name.Value, true, pageIndex, namedDestinations, retainAllFitTypes);
        }

        return new DestinationResult(Explicit(reader, resolved, pageIndex, retainAllFitTypes), null, false, false);
    }

    private static DestinationResult Named(
        DocumentReader reader,
        string name,
        bool isName,
        Func<DictionaryObject, int?> pageIndex,
        IReadOnlyDictionary<string, DocumentObject> namedDestinations,
        bool retainAllFitTypes)
    {
        if (!namedDestinations.TryGetValue(name, out var destination))
        {
            return new DestinationResult(null, name, isName, true);
        }

        var resolved = reader.Resolve(destination);
        if (resolved is DictionaryObject dictionary && dictionary.TryGetValue("D", out var nested))
        {
            resolved = reader.Resolve(nested!);
        }

        return new DestinationResult(Explicit(reader, resolved, pageIndex, retainAllFitTypes), name, isName, true);
    }

    private static OutlineTarget? Explicit(
        DocumentReader reader,
        DocumentObject destination,
        Func<DictionaryObject, int?> pageIndex,
        bool retainAllFitTypes)
    {
        if (destination is not ArrayObject array || array.Count < 2
            || reader.Resolve(array[0]) is not DictionaryObject page
            || pageIndex(page) is not { } index
            || reader.AsName(array[1]) is not { } fit)
        {
            return null;
        }

        double Argument(int argumentIndex)
            => argumentIndex < array.Count && reader.AsNumber(array[argumentIndex]) is { } number ? number : 0;

        return fit switch
        {
            "Fit" => OutlineTarget.ToPageFit(index),
            "FitH" => OutlineTarget.ToPageFitHorizontal(index, Argument(2)),
            "FitV" when retainAllFitTypes => OutlineTarget.FromLoaded(index, OutlineFit.FitVertical, Argument(2)),
            "FitB" when retainAllFitTypes => OutlineTarget.FromLoaded(index, OutlineFit.FitBounding),
            "FitBH" when retainAllFitTypes => OutlineTarget.FromLoaded(index, OutlineFit.FitBoundingHorizontal, Argument(2)),
            "FitBV" when retainAllFitTypes => OutlineTarget.FromLoaded(index, OutlineFit.FitBoundingVertical, Argument(2)),
            "FitR" => OutlineTarget.ToPageRectangle(index, Argument(2), Argument(3), Argument(4), Argument(5)),
            "XYZ" => OutlineTarget.ToPageXYZ(index, Argument(2), Argument(3), Argument(4)),
            _ => null,
        };
    }
}

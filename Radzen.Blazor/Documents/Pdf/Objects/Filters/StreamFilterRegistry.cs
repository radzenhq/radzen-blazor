using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Objects.Filters;

/// <summary>
/// Maps a PDF filter name (full name or its inline-image abbreviation) to the
/// <see cref="IStreamFilter"/> that decodes it. An unknown name is fail-loud.
/// </summary>
internal static class StreamFilterRegistry
{
    private static readonly Dictionary<string, IStreamFilter> Filters = Build();

    public static IStreamFilter Get(string name)
        => Filters.TryGetValue(name, out var filter)
            ? filter
            : throw new DocumentParseException($"Unsupported stream filter '{name}'.", -1);

    private static Dictionary<string, IStreamFilter> Build()
    {
        var map = new Dictionary<string, IStreamFilter>(StringComparer.Ordinal);
        Register(map, new FlateStreamFilter(), "Fl");
        Register(map, new LzwStreamFilter(), "LZW");
        Register(map, new RunLengthStreamFilter(), "RL");
        Register(map, new AsciiHexStreamFilter(), "AHx");
        Register(map, new Ascii85StreamFilter(), "A85");
        return map;
    }

    private static void Register(Dictionary<string, IStreamFilter> map, IStreamFilter filter, string abbreviation)
    {
        map[filter.Name] = filter;
        map[abbreviation] = filter;
    }
}

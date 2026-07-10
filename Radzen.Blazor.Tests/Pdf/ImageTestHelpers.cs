#nullable enable
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Blazor.Pdf.Tests;

internal static class ImageTestHelpers
{
    public static string Name(DictionaryObject dict, string key) => ((NameObject)dict[key]).Value;

    public static int Int(DictionaryObject dict, string key) => ((NumberObject)dict[key]).IntValue;
}

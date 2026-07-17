using System.Diagnostics.CodeAnalysis;

namespace Radzen.Documents.Pdf.Objects;

internal static class CosAccessors
{
    public static DictionaryObject? AsDictionary(this DocumentReader reader, DocumentObject value)
        => reader.Resolve(value) is DictionaryObject result ? result : null;

    public static ArrayObject? AsArray(this DocumentReader reader, DocumentObject value)
        => reader.Resolve(value) is ArrayObject result ? result : null;

    public static StreamObject? AsStream(this DocumentReader reader, DocumentObject value)
        => reader.Resolve(value) is StreamObject result ? result : null;

    public static string? AsName(this DocumentReader reader, DocumentObject value)
        => reader.Resolve(value) is NameObject name ? name.Value : null;

    public static string? AsString(this DocumentReader reader, DocumentObject value)
        => reader.Resolve(value) is StringObject text ? text.Value : null;

    public static int? AsInt(this DocumentReader reader, DocumentObject value)
        => reader.Resolve(value) is NumberObject number ? number.IntValue : null;

    public static double? AsNumber(this DocumentReader reader, DocumentObject value)
        => reader.Resolve(value) is NumberObject number ? number.DoubleValue : null;

    public static bool? AsBool(this DocumentReader reader, DocumentObject value)
        => reader.Resolve(value) is BooleanObject flag ? flag.Value : null;

    public static DictionaryObject? GetDictionary(this DocumentReader reader, DictionaryObject dict, string key)
        => dict.TryGetValue(key, out var value) ? reader.AsDictionary(value!) : null;

    public static ArrayObject? GetArray(this DocumentReader reader, DictionaryObject dict, string key)
        => dict.TryGetValue(key, out var value) ? reader.AsArray(value!) : null;

    public static StreamObject? GetStream(this DocumentReader reader, DictionaryObject dict, string key)
        => dict.TryGetValue(key, out var value) ? reader.AsStream(value!) : null;

    public static string? GetName(this DocumentReader reader, DictionaryObject dict, string key)
        => dict.TryGetValue(key, out var value) ? reader.AsName(value!) : null;

    public static string? GetString(this DocumentReader reader, DictionaryObject dict, string key)
        => dict.TryGetValue(key, out var value) ? reader.AsString(value!) : null;

    public static int? GetInt(this DocumentReader reader, DictionaryObject dict, string key)
        => dict.TryGetValue(key, out var value) ? reader.AsInt(value!) : null;

    public static double? GetNumber(this DocumentReader reader, DictionaryObject dict, string key)
        => dict.TryGetValue(key, out var value) ? reader.AsNumber(value!) : null;

    public static bool? GetBool(this DocumentReader reader, DictionaryObject dict, string key)
        => dict.TryGetValue(key, out var value) ? reader.AsBool(value!) : null;

    public static bool TryGet<T>(this DocumentReader reader, DictionaryObject dict, string key, [NotNullWhen(true)] out T? result)
        where T : DocumentObject
    {
        if (dict.TryGetValue(key, out var value) && reader.Resolve(value!) is T typed)
        {
            result = typed;
            return true;
        }

        result = null;
        return false;
    }
}

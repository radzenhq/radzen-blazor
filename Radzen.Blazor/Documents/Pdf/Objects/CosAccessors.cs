using System.Diagnostics.CodeAnalysis;

namespace Radzen.Documents.Pdf.Objects;

// Typed reads over the untyped COS object model. Each accessor resolves an
// indirect reference through the reader and then type-checks the result,
// centralizing the "resolve then pattern-match/cast" grammar that the read path
// (DocumentLoader, GraphImporter, AcroForm, the signing and emit writers) would
// otherwise hand-roll at every call site. A value of the wrong type - or a
// missing/free entry that resolves to null - yields null (or false for TryGet),
// exactly as the inline checks it replaces did.
//
// Two layers: the As* helpers resolve and cast an arbitrary value (an array
// element, a /Kids entry); the Get* helpers look a key up in a dictionary and
// then apply the same As* check. Get* passes the raw entry to Resolve unguarded
// (value!) so a null entry throws exactly as the original reader.Resolve(dict[key]!)
// did - dictionaries never store a CLR-null value, so this branch is unreachable
// in practice.
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

    // Presence-and-type test binding the resolved, type-checked object: true only
    // when the key is present and resolves to a T. Mirrors the inline
    // "dict.TryGetValue(key, out var v) && reader.Resolve(v!) is T typed" idiom.
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

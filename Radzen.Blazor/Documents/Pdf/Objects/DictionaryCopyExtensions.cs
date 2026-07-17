namespace Radzen.Documents.Pdf.Objects;

internal static class DictionaryCopyExtensions
{
    // Shallow copy preserving key order, so a merged dictionary can be rebuilt without
    // mutating the object read from the input. Omitting a key is the same job with a
    // filter, not a second copier.
    internal static DictionaryObject Copy(this DictionaryObject source, string? omittedKey = null)
    {
        var copy = new DictionaryObject();
        foreach (var pair in source)
        {
            if (omittedKey is null || pair.Key != omittedKey)
            {
                copy[pair.Key] = pair.Value;
            }
        }

        return copy;
    }
}

namespace Radzen.Documents.Pdf.Objects;

internal static class DictionaryCopyExtensions
{
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

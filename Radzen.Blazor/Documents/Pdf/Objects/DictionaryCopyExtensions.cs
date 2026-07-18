namespace Radzen.Documents.Pdf.Objects;

internal static class DictionaryCopyExtensions
{
    internal static DictionaryObject Copy(this DictionaryObject source, string? omittedKey = null)
        => source.Copy(new DictionaryObject(), omittedKey);

    internal static DictionaryObject Copy(this DictionaryObject source, DictionaryObject destination, string? omittedKey = null)
    {
        foreach (var pair in source)
        {
            if (omittedKey is null || pair.Key != omittedKey)
            {
                destination[pair.Key] = pair.Value;
            }
        }

        return destination;
    }
}

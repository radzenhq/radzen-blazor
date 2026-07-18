using System;

namespace Radzen.Documents.Pdf.Objects;

internal static class CosGraphRewriter
{
    public static DocumentObject Rewrite(DocumentObject value, Func<DocumentObject, DocumentObject?> transform)
    {
        if (transform(value) is { } replaced)
        {
            return replaced;
        }

        switch (value)
        {
            case DictionaryObject dictionary:
                var mappedDictionary = new DictionaryObject();
                foreach (var key in dictionary.Keys)
                {
                    mappedDictionary[key] = Rewrite(dictionary[key], transform);
                }

                return mappedDictionary;
            case ArrayObject array:
                var mappedArray = new ArrayObject();
                foreach (var item in array)
                {
                    mappedArray.Add(Rewrite(item, transform));
                }

                return mappedArray;
            default:
                return value;
        }
    }
}

namespace Radzen.Documents.Pdf.Objects.Filters;

internal interface IStreamFilter
{
    string Name { get; }

    byte[] Decode(byte[] data, DictionaryObject? parms, long maxOutput);
}

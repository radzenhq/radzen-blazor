using Radzen.Documents.Pdf.Write;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf;

internal static class FieldBakePolicy
{
    internal static bool CanBakeSingleLine(DocumentReader reader, DictionaryObject field, string value)
    {
        if (!FieldAppearances.CanEncode(value))
        {
            return false;
        }

        var flags = Inherited(reader, field, "Ff") is NumberObject ff ? ff.IntValue : 0;
        if ((flags & (FieldFlags.Multiline | FieldFlags.Password | FieldFlags.Comb)) != 0)
        {
            return false;
        }

        return Inherited(reader, field, "Q") is not NumberObject quad || quad.IntValue == 0;
    }

    // ISO 32000-1 12.7.4.4 stores /V as an array only for a multi-selection list box.
    internal static bool HasSingleSelection(DocumentReader reader, DictionaryObject field)
        => Inherited(reader, field, "V") is not ArrayObject;

    private static DocumentObject? Inherited(DocumentReader reader, DictionaryObject field, string key)
        => FormField.InheritedAttribute(reader, field, key);
}

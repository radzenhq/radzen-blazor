using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf;

internal static class InheritedDefaultAppearance
{
    internal static string? Resolve(DocumentReader reader, DictionaryObject widget, DictionaryObject? acroForm)
        => (FormField.InheritedAttribute(reader, widget, "DA") as StringObject)?.Value
            ?? (acroForm is not null ? reader.GetString(acroForm, "DA") : null);
}

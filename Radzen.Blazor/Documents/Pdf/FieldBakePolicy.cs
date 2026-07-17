using Radzen.Documents.Pdf.Emit;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf;

/// <summary>
/// The single answer to "can this field's value be reproduced as one left-aligned WinAnsi
/// line?" - the only shape both the /AP baker and the flattener know how to draw. AcroForm
/// answers no by deferring to the viewer via /NeedAppearances; the flattener has no viewer
/// to defer to, so it refuses instead of painting a wrong line.
/// </summary>
internal static class FieldBakePolicy
{
    /// <summary>
    /// False when the baked line would be wrong: a non-WinAnsi glyph, a multiline, comb or
    /// password layout, or a centered/right /Q.
    /// </summary>
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

    /// <summary>
    /// False when a choice field holds a multi-selection. ISO 32000-1 12.7.4.4 stores /V as an
    /// array only for a list box that can select several /Opt entries, which renders as those
    /// entries stacked and highlighted - never as one line. Joining them into "Red, Blue"
    /// invents a string the field never shows and silently drops the unselected options.
    /// </summary>
    internal static bool HasSingleSelection(DocumentReader reader, DictionaryObject field)
        => Inherited(reader, field, "V") is not ArrayObject;

    private static DocumentObject? Inherited(DocumentReader reader, DictionaryObject field, string key)
        => FormField.InheritedAttribute(reader, field, key);
}

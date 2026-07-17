namespace Radzen.Documents.Pdf;

/// <summary>
/// The interactive form field flags of an /Ff entry (ISO 32000-1 tables 226, 228 and 230).
/// Bit numbers in the spec are 1-based; the shifts here are 0-based.
/// </summary>
internal static class FieldFlags
{
    /// <summary>Button field, spec bit 16: the field is a set of radio buttons.</summary>
    internal const int Radio = 1 << 15;

    /// <summary>Button field, spec bit 17: the field is a pushbutton with no value.</summary>
    internal const int PushButton = 1 << 16;

    /// <summary>Choice field, spec bit 18: the field is a combo box rather than a list box.</summary>
    internal const int Combo = 1 << 17;

    /// <summary>Text field, spec bit 13: the field may contain multiple lines.</summary>
    internal const int Multiline = 1 << 12;

    /// <summary>Text field, spec bit 14: the field's value is echoed as asterisks.</summary>
    internal const int Password = 1 << 13;

    /// <summary>Text field, spec bit 25: the field is laid out as evenly spaced comb cells.</summary>
    internal const int Comb = 1 << 24;
}

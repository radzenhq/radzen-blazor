namespace Radzen.Documents.Pdf.Write;

// ISO 19005-2 6.9: the interactive form dictionary's NeedAppearances key shall either be absent or
// false, and 6.3.3 requires every annotation to carry an appearance stream; ISO 14289-1 7.18.1 and
// 7.18.5 hold a widget annotation and its form field to the same requirement. An /AP whose /N is the
// null object carries no value at all (ISO 32000-1 7.3.9), so it counts as absent.
internal static class FormAppearanceConformance
{
    private const string Remedy =
        "or save without a conformance claim (PdfAConformance.None and PdfUaConformance.None).";

    internal static string? Claim(PortableDocument document)
        => document.Conformance != PdfAConformance.None ? "PDF/A"
            : document.IsPdfUa ? "PDF/UA"
            : null;

    internal static string NeedAppearancesForbidden(string label)
        => $"{label} forbids the interactive form flag that leaves field appearances to the viewer "
            + "(/AcroForm /NeedAppearances), but it is set on this document; fill every field with a value "
            + "that renders as a single left-aligned line so its appearance can be baked, flatten the form "
            + $"with PortableDocument.Flatten, {Remedy}";

    internal static string MissingAppearance(string label, string field)
        => $"{label} requires every widget annotation to carry an appearance stream, but the form field "
            + $"'{field}' has no /AP /N appearance and its value would be left to the viewer to draw; fill "
            + "the field with a value that renders as a single left-aligned line so its appearance can be "
            + $"baked, flatten the form with PortableDocument.Flatten, {Remedy}";

    internal static string UnbakeableValue(string label, string field)
        => $"{label} requires every widget annotation to carry an appearance stream and forbids /AcroForm "
            + $"/NeedAppearances, but the value given for the field '{field}' does not render as a single "
            + "left-aligned line, so it cannot be baked into an appearance stream and the field would be "
            + "left to the viewer to draw; fill the field with WinAnsi text on a field that is not "
            + $"multiline, comb or password and is left aligned, {Remedy}";

    internal static string AppendedNeedAppearances(string label)
        => $"{label} forbids the interactive form flag that leaves field appearances to the viewer "
            + "(/AcroForm /NeedAppearances), but the appended document's form sets it and appending would "
            + "carry the flag onto this document; flatten the appended document with "
            + $"PortableDocument.Flatten before appending it, {Remedy}";
}

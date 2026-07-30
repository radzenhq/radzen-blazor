using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Write;

internal static class FormXObjectShell
{
    // ISO 32000-1 8.10.2 Table 95: /FormType default is 1, so it may be omitted.
    public static void ApplyHeader(DictionaryObject dict, DocumentObject bbox, bool formType)
    {
        dict["Type"] = new NameObject("XObject");
        dict["Subtype"] = new NameObject("Form");
        if (formType)
        {
            dict["FormType"] = new NumberObject(1);
        }

        dict["BBox"] = bbox;
    }
}

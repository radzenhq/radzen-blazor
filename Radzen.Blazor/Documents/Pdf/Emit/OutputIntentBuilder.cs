using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Emit;

internal static class OutputIntentBuilder
{
    public static DictionaryObject BuildSrgb(string outputConditionIdentifier)
    {
        var profile = new StreamObject(SrgbIccProfile.GetBytes());
        profile.Dictionary["N"] = new NumberObject(3);

        return new DictionaryObject
        {
            ["Type"] = new NameObject("OutputIntent"),
            ["S"] = new NameObject("GTS_PDFA1"),
            ["OutputConditionIdentifier"] = new StringObject(outputConditionIdentifier),
            ["DestOutputProfile"] = profile,
        };
    }
}

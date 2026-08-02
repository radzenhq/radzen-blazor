namespace Radzen.Documents.Layout;

internal readonly record struct FormInputExtent(double Width, double Height, double Ascent);

internal static class FormInputMetrics
{
    public static FormInputExtent Measure(FormInput input, double fontSize) => input switch
    {
        TextInput text => Box(text.Width?.Point ?? fontSize * 10, fontSize),
        DropDown drop => Box(drop.Width?.Point ?? fontSize * 10, fontSize),
        _ => new FormInputExtent(fontSize, fontSize, fontSize * 0.8),
    };

    private static FormInputExtent Box(double width, double fontSize)
        => new(width, fontSize * 1.4, fontSize * 1.1);
}

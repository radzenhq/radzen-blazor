namespace Radzen.Blazor.Spreadsheet;

#nullable enable

internal sealed class TextMeasureItem
{
    public string Text { get; set; } = "";
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public double? FontSize { get; set; }
    public string? FontFamily { get; set; }
}

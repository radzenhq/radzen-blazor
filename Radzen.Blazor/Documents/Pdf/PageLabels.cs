using Radzen.Documents.Pdf.Objects;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;


/// <summary>
/// The numbering style of a <see cref="PageLabel"/> range (ISO 32000-1 Table 159,
/// the <c>/S</c> entry of a page label dictionary).
/// </summary>
public enum PageLabelStyle
{
    /// <summary>Decimal arabic numerals (1, 2, 3, ...), written as <c>/D</c>.</summary>
    Decimal,
    /// <summary>Uppercase roman numerals (I, II, III, ...), written as <c>/R</c>.</summary>
    UppercaseRoman,
    /// <summary>Lowercase roman numerals (i, ii, iii, ...), written as <c>/r</c>.</summary>
    LowercaseRoman,
    /// <summary>Uppercase letters (A, B, ..., AA, ...), written as <c>/A</c>.</summary>
    UppercaseLetters,
    /// <summary>Lowercase letters (a, b, ..., aa, ...), written as <c>/a</c>.</summary>
    LowercaseLetters,
}

/// <summary>
/// A page-label range: from <see cref="StartPage"/> onward (until the next range),
/// pages are labelled with the given <see cref="Style"/>, optional <see cref="Prefix"/>
/// and starting ordinal <see cref="Start"/>. Add ranges to
/// <see cref="Document.PageLabels"/> to control the page numbers a viewer shows.
/// </summary>
/// <param name="startPage">The zero-based index of the first page in the range.</param>
public sealed class PageLabel(int startPage)
{
    /// <summary>Gets the zero-based index of the first page this range labels.</summary>
    public int StartPage { get; } = startPage;

    /// <summary>
    /// Gets or sets the numbering style. When <see langword="null"/> the pages carry only
    /// the <see cref="Prefix"/> (no numeric portion).
    /// </summary>
    public PageLabelStyle? Style { get; set; }

    /// <summary>Gets or sets the label prefix (the <c>/P</c> entry). When <see langword="null"/> no prefix is written.</summary>
    public string? Prefix { get; set; }

    /// <summary>Gets or sets the ordinal of the first page in the range (the <c>/St</c> entry). Defaults to 1.</summary>
    public int Start { get; set; } = 1;
}

// Serializes Document.PageLabels as the catalog /PageLabels number tree (ISO 32000-1
// 12.4.2): a /Nums array pairing each range's start page index with its label dict.
internal static class PageLabelsWriter
{
    private static string StyleName(PageLabelStyle style) => style switch
    {
        PageLabelStyle.Decimal => "D",
        PageLabelStyle.UppercaseRoman => "R",
        PageLabelStyle.LowercaseRoman => "r",
        PageLabelStyle.UppercaseLetters => "A",
        _ => "a",
    };

    public static DictionaryObject Build(IReadOnlyList<PageLabel> labels)
    {
        var sorted = new List<PageLabel>(labels);
        sorted.Sort((a, b) => a.StartPage.CompareTo(b.StartPage));

        var nums = new ArrayObject();
        foreach (var label in sorted)
        {
            var dictionary = new DictionaryObject();
            if (label.Style is { } style)
            {
                dictionary["S"] = new NameObject(StyleName(style));
            }

            if (label.Prefix is { } prefix)
            {
                dictionary["P"] = new StringObject(prefix);
            }

            if (label.Start != 1)
            {
                dictionary["St"] = new NumberObject(label.Start);
            }

            nums.Add(new NumberObject(label.StartPage));
            nums.Add(dictionary);
        }

        return new DictionaryObject { ["Nums"] = nums };
    }
}

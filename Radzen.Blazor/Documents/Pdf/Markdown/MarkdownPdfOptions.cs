using System;

namespace Radzen.Documents.Pdf.Markdown;

/// <summary>
/// Options controlling how <see cref="MarkdownPdf"/> maps Markdown content onto PDF blocks.
/// </summary>
public sealed class MarkdownPdfOptions
{
    /// <summary>Gets or sets the font family used for body text. Defaults to "Helvetica".</summary>
    public string BodyFontName { get; set; } = "Helvetica";

    /// <summary>Gets or sets the font family used for headings. Falls back to <see cref="BodyFontName"/> when <see langword="null"/>.</summary>
    public string? HeadingFontName { get; set; }

    /// <summary>Gets or sets the font family used for inline code and code blocks. Falls back to <see cref="BodyFontName"/> when <see langword="null"/>.</summary>
    public string? MonospaceFontName { get; set; }

    /// <summary>
    /// Gets or sets the font sizes for heading levels 1 through 6, indexed 0-based (index 0 is level 1).
    /// Must contain exactly 6 entries. Defaults to a decreasing scale from 24pt down to 11pt.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> does not contain exactly 6 entries.</exception>
    public double[] HeadingFontSizes
    {
        get => headingFontSizes;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.Length != HeadingLevels)
            {
                throw new ArgumentException(
                    $"{nameof(HeadingFontSizes)} must contain exactly {HeadingLevels} entries, but had {value.Length}.",
                    nameof(value));
            }

            headingFontSizes = value;
        }
    }

    private const int HeadingLevels = 6;

    private double[] headingFontSizes = [24, 20, 16, 14, 12, 11];

    /// <summary>Gets or sets the indent applied per block quote nesting level, in points. Defaults to 24pt.</summary>
    public double BlockQuoteIndent { get; set; } = 24;

    /// <summary>
    /// Gets or sets a delegate that resolves a Markdown image "src" (the link destination) to the raw
    /// image bytes. Returning <see langword="null"/> (or leaving this unset) skips the image without
    /// throwing.
    /// </summary>
    public Func<string, byte[]?>? ImageResolver { get; set; }

    internal string ResolvedHeadingFontName => HeadingFontName ?? BodyFontName;

    internal string ResolvedMonospaceFontName => MonospaceFontName ?? BodyFontName;

    internal double HeadingFontSize(int level)
    {
        var index = Math.Clamp(level - 1, 0, HeadingFontSizes.Length - 1);
        return HeadingFontSizes[index];
    }
}

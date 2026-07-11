using System.Linq;

namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// A single item of a <see cref="List"/>, holding inline content that flows after the item marker.
/// </summary>
public sealed class ListItem
{
    /// <summary>Gets the inline content runs.</summary>
    public InlineCollection Inlines { get; } = [];

    /// <summary>Gets the item font. Item runs inherit any property they leave unset from this font, then the list font.</summary>
    public Font Font { get; } = new();

    /// <summary>
    /// Gets or sets the item text. Getting returns the concatenation of the run texts, or
    /// <see langword="null"/> when there are no runs. Setting <see langword="null"/> clears the runs;
    /// setting a value replaces all runs with a single run holding that text.
    /// </summary>
    public string? Text
    {
        get => Inlines.Count == 0 ? null : string.Concat(Inlines.Select(run => run.Text));
        set
        {
            Inlines.Clear();
            if (value != null)
            {
                Inlines.Add(value);
            }
        }
    }
}

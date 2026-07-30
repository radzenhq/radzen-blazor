namespace Radzen.Documents;


/// <summary>
/// A contiguous run of text sharing the same font. Create runs through
/// <see cref="InlineCollection.Add(string)"/>.
/// </summary>
public sealed class Run : TextInline
{
    internal Run(string text) => Text = text;

    /// <summary>Gets or sets the run text.</summary>
    public string Text { get; set; }

    internal override string LayoutText => Text;
}

namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// The repeating content of a page header or footer.
/// </summary>
public class HeaderFooter
{
    /// <summary>Gets the content blocks of the header or footer.</summary>
    public BlockCollection Blocks { get; } = [];
}

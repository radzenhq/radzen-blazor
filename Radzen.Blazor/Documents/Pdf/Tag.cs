namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// A structure tag associating a content element with a logical role
/// (for example <c>P</c>, <c>H1</c> or <c>Figure</c>).
/// </summary>
public sealed class Tag
{
    /// <summary>Gets or sets the structure role name.</summary>
    public string Role { get; set; } = "";
}

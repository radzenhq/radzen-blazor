namespace Radzen.Blazor;

/// <summary>
/// Represents a cell in <see cref="RadzenTable"/>
/// </summary>
public partial class RadzenTableCell : RadzenComponentWithChildren
{
    /// <inheritdoc />
    protected override string GetComponentCssClass() => "rz-data-cell";
}
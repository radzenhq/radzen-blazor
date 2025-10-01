using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor;

/// <summary>
/// RadzenFabMenuItem component.
/// </summary>
/// <example>
/// <code>
/// &lt;RadzenFabMenuItem Text="Folder" Icon="folder" Click=@(args => Console.WriteLine("Item clicked")) /&gt;
/// </code>
/// </example>
public partial class RadzenFabMenuItem : RadzenButton
{
    /// <inheritdoc />
    [Parameter]
    public override Variant Variant { get; set; } = Variant.Flat;

    /// <inheritdoc />
    [Parameter]
    public override Shade Shade { get; set; } = Shade.Light;

    /// <inheritdoc />
    [Parameter]
    public override ButtonSize Size { get; set; } = ButtonSize.Large;
}
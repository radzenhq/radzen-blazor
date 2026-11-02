using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

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

    /// <summary>
    /// Gets or sets the tab index. Menu items are reached with the arrow keys of the <see cref="RadzenFabMenu"/> rather than with Tab, so the default is <c>-1</c>.
    /// </summary>
    [Parameter]
    public override int TabIndex { get; set; } = -1;

    /// <summary>
    /// Renders the item as a <c>menuitem</c> of the <see cref="RadzenFabMenu"/> menu unless the consumer supplied a <c>role</c> attribute.
    /// </summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Attributes == null || !Attributes.ContainsKey("role"))
        {
            var attributes = Attributes != null ? new Dictionary<string, object>(Attributes) : new Dictionary<string, object>();

            attributes["role"] = "menuitem";

            Attributes = attributes;
        }
    }
}

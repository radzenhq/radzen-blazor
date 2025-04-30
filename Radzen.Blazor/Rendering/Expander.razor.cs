using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen.Blazor.Rendering;

/// <summary>
/// Expandable content.
/// </summary>
public partial class Expander : ComponentBase
{
    /// <summary>
    /// Gets or sets the child content.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the additional attributes.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object> Attributes { get; set; }

    /// <summary>
    /// Gets or sets the CSS class.
    /// </summary>

    [Parameter]
    public string CssClass { get; set; }

    /// <summary>
    /// Determines whether the content is visible.
    /// </summary>
    [Parameter]
    public bool Expanded { get; set; }

    bool expanded;

    string Class => ClassList.Create("rz-expander")
        .Add($"rz-state-expanded", expanded)
        .Add($"rz-state-collapsed", !expanded)
        .Add(CssClass)
        .ToString();

    string Hidden => expanded ? "false" : "true";

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var expandedChanged = parameters.DidParameterChange(nameof(Expanded), Expanded);

        await base.SetParametersAsync(parameters);

        if (expandedChanged)
        {
            expanded = Expanded;
        }
    }

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        expanded = Expanded;
    }
}
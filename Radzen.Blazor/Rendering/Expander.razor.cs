using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen.Blazor.Rendering;

/// <summary>
/// Adds support for the ontransitionend event.
/// </summary>
[EventHandler("ontransitionend", typeof(EventArgs), true, false)]
public static class EventHandlers;

/// <summary>
/// Expandable content.
/// </summary>
public partial class Expander : ComponentBase
{
    enum ExpandState
    {
        Expanding,
        Expanded,
        Collapsing,
        Collapsed
    }

    private ExpandState state = ExpandState.Collapsed;

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

    string Class => ClassList.Create("rz-expander")
        .Add($"rz-state-{state.ToString().ToLowerInvariant()}")
        .Add(CssClass)
        .ToString();

    string Hidden => state == ExpandState.Collapsed ? "true" : "false";

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var expandedChanged = parameters.DidParameterChange(nameof(Expanded), Expanded);

        await base.SetParametersAsync(parameters);

        if (expandedChanged)
        {
            if (Expanded)
            {
                state = ExpandState.Expanding;
            }
            else
            {
                state = ExpandState.Collapsing;
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        if (Expanded)
        {
            state = ExpandState.Expanded;
        }
    }

    private void OnTransitionEnd()
    {
        if (state == ExpandState.Expanding)
        {
            state = ExpandState.Expanded;
        }
        else if (state == ExpandState.Collapsing)
        {
            state = ExpandState.Collapsed;
        }

        StateHasChanged();
    }
}
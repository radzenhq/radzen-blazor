using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Radzen.Blazor.Markdown;

#nullable enable

/// <summary>
/// Component for rendering anchor links in markdown.
/// </summary>
internal class RadzenAnchor : ComponentBase
{
    /// <summary>
    /// Gets or sets the JavaScript runtime.
    /// </summary>
    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    /// <summary>
    /// Gets or sets the additional attributes for the anchor element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? Attributes { get; set; }

    /// <summary>
    /// Gets or sets the child content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(1, "a");
        builder.AddMultipleAttributes(2, Attributes);
        builder.AddEventPreventDefaultAttribute(3, "onclick", true);
        builder.AddAttribute(4, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, OnClick));
        builder.AddContent(5, ChildContent);
        builder.CloseElement();
    }

    private async Task OnClick()
    {
        if (Attributes?.TryGetValue("href", out var href) == true)
        {
            await JSRuntime.InvokeVoidAsync("eval", $"document.querySelector('{href}').scrollIntoView()");
        }
    }
}


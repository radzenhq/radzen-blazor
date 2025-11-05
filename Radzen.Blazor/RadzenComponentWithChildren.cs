using Microsoft.AspNetCore.Components;
using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// A base class of components that have child content.
/// </summary>
public class RadzenComponentWithChildren : RadzenComponent
{
    /// <summary>
    /// Gets or sets the child content
    /// </summary>
    /// <value>The content of the child.</value>
    [Parameter]
    public RenderFragment ChildContent { get; set; }
}


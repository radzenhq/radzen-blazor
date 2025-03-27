using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
namespace Radzen.Blazor;

#nullable enable

/// <summary>
/// Represents a table of contents item.
/// </summary>
public partial class RadzenTocItem : ComponentBase, IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the child content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the text displayed in the table of contents.
    /// </summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the path to the anchor which the table of contents item links to.
    /// </summary>
    [Parameter]
    public string? Path { get; set; }

    [CascadingParameter]
    RadzenToc? Toc { get; set; }

    private bool active;

    private string Class => ClassList.Create("rz-toc-item")
        .Add("rz-toc-item-active", active)
        .ToString();
    
    internal void Activate()
    {
        active = true;
        StateHasChanged();
    }

    internal void Deactivate()
    {
        active = false;
        StateHasChanged();
    }

    private async Task OnClickAsync()
    {
        if (Toc != null)
        {
            await Toc.SelectItemAsync(this);
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (Toc != null)
        {
            await Toc.AddItemAsync(this);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Toc != null)
        {
            await Toc.RemoveItemAsync(this);
        }
    }
}
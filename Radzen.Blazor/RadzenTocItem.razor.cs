using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Threading.Tasks;
namespace Radzen.Blazor;

#nullable enable

/// <summary>
/// Represents a table of contents item.
/// </summary>
public partial class RadzenTocItem : RadzenComponentWithChildren, IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the text displayed in the table of contents.
    /// </summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the custom content of the item.
    /// </summary>
    [Parameter]
    public RenderFragment? Template { get; set; }

    /// <summary>
    /// Gets or sets the CSS selector of the element to scroll to.
    /// </summary>
    [Parameter]
    public string? Selector { get; set; }

    [CascadingParameter]
    RadzenToc? Toc { get; set; }

    [CascadingParameter(Name = nameof(Level))]
    int Level { get; set; }

    private bool selected;

    /// <inheritdoc />
    protected override string GetComponentCssClass()
    {
        return ClassList.Create("rz-toc-item")
        .Add("rz-toc-item-selected", selected)
        .ToString();
    }

    private string WrapperClass => ClassList.Create("rz-toc-item-wrapper")
        .Add(Level switch
        {
            0 => "rz-ps-0",
            1 => "rz-ps-2",
            2 => "rz-ps-4",
            _ => "rz-ps-6",
        })
        .ToString();

    private string LinkClass => ClassList.Create("rz-toc-link")
        .ToString();

    internal void Activate()
    {
        selected = true;
        StateHasChanged();
    }

    internal void Deactivate()
    {
        selected = false;
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
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
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
    [Inject]
    NavigationManager? NavigationManager { get; set; }
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
    private string? href;

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

    private string? CreateHref(string? uri) => !string.IsNullOrEmpty(uri) && !string.IsNullOrEmpty(Selector)
        ? new Uri(uri).PathAndQuery + Selector
        : Selector;

    internal void Activate()
    {
        if (!selected)
        {
            selected = true;
            StateHasChanged();
        }
    }

    internal void Deactivate()
    {
        if (selected)
        {
            selected = false;
            StateHasChanged();
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (NavigationManager != null)
        {
            NavigationManager.LocationChanged += OnLocationChanged;
        }

        if (Toc != null)
        {
            await Toc.AddItemAsync(this);
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        href = CreateHref(NavigationManager?.Uri);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        var newHref = CreateHref(args.Location);
        if (href == newHref)
        {
            return;
        }

        href = newHref;

        InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (NavigationManager != null)
        {
            NavigationManager.LocationChanged -= OnLocationChanged;
        }

        if (Toc != null)
        {
            await Toc.RemoveItemAsync(this);
        }
    }
}
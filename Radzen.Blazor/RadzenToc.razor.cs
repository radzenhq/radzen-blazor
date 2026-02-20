using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor;

#nullable enable
/// <summary>
/// Displays a table of contents for a page.
/// </summary>
public partial class RadzenToc : RadzenComponent, IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the child content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the orientation of the table of contents.
    /// </summary>
    [Parameter]
    public Orientation Orientation { get; set; } = Orientation.Vertical;

    /// <summary>
    /// Gets or sets the CSS selector of the element to monitor for scroll events. By default the entire page is monitored.
    /// </summary>
    [Parameter]
    public string? Selector { get; set; }

    /// <inheritdoc />
    protected override string GetComponentCssClass() => ClassList.Create("rz-toc")
        .Add($"rz-toc-{Orientation.ToString().ToLowerInvariant()}")
        .ToString();

    void ActivateItem(string? path)
    {
        foreach (var item in items)
        {
            if (item.Selector == path)
            {
                item.Activate();
            }
            else
            {
                item.Deactivate();
            }
        }
    }

    /// <summary>
    /// Invoked when the current toc item changes.
    /// </summary>

    [JSInvokable]
    public void ScrollIntoView(string selector)
    {
        ActivateItem(selector);
    }

    private readonly List<RadzenTocItem> items = [];
    private IJSObjectReference? scrollListenerRef;

    internal async Task AddItemAsync(RadzenTocItem item)
    {
        items.Add(item);

        if (rendered && !disposed)
        {
            await RegisterScrollListenerAsync();
        }
    }

    internal async Task RemoveItemAsync(RadzenTocItem item)
    {
        items.Remove(item);

        if (rendered && !disposed)
        {
            await RegisterScrollListenerAsync();
        }
    }

    private bool rendered;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            rendered = true;

            await RegisterScrollListenerAsync();
        }
    }

    private async Task RegisterScrollListenerAsync()
    {
        if (IsJSRuntimeAvailable && JSRuntime != null)
        {
            try
            {
                await UnregisterScrollListenerAsync();
                scrollListenerRef = await JSRuntime.InvokeAsync<IJSObjectReference>("Radzen.createScrollListener", Reference, items.Select(items => items.Selector), Selector);
            } 
            catch (JSDisconnectedException)
            {
            }
        }
    }

    private async Task UnregisterScrollListenerAsync()
    {
        var listener = scrollListenerRef;
        scrollListenerRef = null;

        if (listener != null)
        {
            try
            {
                await listener.InvokeVoidAsync("dispose");
                await listener.DisposeAsync();
            } 
            catch (JSDisconnectedException)
            {
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await UnregisterScrollListenerAsync();
        Dispose();
    }
}
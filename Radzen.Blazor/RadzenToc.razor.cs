using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

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
    /// Gets or sets the CSS selector of the element to monitor for scroll events.
    /// </summary>
    [Parameter]
    public string Selector { get; set; } = ".rz-body";

    /// <inheritdoc />
    protected override string GetComponentCssClass() => "rz-toc";

    internal async Task SelectItemAsync(RadzenTocItem item)
    {
        await JSRuntime.InvokeVoidAsync("Radzen.navigateTo", item.Path, true);
    }

    void ActivateItem(string? path)
    {
        foreach (var item in items)
        {
            if (item.Path == path)
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

    internal async Task AddItemAsync(RadzenTocItem item)
    {
        items.Add(item);

        if (rendered)
        {
            await RegisterScrollListenerAsync();
        }
    }

    internal async Task RemoveItemAsync(RadzenTocItem item)
    {
        items.Remove(item);

        if (rendered)
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
        if (IsJSRuntimeAvailable)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("Radzen.registerScrollListener", Element, Reference, items.Select(items => items.Path), Selector);
            } 
            catch (JSDisconnectedException)
            {
            }
        }
    }

    private async Task UnregisterScrollListenerAsync()
    {
        if (IsJSRuntimeAvailable)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("Radzen.unregisterScrollListener", Element);
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
    }
}
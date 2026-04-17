using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// An accordion component that displays collapsible content panels with only one or multiple panels expanded at a time.
    /// RadzenAccordion organizes content into expandable sections, saving vertical space while keeping all content accessible.
    /// Ideal for FAQs, settings panels, grouped content, or any scenario where showing all content at once would be overwhelming.
    /// Features single/multiple expand control, optional icons in panel headers, programmatic control via SelectedIndex two-way binding,
    /// Expand and Collapse event callbacks, keyboard navigation (Arrow keys, Space/Enter, Home/End), and disabled item support.
    /// By default, only one panel can be expanded at a time (Multiple = false). Set Multiple = true to allow multiple panels to be expanded simultaneously.
    /// </summary>
    /// <example>
    /// Basic accordion (single expand mode):
    /// <code>
    /// &lt;RadzenAccordion&gt;
    ///     &lt;Items&gt;
    ///         &lt;RadzenAccordionItem Text="Personal Information" Icon="person"&gt;
    ///             Name, email, address fields...
    ///         &lt;/RadzenAccordionItem&gt;
    ///         &lt;RadzenAccordionItem Text="Account Settings" Icon="settings"&gt;
    ///             Password, preferences, notifications...
    ///         &lt;/RadzenAccordionItem&gt;
    ///         &lt;RadzenAccordionItem Text="Billing" Icon="payment"&gt;
    ///             Payment methods, invoices...
    ///         &lt;/RadzenAccordionItem&gt;
    ///     &lt;/Items&gt;
    /// &lt;/RadzenAccordion&gt;
    /// </code>
    /// Accordion with multiple expand and events:
    /// <code>
    /// &lt;RadzenAccordion Multiple="true" Expand=@OnExpand Collapse=@OnCollapse&gt;
    ///     &lt;Items&gt;
    ///         &lt;RadzenAccordionItem Text="FAQ 1" Selected="true"&gt;Answer 1&lt;/RadzenAccordionItem&gt;
    ///         &lt;RadzenAccordionItem Text="FAQ 2"&gt;Answer 2&lt;/RadzenAccordionItem&gt;
    ///     &lt;/Items&gt;
    /// &lt;/RadzenAccordion&gt;
    /// </code>
    /// </example>
    public partial class RadzenAccordion : RadzenComponent
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-accordion";
        }

        /// <summary>
        /// Gets or sets whether multiple accordion items can be expanded simultaneously.
        /// When false (default), expanding one item automatically collapses others.
        /// When true, users can expand multiple items independently.
        /// </summary>
        /// <value><c>true</c> to allow multiple items expanded; <c>false</c> for single-item expansion. Default is <c>false</c>.</value>
        [Parameter]
        public bool Multiple { get; set; }

        /// <summary>
        /// Gets or sets the render mode of the accordion.
        /// When set to <see cref="AccordionRenderMode.Server"/> (default), the component re-renders on every expand/collapse.
        /// When set to <see cref="AccordionRenderMode.Client"/>, all items are rendered and expand/collapse is handled with JavaScript.
        /// </summary>
        /// <value>The render mode. Default is <see cref="AccordionRenderMode.Server"/>.</value>
        [Parameter]
        public AccordionRenderMode RenderMode { get; set; } = AccordionRenderMode.Server;

        /// <summary>
        /// Gets or sets the zero-based index of the currently expanded item.
        /// Use with @bind-SelectedIndex for two-way binding to programmatically control which item is expanded.
        /// In multiple expand mode, this represents the last expanded item.
        /// </summary>
        /// <value>The selected item index. Default is -1 (no selection).</value>
        [Parameter]
        public int SelectedIndex { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the selected index changes.
        /// Used for two-way binding with @bind-SelectedIndex.
        /// </summary>
        /// <value>The event callback receiving the new selected index.</value>
        [Parameter]
        public EventCallback<int> SelectedIndexChanged { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when an accordion item is expanded.
        /// Receives the index of the expanded item as a parameter.
        /// </summary>
        /// <value>The expand event callback.</value>
        [Parameter]
        public EventCallback<int> Expand { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when an accordion item is collapsed.
        /// Receives the index of the collapsed item as a parameter.
        /// </summary>
        /// <value>The collapse event callback.</value>
        [Parameter]
        public EventCallback<int> Collapse { get; set; }

        /// <summary>
        /// Gets or sets the render fragment containing RadzenAccordionItem components that define the accordion panels.
        /// Each RadzenAccordionItem represents one expandable panel with its header and content.
        /// </summary>
        /// <value>The items render fragment containing accordion item definitions.</value>
        [Parameter]
        public RenderFragment? Items { get; set; }

        List<RadzenAccordionItem> items = new List<RadzenAccordionItem>();

        /// <summary>
        /// Gets the collection of <see cref="RadzenAccordionItem" /> components that belong to this accordion.
        /// </summary>
        public IReadOnlyList<RadzenAccordionItem> AccordionItems => items.AsReadOnly();

        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void AddItem(RadzenAccordionItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (items.IndexOf(item) == -1)
            {
                if (item.GetSelected())
                {
                    SelectedIndexChanged.InvokeAsync(items.Count);
                }

                items.Add(item);
                StateHasChanged();
            }
        }

        /// <summary>
        /// Removes the item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void RemoveItem(RadzenAccordionItem item)
        {
            if (items.Remove(item))
            {
                if (!disposed)
                {
                    try { InvokeAsync(StateHasChanged); } catch { }
                }
            }
        }

        string ToggleIconClass(RadzenAccordionItem item) => ClassList.Create("notranslate rz-accordion-toggle-icon rzi")
                                               .Add("rz-state-expanded", item.GetSelected())
                                               .Add("rz-state-collapsed", !item.GetSelected())
                                               .ToString();

        /// <summary>
        /// Refreshes this instance.
        /// </summary>
        public void Refresh()
        {
            StateHasChanged();
        }

        bool _itemRefreshPending;

        internal void ItemRefresh()
        {
            if (!_itemRefreshPending)
            {
                _itemRefreshPending = true;
                StateHasChanged();
            }
        }

        /// <summary>
        /// Determines whether the specified index is selected.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if the specified index is selected; otherwise, <c>false</c>.</returns>
        protected bool IsSelected(int index, RadzenAccordionItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            return item.GetSelected() == true;
        }

        /// <summary>
        /// Gets the item's title attribute value.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <returns>The item's collapse or expand title value depending on if the item is expanded or collapsed.
        /// If the relevant title is null or whitespace this method returns "Expand" or "Collapse".</returns>
        protected string ItemTitle(int index, RadzenAccordionItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (IsSelected(index, item))
            {
                return string.IsNullOrWhiteSpace(item.CollapseTitle) ? "Collapse" : item.CollapseTitle;
            }
            return string.IsNullOrWhiteSpace(item.ExpandTitle) ? "Expand" : item.ExpandTitle;
        }

        /// <summary>
        /// Gets the item's aria-label attribute value.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <returns>The item's collapse or expand aria-label value depending on if the item is expanded or collapsed.
        /// If the relevant aria-label is null or whitespace this method returns "Expand" or "Collapse".</returns>
        protected string ItemAriaLabel(int index, RadzenAccordionItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (IsSelected(index, item))
            {
                return string.IsNullOrWhiteSpace(item.CollapseAriaLabel) ? "Collapse" : item.CollapseAriaLabel;
            }
            return string.IsNullOrWhiteSpace(item.ExpandAriaLabel) ? "Expand" : item.ExpandAriaLabel;          
        }
        
        internal async System.Threading.Tasks.Task SelectItem(RadzenAccordionItem item, bool? value = null)
        {
            if(item.Disabled) return;

            if (RenderMode == AccordionRenderMode.Client && accordionJs != null && value == null)
            {
                await SelectItemOnClient(item);
                return;
            }

            await CollapseAll(item);

            var itemIndex = items.IndexOf(item);

            var selected = item.GetSelected();

            if (selected)
            {
                await Collapse.InvokeAsync(itemIndex);
            }
            else
            {
                await Expand.InvokeAsync(itemIndex);
            }

            await item.SetSelected(value ?? !selected);

            if (!Multiple)
            {
                await SelectedIndexChanged.InvokeAsync(itemIndex);
            }

            StateHasChanged();
        }

        /// <summary>
        /// Expands all accordion items.
        /// </summary>
        public async Task ExpandAll()
        {
            var visibleItems = items.Where(i => i.Visible && !i.Disabled).ToList();

            foreach (var item in visibleItems)
            {
                if (!item.GetSelected())
                {
                    if (RenderMode == AccordionRenderMode.Client && accordionJs != null)
                    {
                        var visibleIndex = items.Where(i => i.Visible).ToList().IndexOf(item);
                        await accordionJs.InvokeVoidAsync("toggle", visibleIndex, true);
                    }

                    await item.SetSelected(true);
                    await Expand.InvokeAsync(items.IndexOf(item));
                }
            }

            if (RenderMode != AccordionRenderMode.Client)
            {
                StateHasChanged();
            }
        }

        /// <summary>
        /// Collapses all accordion items.
        /// </summary>
        public async Task CollapseAll()
        {
            var visibleItems = items.Where(i => i.Visible && !i.Disabled).ToList();

            foreach (var item in visibleItems)
            {
                if (item.GetSelected())
                {
                    if (RenderMode == AccordionRenderMode.Client && accordionJs != null)
                    {
                        var visibleIndex = items.Where(i => i.Visible).ToList().IndexOf(item);
                        await accordionJs.InvokeVoidAsync("toggle", visibleIndex, false);
                    }

                    await item.SetSelected(false);
                    await Collapse.InvokeAsync(items.IndexOf(item));
                }
            }

            if (RenderMode != AccordionRenderMode.Client)
            {
                StateHasChanged();
            }
        }

        async System.Threading.Tasks.Task CollapseAll(RadzenAccordionItem item)
        {
            if (!Multiple && items.Count > 1)
            {
                foreach (var i in items.Where(i => i != item))
                {
                    if (i.GetSelected())
                    {
                        await i.SetSelected(false);
                        await Collapse.InvokeAsync(items.IndexOf(i));
                    }
                }
            }
        }

        IJSObjectReference? accordionJs;
        bool shouldRender = true;
        bool renderModeNeedsInit;

        /// <inheritdoc />
        protected override bool ShouldRender()
        {
            return shouldRender;
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if ((firstRender || renderModeNeedsInit) && RenderMode == AccordionRenderMode.Client && JSRuntime != null)
            {
                renderModeNeedsInit = false;
                accordionJs = await JSRuntime.InvokeAsync<IJSObjectReference>(
                    "Radzen.createAccordion", Element, Multiple);
            }
        }

        internal async Task SelectItemOnClient(RadzenAccordionItem item)
        {
            if (item.Disabled || accordionJs == null) return;

            var visibleItems = items.Where(i => i.Visible).ToList();
            var visibleIndex = visibleItems.IndexOf(item);
            if (visibleIndex < 0) return;

            var expanded = !item.GetSelected();

            await accordionJs.InvokeVoidAsync("toggle", visibleIndex, expanded);

            shouldRender = false;

            if (!Multiple)
            {
                foreach (var i in items.Where(i => i != item && i.Visible && !i.Disabled))
                {
                    if (i.GetSelected())
                    {
                        await i.SetSelected(false);
                        await Collapse.InvokeAsync(items.IndexOf(i));
                    }
                }
            }

            var itemIndex = items.IndexOf(item);

            if (expanded)
            {
                await Expand.InvokeAsync(itemIndex);
            }
            else
            {
                await Collapse.InvokeAsync(itemIndex);
            }

            await item.SetSelected(expanded);

            if (!Multiple)
            {
                await SelectedIndexChanged.InvokeAsync(itemIndex);
            }

            shouldRender = true;
        }

        internal int focusedIndex = -1;
        bool preventKeyPress = true;

        bool stopKeydownPropagation = true;
        void OnGuardKeyDown(KeyboardEventArgs args)
        {
            var key = args.Code ?? args.Key;
            stopKeydownPropagation = key != "Escape";
        }
        async Task OnKeyPress(KeyboardEventArgs args)
        {
            var key = args.Code != null ? args.Code : args.Key;

            if (key == "ArrowUp" || key == "ArrowDown")
            {
                preventKeyPress = true;

                focusedIndex = Math.Clamp(focusedIndex + (key == "ArrowUp" ? -1 : 1), 0, items.Count - 1);
            }
            else if (key == "Space" || key == "Enter")
            {
                preventKeyPress = true;

                if (focusedIndex >= 0 && focusedIndex < items.Count)
                {
                    await SelectItem(items.Where(i => i.Visible).ElementAt(focusedIndex));
                }
            }
            else
            {
                preventKeyPress = false;
            }
        }

        internal bool IsFocused(RadzenAccordionItem item)
        {
            return items.Where(i => i.Visible).ToList().IndexOf(item) == focusedIndex && focusedIndex != -1;
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            _itemRefreshPending = false;

            if (parameters.DidParameterChange(nameof(Multiple), Multiple) && accordionJs != null)
            {
                await accordionJs.InvokeVoidAsync("setMultiple", parameters.GetValueOrDefault<bool>(nameof(Multiple)));
            }

            var renderModeChanged = parameters.DidParameterChange(nameof(RenderMode), RenderMode);
            if (renderModeChanged)
            {
                var newRenderMode = parameters.GetValueOrDefault<AccordionRenderMode>(nameof(RenderMode));

                if (newRenderMode == AccordionRenderMode.Server && accordionJs != null)
                {
                    try { await accordionJs.InvokeVoidAsync("dispose"); } catch { }
                    accordionJs = null;
                }

                renderModeNeedsInit = newRenderMode == AccordionRenderMode.Client && accordionJs == null;
            }

            if (parameters.DidParameterChange(nameof(SelectedIndex), SelectedIndex))
            {
                var item = items.Where(i => i.Visible).ElementAtOrDefault(parameters.GetValueOrDefault<int>(nameof(SelectedIndex)));
                if (item != null && !item.GetSelected())
                {
                    await SelectItem(item);
                }
            }

            await base.SetParametersAsync(parameters);
        }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            focusedIndex = focusedIndex == -1 ? 0 : focusedIndex;

            base.OnInitialized();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            if (accordionJs != null)
            {
                accordionJs.InvokeVoid("dispose");
                accordionJs.DisposeFireAndForget();
                accordionJs = null;
            }
        }
    }
}

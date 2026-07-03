using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenProfileMenu component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenProfileMenu&gt;
    ///     &lt;RadzenProfileMenuItem Text="Data"&gt;
    ///         &lt;RadzenProfileMenuItem Text="Orders" Path="orders" /&gt;
    ///         &lt;RadzenProfileMenuItem Text="Employees" Path="employees" /&gt;
    ///     &lt;/RadzenProfileMenuItemItem&gt;
    /// &lt;/RadzenProfileMenu&gt;
    /// </code>
    /// </example>
    public partial class RadzenProfileMenu : RadzenComponentWithChildren
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-menu rz-profile-menu";
        }

        IJSObjectReference? _jsRef;

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender && Visible && JSRuntime != null)
            {
                _jsRef = await JSRuntime.InvokeAsync<IJSObjectReference>(
                    "Radzen.createProfileMenu", Element);
            }

            if (shouldFocusMenu)
            {
                shouldFocusMenu = false;

                try
                {
                    await menuElement.FocusAsync(preventScroll: true);
                }
                catch (InvalidOperationException)
                {
                }
                catch (JSDisconnectedException)
                {
                }
                catch (JSException)
                {
                }
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();
            _jsRef?.InvokeVoidAsync("dispose");
            _jsRef?.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The template.</value>
        [Parameter]
        public RenderFragment? Template { get; set; }

        /// <summary>
        /// Gets or sets the click callback.
        /// </summary>
        /// <value>The click callback.</value>
        [Parameter]
        public EventCallback<RadzenProfileMenuItem> Click { get; set; }

        /// <summary>
        /// Show/Hide the "arrow down" icon
        /// </summary>
        /// <value>Show the "arrow down" icon.</value>
        [Parameter]
        public bool ShowIcon { get; set; } = true;

        private string? toggleAriaLabel;

        /// <summary>
        /// Gets or sets the toggle aria label text.
        /// </summary>
        /// <value>The toggle aria label text.</value>
        [Parameter]
        public string ToggleAriaLabel { get => toggleAriaLabel ?? Localize(nameof(RadzenStrings.ProfileMenu_ToggleAriaLabel)); set => toggleAriaLabel = value; }

        string contentStyle = "display:none;position:absolute;z-index:1;";

        /// <summary>
        /// Toggles the menu open/close state.
        /// </summary>
        /// <param name="args">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        public async Task Toggle(MouseEventArgs args)
        {
            contentStyle = Collapsed ?  "display:block;" : "display:none;position:absolute;z-index:1;";
            await InvokeAsync(StateHasChanged);
        }

        bool Collapsed => contentStyle.Contains("display:none;", StringComparison.CurrentCulture);

        string ToggleClass => ClassList.Create("notranslate rzi rz-navigation-item-icon-children")
                            .Add("rz-state-expanded", !Collapsed)
                            .Add("rz-state-collapsed", Collapsed)
                            .ToString();

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public void Close()
        {
            contentStyle = "display:none;";
            focusedIndex = -1;
            StateHasChanged();
        }

        ElementReference toggleElement;
        ElementReference menuElement;

        string? ActiveDescendantId => !Collapsed && focusedIndex >= 0 && focusedIndex < items.Count
            ? items[focusedIndex].GetItemId()
            : null;

        async Task RestoreFocusToToggle()
        {
            try
            {
                await toggleElement.FocusAsync();
            }
            catch (InvalidOperationException)
            {
            }
        }

        [Inject]
        NavigationManager? NavigationManager { get; set; }

        internal int focusedIndex = -1;

        bool shouldFocusMenu;

        bool preventKeyPress = true;
        bool stopKeydownPropagation;
        async Task OnKeyPress(KeyboardEventArgs args)
        {
            var key = args.Code != null ? args.Code : args.Key;

            if (key == "ArrowUp" || key == "ArrowDown")
            {
                preventKeyPress = true;
                stopKeydownPropagation = true;

                if (Collapsed)
                {
                    await Toggle(new MouseEventArgs());

                    focusedIndex = key == "ArrowUp" ? items.Count - 1 : 0;

                    shouldFocusMenu = true;
                }
                else if (items.Count > 0)
                {
                    var start = Math.Clamp(focusedIndex, 0, items.Count - 1);
                    focusedIndex = (start + (key == "ArrowUp" ? -1 : 1) + items.Count) % items.Count;
                }
            }
            else if (key == "Home" || key == "End")
            {
                preventKeyPress = true;
                stopKeydownPropagation = true;

                if (Collapsed)
                {
                    await Toggle(new MouseEventArgs());

                    shouldFocusMenu = true;
                }

                focusedIndex = key == "Home" ? 0 : items.Count - 1;
            }
            else if (key == "Space" || key == "Enter")
            {
                preventKeyPress = true;
                stopKeydownPropagation = true;

                if (!Collapsed && focusedIndex >= 0 && focusedIndex < items.Count)
                {
                    var item = items[focusedIndex];

                    if (item.Path != null)
                    {
                        NavigationManager?.NavigateTo(item.Path);
                    }
                    else
                    {
                        await item.OnClick(new MouseEventArgs());
                    }
                }
                else
                {
                    await Toggle(new MouseEventArgs());

                    if (!Collapsed)
                    {
                        focusedIndex = focusedIndex != -1 ? focusedIndex : 0;

                        shouldFocusMenu = true;
                    }
                }
            }
            else if (key == "Escape")
            {
                preventKeyPress = true;
                stopKeydownPropagation = true;

                Close();

                await RestoreFocusToToggle();
            }
            else if (key == "Tab")
            {
                preventKeyPress = false;
                stopKeydownPropagation = false;

                if (!Collapsed)
                {
                    Close();
                }
            }
            else if (!Collapsed && args.Key != null && args.Key.Length == 1 && !char.IsControl(args.Key[0]) && items.Count > 0)
            {
                preventKeyPress = true;
                stopKeydownPropagation = true;

                var search = args.Key;
                var start = focusedIndex < 0 ? 0 : focusedIndex;

                for (var offset = 1; offset <= items.Count; offset++)
                {
                    var index = (start + offset) % items.Count;
                    var text = items[index].Text;

                    if (text != null && text.StartsWith(search, StringComparison.OrdinalIgnoreCase))
                    {
                        focusedIndex = index;
                        break;
                    }
                }
            }
            else
            {
                preventKeyPress = false;
                stopKeydownPropagation = false;
            }
        }

        bool stopGuardKeydownPropagation = true;
        void OnGuardKeyDown(KeyboardEventArgs args)
        {
            var key = args.Code ?? args.Key;
            stopGuardKeydownPropagation = key != "Escape";
        }

        internal bool IsFocused(RadzenProfileMenuItem item)
        {
            return items.IndexOf(item) == focusedIndex && focusedIndex != -1;
        }

        internal List<RadzenProfileMenuItem> items = new List<RadzenProfileMenuItem>();
        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void AddItem(RadzenProfileMenuItem item)
        {
            if (items.IndexOf(item) == -1)
            {
                items.Add(item);
            }
        }
    }
}

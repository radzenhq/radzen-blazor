using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenPanelMenuItem component.
    /// </summary>
    public partial class RadzenPanelMenuItem : RadzenComponent
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass() => ClassList.Create("rz-navigation-item")
            .Add("rz-state-focused", Parent?.IsFocused(this) == true)
            .AddDisabled(Disabled)
            .ToString();

        /// <summary>
        /// Gets or sets the target.
        /// </summary>
        /// <value>The target.</value>
        [Parameter]
        public string Target { get; set; }

        /// <summary>
        /// Gets or sets the expanded changed callback.
        /// </summary>
        /// <value>The expanded changed callback.</value>
        [Parameter]
        public EventCallback<bool> ExpandedChanged { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public string ImageAlternateText { get; set; } = "image";

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        [Parameter]
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        [Parameter]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the navigation link match.
        /// </summary>
        /// <value>The navigation link match.</value>
        [Parameter]
        public NavLinkMatch Match { get; set; } = NavLinkMatch.Prefix;

        /// <summary>
        /// Gets or sets the icon.
        /// </summary>
        /// <value>The icon.</value>
        [Parameter]
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the icon color.
        /// </summary>
        /// <value>The icon color.</value>
        [Parameter]
        public string IconColor { get; set; }

        /// <summary>
        /// Gets or sets the image.
        /// </summary>
        /// <value>The image.</value>
        [Parameter]
        public string Image { get; set; }

        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The template.</value>
        [Parameter]
        public RenderFragment Template { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenPanelMenuItem"/> is expanded.
        /// </summary>
        /// <value><c>true</c> if expanded; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Expanded { get; set; }

        private bool expanded;

        internal bool IsExpanded => expanded;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenPanelMenuItem"/> is selected.
        /// </summary>
        /// <value><c>true</c> if selected; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Selected { get; set; }

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenPanelMenuItem"/> is disabled.
        /// </summary>
        /// <value><c>true</c> if disabled; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Disabled { get; set; }

        internal async Task Toggle()
        {
            if (!expanded && !Parent.Multiple)
            {
                var itemsToSkip = new List<RadzenPanelMenuItem>();

                var p = ParentItem;

                while (p != null)
                {
                    itemsToSkip.Add(p);

                    p = p.ParentItem;
                }

                await Parent.CollapseAllAsync(itemsToSkip);
            }

            expanded = !expanded;

            await ExpandedChanged.InvokeAsync(expanded);
        }

        internal async Task CollapseAsync()
        {
            if (expanded)
            {
                expanded = false;

                await ExpandedChanged.InvokeAsync(expanded);
            }
        }

        string ToggleClass => ClassList.Create("notranslate rzi rz-navigation-item-icon-children")
                            .Add("rz-state-expanded", expanded)
                            .Add("rz-state-collapsed", !expanded)
                            .ToString();

        string getIconStyle()
        {
            return $"{(Parent?.DisplayStyle == MenuItemDisplayStyle.Icon ? "margin-inline-end:0px;" : "")}{(!string.IsNullOrEmpty(IconColor) ? $"color:{IconColor}" : "")}";
        }

        void Expand()
        {
            expanded = true;
        }

        /// <summary>
        /// Gets or sets the click callback.
        /// </summary>
        /// <value>The click callback.</value>
        [Parameter]
        public EventCallback<MenuItemEventArgs> Click { get; set; }

        /// <summary>
        /// Gets or sets the parent item.
        /// </summary>
        /// <value>The parent item.</value>
        [CascadingParameter]
        public RadzenPanelMenuItem ParentItem { get; set;}

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        [CascadingParameter]
        public RadzenPanelMenu Parent { get; set; }

        internal List<RadzenPanelMenuItem> items = new List<RadzenPanelMenuItem>();

        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void AddItem(RadzenPanelMenuItem item)
        {
            if (!items.Contains(item))
            {
                items.Add(item);
            }
        }

        void EnsureVisible()
        {
            if (selected)
            {
                var parent = ParentItem;

                while (parent != null)
                {
                    parent.Expand();

                    if (parent.ParentItem == null)
                    {
                        parent.StateHasChanged();
                    }

                    parent = parent.ParentItem;
                }
            }
        }

        [Inject]
        NavigationManager NavigationManager { get; set; }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();

            expanded = Expanded;

            selected = Selected;

            NavigationManager.LocationChanged += OnLocationChanged;

            if (ParentItem != null)
            {
                ParentItem.AddItem(this);
            }
            else if (Parent != null)
            {
                Parent.AddItem(this);
            }

            SyncWithNavigationManager();
        }

        string WrapperClass => ClassList.Create("rz-navigation-item-wrapper")
            .Add("rz-navigation-item-wrapper-active", selected)
            .ToString();

        string LinkClass => ClassList.Create("rz-navigation-item-link")
            .Add("rz-navigation-item-link-active", selected)
            .ToString();

        private bool selected = false;

        private void OnLocationChanged(object sender, LocationChangedEventArgs e)
        {
            SyncWithNavigationManager();
        }

        private void SyncWithNavigationManager()
        {
            var matches = ShouldMatch();

            if (matches != selected)
            {
                selected = matches;

                EnsureVisible();

                StateHasChanged();
            }
        }

        bool ShouldMatch()
        {
            if (string.IsNullOrEmpty(Path))
            {
                return false;
            }

            var currentAbsoluteUrl = NavigationManager.ToAbsoluteUri(NavigationManager.Uri).AbsoluteUri;
            var absoluteUrl = NavigationManager.ToAbsoluteUri(Path).AbsoluteUri;

            if (EqualsHrefExactlyOrIfTrailingSlashAdded(absoluteUrl, currentAbsoluteUrl))
            {
                return true;
            }

            if (Path == "/")
            {
                return false;
            }

            var match = Match != NavLinkMatch.Prefix ? Match : Parent.Match;

            if (match == NavLinkMatch.Prefix && IsStrictlyPrefixWithSeparator(currentAbsoluteUrl, absoluteUrl))
            {
                return true;
            }

            return false;
        }

        private static bool EqualsHrefExactlyOrIfTrailingSlashAdded(string absoluteUrl, string currentAbsoluteUrl)
        {
            if (string.Equals(currentAbsoluteUrl, absoluteUrl, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (currentAbsoluteUrl.Length == absoluteUrl.Length - 1)
            {
                if (absoluteUrl[^1] == '/' && absoluteUrl.StartsWith(currentAbsoluteUrl, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSeparator(char c)
        {
            return c == '?' || c == '/' || c == '#';
        }

        private static bool IsStrictlyPrefixWithSeparator(string value, string prefix)
        {
            var prefixLength = prefix.Length;
            if (value.Length > prefixLength)
            {
                return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    && (
                        prefixLength == 0
                        || IsSeparator(prefix[prefixLength - 1])
                        || IsSeparator(value[prefixLength])
                    );
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var expandedChanged = parameters.DidParameterChange(nameof(Expanded), Expanded);

            var selectedChanged = parameters.DidParameterChange(nameof(Selected), Selected);

            await base.SetParametersAsync(parameters);

            if (expandedChanged)
            {
                expanded = Expanded;
            }

            if (selectedChanged)
            {
                selected = Selected;

                if (selected)
                {
                    EnsureVisible();
                }
            }
        }

        /// <summary>
        /// Handles the <see cref="E:Click" /> event.
        /// </summary>
        /// <param name="args">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        public async Task OnClick(MouseEventArgs args)
        {
            if (Parent != null)
            {
                var eventArgs = new MenuItemEventArgs
                {
                    Text = Text,
                    Path = Path,
                    Value = Value,
                    AltKey = args.AltKey,
                    Button = args.Button,
                    Buttons = args.Buttons,
                    ClientX = args.ClientX,
                    ClientY = args.ClientY,
                    CtrlKey = args.CtrlKey,
                    Detail = args.Detail,
                    MetaKey = args.MetaKey,
                    ScreenX = args.ScreenX,
                    ScreenY = args.ScreenY,
                    ShiftKey = args.ShiftKey,
                    Type = args.Type,
                };
                await Parent.Click.InvokeAsync(eventArgs);

                if (Click.HasDelegate)
                {
                    await Click.InvokeAsync(eventArgs);
                }
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            NavigationManager.LocationChanged -= OnLocationChanged;

            if (Parent != null)
            {
                Parent.RemoveItem(this);
            }
        }
    }
}

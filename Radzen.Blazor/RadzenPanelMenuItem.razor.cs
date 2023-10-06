using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenPanelMenuItem component.
    /// </summary>
    public partial class RadzenPanelMenuItem : RadzenComponent
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-navigation-item";
        }

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
        public bool Expanded { get; set; } = false;

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

        async System.Threading.Tasks.Task Toggle()
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

                Parent.CollapseAll(itemsToSkip);
            }

            expanded = !expanded;
            await ExpandedChanged.InvokeAsync(expanded);
            StateHasChanged();
        }

        internal async System.Threading.Tasks.Task Collapse()
        {
            if (expanded)
            {
                expanded = false;
                await ExpandedChanged.InvokeAsync(expanded);
                StateHasChanged();
            }
        }

        string getStyle()
        {
            string deg = expanded ? "180" : "0";
            return $@"transform: rotate({deg}deg);";
        }

        string getIconStyle()
        { 
            return $"{(Parent?.DisplayStyle == MenuItemDisplayStyle.Icon ? "margin-right:0px;" : "")}{(!string.IsNullOrEmpty(IconColor) ? $"color:{IconColor}" : "")}";
        }

        string getItemStyle()
        {
            return expanded ? "" : "display:none";
        }

        void Expand()
        {
            expanded = true;
        }

        RadzenPanelMenu _parent;

        /// <summary>
        /// Gets or sets the click callback.
        /// </summary>
        /// <value>The click callback.</value>
        [Parameter]
        public EventCallback<MenuItemEventArgs> Click { get; set; }

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        [CascadingParameter]
        public RadzenPanelMenu Parent
        {
            get
            {
                return _parent;
            }
            set
            {
                if (_parent != value)
                {
                    _parent = value;
                    _parent.AddItem(this);
                }
            }
        }

        RadzenPanelMenuItem _parentItem;

        /// <summary>
        /// Gets or sets the parent item.
        /// </summary>
        /// <value>The parent item.</value>
        [CascadingParameter]
        public RadzenPanelMenuItem ParentItem
        {
            get
            {
                return _parentItem;
            }
            set
            {
                if (_parentItem != value)
                {
                    _parentItem = value;
                    _parentItem.AddItem(this);

                    EnsureVisible();
                }
            }
        }

        internal List<RadzenPanelMenuItem> items = new List<RadzenPanelMenuItem>();

        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void AddItem(RadzenPanelMenuItem item)
        {
            if (items.IndexOf(item) == -1)
            {
                items.Add(item);
                StateHasChanged();
            }
        }

        /// <summary>
        /// Selects the specified item by value.
        /// </summary>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public void Select(bool value)
        {
            Selected = value;

            StateHasChanged();
        }

        void EnsureVisible()
        {
            if (Selected)
            {
                var parent = ParentItem;

                while (parent != null)
                {
                    parent.Expand();
                    parent = parent.ParentItem;
                }
            }
        }

        private bool expanded = false;

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(Expanded), Expanded))
            {
                expanded = parameters.GetValueOrDefault<bool>(nameof(Expanded));
            }

            await base.SetParametersAsync(parameters);
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
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenMenuItem component.
    /// </summary>
    public partial class RadzenMenuItem : RadzenComponent
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return $"rz-navigation-item{(Disabled ? " rz-state-disabled" : "")}{(Parent.IsFocused(this) ? " rz-state-focused" : "")}";
        }

        /// <summary>
        /// Gets a value indicating whether this instance is disabled.
        /// </summary>
        /// <value><c>true</c> if this instance is disabled; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets the target.
        /// </summary>
        /// <value>The target.</value>
        [Parameter]
        public string Target { get; set; }

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
        /// Gets or sets the image style.
        /// </summary>
        /// <value>The image style.</value>
        [Parameter]
        public string ImageStyle { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public string ImageAlternateText { get; set; } = "image";

        /// <summary>
        /// Gets or sets the navigation link match.
        /// </summary>
        /// <value>The navigation link match.</value>
        [Parameter]
        public NavLinkMatch Match { get; set; } = NavLinkMatch.Prefix;

        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The template.</value>
        [Parameter]
        public RenderFragment Template { get; set; }

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the click callback.
        /// </summary>
        /// <value>The click callback.</value>
        [Parameter]
        public EventCallback<MenuItemEventArgs> Click { get; set; }

        RadzenMenuItem _parentItem;

        /// <summary>
        /// Gets or sets the parent item.
        /// </summary>
        /// <value>The parent item.</value>
        [CascadingParameter]
        public RadzenMenuItem ParentItem
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
                }
            }
        }

        RadzenMenu _parent;
        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        [CascadingParameter]
        public RadzenMenu Parent
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

                    if (ParentItem == null)
                    {
                        _parent.AddItem(this);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the <see cref="E:Click" /> event.
        /// </summary>
        /// <param name="args">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        public async Task OnClick(MouseEventArgs args)
        {
            if (Parent != null && !Disabled)
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

        Dictionary<string, object> getOpenEvents()
        {
            var events = new Dictionary<string, object>();

            if (!Disabled)
            {
                if (Parent.ClickToOpen || ChildContent != null)
                {
                    events.Add("onclick", "Radzen.toggleMenuItem(this)");
                }
                else
                {
                    events.Add("onclick", "Radzen.toggleMenuItem(this, event, false)");
                }
            }

            return events;
        }

        internal List<RadzenMenuItem> items = new List<RadzenMenuItem>();

        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void AddItem(RadzenMenuItem item)
        {
            if (items.IndexOf(item) == -1)
            {
                items.Add(item);
                StateHasChanged();
            }
        }

        /// <summary>
        /// Toggle the menu item.
        /// </summary>
        public async Task Toggle()
        {
            await JSRuntime.InvokeVoidAsync("Radzen.toggleMenuItem", Element);
        }

        /// <summary>
        /// Close the menu item.
        /// </summary>
        public async Task Close()
        {
            await JSRuntime.InvokeVoidAsync("Radzen.toggleMenuItem", Element, "event", false);
        }

        /// <summary>
        /// Open the menu item.
        /// </summary>
        public async Task Open()
        {
            await JSRuntime.InvokeVoidAsync("Radzen.toggleMenuItem", Element, "event", true);
        }
    }
}

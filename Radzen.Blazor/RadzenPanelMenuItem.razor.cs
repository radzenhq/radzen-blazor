using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenPanelMenuItem component.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponent" />
    public partial class RadzenPanelMenuItem : RadzenComponent
    {
        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
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
        /// Gets or sets the icon.
        /// </summary>
        /// <value>The icon.</value>
        [Parameter]
        public string Icon { get; set; }

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
            expanded = !expanded;
            await ExpandedChanged.InvokeAsync(Expanded);
            StateHasChanged();
        }

        string getStyle()
        {
            string deg = expanded ? "180" : "0";
            return $@"transform: rotate({deg}deg);";
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

        List<RadzenPanelMenuItem> items = new List<RadzenPanelMenuItem>();

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

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
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
        public async System.Threading.Tasks.Task OnClick(MouseEventArgs args)
        {
            if (Parent != null)
            {
                await Parent.Click.InvokeAsync(new MenuItemEventArgs() { Text = this.Text, Path = this.Path, Value = this.Value });
            }
        }
    }
}
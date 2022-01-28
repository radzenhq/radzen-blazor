using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

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
            return "rz-navigation-item";
        }

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

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        [CascadingParameter]
        public RadzenMenu Parent { get; set; }

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

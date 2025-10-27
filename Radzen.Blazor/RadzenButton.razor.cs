using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A clickable button component that supports various visual styles, icons, images, and loading states.
    /// RadzenButton provides a consistent and accessible way to trigger actions in your Blazor application.
    /// Supports multiple visual variants (Filled, Flat, Outlined, Text), color styles (Primary, Secondary, Success, etc.), 
    /// and sizes (ExtraSmall, Small, Medium, Large). It can display text, icons, images, or a combination of these.
    /// When <see cref="IsBusy"/> is true, the button shows a loading indicator and becomes disabled.
    /// </summary>
    /// <example>
    /// Basic usage with text and click handler:
    /// <code>
    /// &lt;RadzenButton Click=@(args => Console.WriteLine("Button clicked")) Text="Click Me" /&gt;
    /// </code>
    /// Button with icon and custom style:
    /// <code>
    /// &lt;RadzenButton Icon="save" Text="Save" ButtonStyle="ButtonStyle.Success" Variant="Variant.Flat" Size="ButtonSize.Large" /&gt;
    /// </code>
    /// Button with busy state:
    /// <code>
    /// &lt;RadzenButton IsBusy=@isSaving BusyText="Saving..." Text="Save" Click=@SaveData /&gt;
    /// </code>
    /// </example>
    public partial class RadzenButton : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the custom child content to be rendered inside the button.
        /// When set, this content will be displayed instead of the <see cref="Text"/>, <see cref="Icon"/>, or <see cref="Image"/>.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the tab index for keyboard navigation.
        /// Controls the order in which the button receives focus when the user presses the Tab key.
        /// </summary>
        /// <value>The tab index. Default value is 0.</value>
        [Parameter]
        public int TabIndex { get; set; } = 0;

        /// <summary>
        /// Gets or sets the text label displayed on the button.
        /// If both <see cref="Text"/> and <see cref="Icon"/> are set, both will be displayed.
        /// </summary>
        /// <value>The button text. Default is empty string.</value>
        [Parameter]
        public string Text { get; set; } = "";

        /// <summary>
        /// Gets or sets the alternate text for the button's image.
        /// This is used as the alt attribute when an <see cref="Image"/> is specified, improving accessibility.
        /// </summary>
        /// <value>The image alternate text. Default is "button".</value>
        [Parameter]
        public string ImageAlternateText { get; set; } = "button";

        /// <summary>
        /// Gets or sets the Material icon name to be displayed in the button.
        /// Use Material Symbols icon names (e.g., "save", "delete", "add"). The icon will be rendered using the rzi icon font.
        /// </summary>
        /// <value>The Material icon name.</value>
        [Parameter]
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets a custom color for the icon.
        /// This overrides the default icon color determined by the button's <see cref="ButtonStyle"/> and <see cref="Variant"/>.
        /// Supports any valid CSS color value (e.g., "#FF0000", "rgb(255, 0, 0)", "var(--my-color)").
        /// </summary>
        /// <value>The icon color as a CSS color value.</value>
        [Parameter]
        public string IconColor { get; set; }

        /// <summary>
        /// Gets or sets the URL of an image to be displayed in the button.
        /// The image will be rendered as an img element. For icon fonts, use the <see cref="Icon"/> property instead.
        /// </summary>
        /// <value>The image URL or path.</value>
        [Parameter]
        public string Image { get; set; }

        /// <summary>
        /// Gets or sets the semantic color style of the button.
        /// Determines the button's color scheme based on its purpose (e.g., Primary for main actions, Danger for destructive actions).
        /// </summary>
        /// <value>The button style. Default is <see cref="ButtonStyle.Primary"/>.</value>
        [Parameter]
        public ButtonStyle ButtonStyle { get; set; } = ButtonStyle.Primary;

        /// <summary>
        /// Gets or sets the HTML button type attribute.
        /// Use <see cref="ButtonType.Submit"/> for form submissions or <see cref="ButtonType.Button"/> for regular clickable buttons.
        /// </summary>
        /// <value>The button type. Default is <see cref="ButtonType.Button"/>.</value>
        [Parameter]
        public ButtonType ButtonType { get; set; } = ButtonType.Button;

        /// <summary>
        /// Gets or sets the design variant that controls the button's visual appearance.
        /// Options include Filled (solid background), Flat (subtle background), Outlined (border only), and Text (minimal styling).
        /// </summary>
        /// <value>The button variant. Default is <see cref="Variant.Filled"/>.</value>
        [Parameter]
        public virtual Variant Variant { get; set; } = Variant.Filled;

        /// <summary>
        /// Gets or sets the color intensity shade for the button.
        /// Works in combination with <see cref="ButtonStyle"/> to adjust the color darkness/lightness.
        /// </summary>
        /// <value>The color shade. Default is <see cref="Shade.Default"/>.</value>
        [Parameter]
        public virtual Shade Shade { get; set; } = Shade.Default;

        /// <summary>
        /// Gets or sets the button size.
        /// Controls the padding, font size, and overall dimensions of the button.
        /// </summary>
        /// <value>The button size. Default is <see cref="ButtonSize.Medium"/>.</value>
        [Parameter]
        public virtual ButtonSize Size { get; set; } = ButtonSize.Medium;

        /// <summary>
        /// Gets or sets whether the button is disabled and cannot be clicked.
        /// When true, the button will have a disabled appearance and will not respond to user interactions.
        /// </summary>
        /// <value><c>true</c> if the button is disabled; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the button is clicked.
        /// This event will not fire if the button is <see cref="Disabled"/> or <see cref="IsBusy"/>.
        /// </summary>
        /// <value>The click event callback.</value>
        [Parameter]
        public EventCallback<MouseEventArgs> Click { get; set; }

        /// <summary>
        /// Gets or sets whether the button is in a busy/loading state.
        /// When true, the button displays a loading indicator, shows the <see cref="BusyText"/>, and becomes disabled.
        /// This is useful for indicating asynchronous operations are in progress.
        /// </summary>
        /// <value><c>true</c> if the button is busy; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool IsBusy { get; set; }

        /// <summary>
        /// Gets or sets the text displayed when the button is in a busy state (<see cref="IsBusy"/> is true).
        /// If not set, the button will show a loading indicator without text.
        /// </summary>
        /// <value>The busy state text. Default is empty string.</value>
        [Parameter]
        public string BusyText { get; set; } = "";

        /// <summary>
        /// Gets a value indicating whether this button is effectively disabled.
        /// The button is disabled if either <see cref="Disabled"/> is true or <see cref="IsBusy"/> is true.
        /// </summary>
        /// <value><c>true</c> if the button is disabled or busy; otherwise, <c>false</c>.</value>
        public bool IsDisabled { get => Disabled || IsBusy; }


        bool clicking;
        /// <summary>
        /// Handles the button click event. This method is called internally when the button is clicked.
        /// It prevents multiple simultaneous clicks and invokes the <see cref="Click"/> callback.
        /// </summary>
        /// <param name="args">The mouse event arguments containing click information.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual async Task OnClick(MouseEventArgs args)
        {
            if (clicking)
            {
                return;
            }

            try
            {
                clicking = true;

                await Click.InvokeAsync(args);
            }
            finally
            {
                clicking = false;
            }
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass() => ClassList.Create("rz-button")
                                                                     .AddButtonSize(Size)
                                                                     .AddVariant(Variant)
                                                                     .AddButtonStyle(ButtonStyle)
                                                                     .AddDisabled(IsDisabled)
                                                                     .AddShade(Shade)
                                                                     .Add($"rz-button-icon-only", string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(Icon))
                                                                     .ToString();
    }
}

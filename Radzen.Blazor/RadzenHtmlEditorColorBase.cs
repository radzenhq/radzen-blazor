using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenHtmlEditorColorBase.
    /// Implements the <see cref="Radzen.Blazor.RadzenHtmlEditorButtonBase" />
    /// </summary>
    /// <seealso cref="Radzen.Blazor.RadzenHtmlEditorButtonBase" />
    public abstract class RadzenHtmlEditorColorBase : RadzenHtmlEditorButtonBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether [show HSV].
        /// </summary>
        /// <value><c>true</c> if [show HSV]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowHSV { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether [show rgba].
        /// </summary>
        /// <value><c>true</c> if [show rgba]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowRGBA { get; set; } = true;

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show colors].
        /// </summary>
        /// <value><c>true</c> if [show colors]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowColors { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether [show button].
        /// </summary>
        /// <value><c>true</c> if [show button]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowButton { get; set; } = true;

        /// <summary>
        /// Gets or sets the hexadecimal text.
        /// </summary>
        /// <value>The hexadecimal text.</value>
        [Parameter]
        public string HexText { get; set; } = "Hex";

        /// <summary>
        /// Gets or sets the red text.
        /// </summary>
        /// <value>The red text.</value>
        [Parameter]
        public string RedText { get; set; } = "R";

        /// <summary>
        /// Gets or sets the green text.
        /// </summary>
        /// <value>The green text.</value>
        [Parameter]
        public string GreenText { get; set; } = "G";

        /// <summary>
        /// Gets or sets the blue text.
        /// </summary>
        /// <value>The blue text.</value>
        [Parameter]
        public string BlueText { get; set; } = "B";

        /// <summary>
        /// Gets or sets the alpha text.
        /// </summary>
        /// <value>The alpha text.</value>
        [Parameter]
        public string AlphaText { get; set; } = "A";

        /// <summary>
        /// Gets or sets the button text.
        /// </summary>
        /// <value>The button text.</value>
        [Parameter]
        public string ButtonText { get; set; } = "OK";


        /// <summary>
        /// Called when [change].
        /// </summary>
        /// <param name="value">The value.</param>
        protected async Task OnChange(string value)
        {
            await Editor.ExecuteCommandAsync(CommandName, value);
        }
    }
}

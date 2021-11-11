using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenColorPickerItem component.
    /// </summary>
    public partial class RadzenColorPickerItem
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        [Parameter]
        public string Value { get; set; }

        string Background
        {
            get
            {
                RGB rgb = RGB.Parse(Value);

                return rgb?.ToCSS();
            }
        }

        /// <summary>
        /// Gets or sets the color picker.
        /// </summary>
        /// <value>The color picker.</value>
        [CascadingParameter]
        public RadzenColorPicker ColorPicker { get; set; }

        async Task OnClick()
        {
            await ColorPicker.SelectColor(Value);
        }
    }
}

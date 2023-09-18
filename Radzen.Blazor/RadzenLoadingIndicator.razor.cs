using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A component to show the loading indicator
    /// </summary>
    public partial class RadzenLoadingIndicator
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance loading indicator is shown.
        /// </summary>
        /// <value><c>true</c> if this instance loading indicator is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool IsLoading { get; set; }

        /// <summary>
        /// Gets or sets a value of the text while loading indicator is shown
        /// </summary>
        /// <value>Value of the loading indicator text; default: string.Empty</value>
        [Parameter]
        public string LoadingText { get; set; } = string.Empty;
    }
}

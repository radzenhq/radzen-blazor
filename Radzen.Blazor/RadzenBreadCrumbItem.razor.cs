using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Bread Crumb Item Component
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    public partial class RadzenBreadCrumbItem : RadzenComponent
    {
        [Parameter]
        public string Text { get; set; }

        [Parameter]
        public string Link { get; set; }

        [Parameter]
        public string Icon { get; set; }
    }
}

using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// A component to display a Bread Crumb style menu
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    public partial class RadzenBreadCrumb<TItem> : RadzenComponent
    {
        private int _count;

        /// <summary>
        /// An optional RenderFragment that is rendered per Item
        /// in the <see cref="Data"/> Collection
        /// </summary>
        [Parameter]
        public RenderFragment<TItem> Template { get; set; }

        /// <summary>
        /// Items to be Displayed
        /// </summary>
        [Parameter]
        public RenderFragment Items { get; set; }

        /// <summary>
        /// An optional RenderFragment that is rendered between Items
        /// </summary>
        [Parameter]
        public RenderFragment SeparatorTemplate { get; set; }

        /// <summary>
        /// A Separator string that is rendered between Items
        /// if <see cref="SeparatorTemplate"/> is not set
        /// </summary>
        [Parameter]
        public string Separator { get; set; } = "»";

        /// <summary>
        /// The name of the property used to get the content
        /// </summary>
        [Parameter]
        public string TextProperty { get; set; }

        /// <summary>
        /// Optional name of the property holding a path or link
        /// </summary>
        [Parameter]
        public string LinkProperty { get; set; }

        /// <summary>
        /// The Items to be displayed
        /// </summary>
        [Parameter]
        public IEnumerable<TItem> Data { get; set; }

        /// <inheritdoc/>
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            // temporary store the item count on each set
            _count = Data?.Count() ?? 0;
        }

        private string GetLink(TItem item)
            => item.GetType().GetProperty(LinkProperty).GetValue(item, null).ToString();


        private string GetText(TItem item)
            => item.GetType().GetProperty(TextProperty).GetValue(item, null).ToString();
    }

}

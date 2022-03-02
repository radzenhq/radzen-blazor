using Microsoft.AspNetCore.Components;
using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// Bread Crumb Item Component
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    public partial class RadzenBreadCrumbItem<TItem> : RadzenComponent
    {
        /// <summary>
        /// Parent <see cref="RadzenBreadCrumb{TItem}"/>
        /// </summary>
        [CascadingParameter]
        public RadzenBreadCrumb<TItem> BreadCrumb { get; set; }

        /// <summary>
        /// The Item
        /// </summary>
        [Parameter]
        public TItem Item { get; set; }

        private readonly Type type = typeof(TItem);

        private string GetLink()
        {
            if (BreadCrumb.LinkProperty == null)
            {
                return string.Empty;
            }
            else
            {
                return type
                    .GetProperty(BreadCrumb.LinkProperty)
                    .GetValue(Item, null)
                    .ToString();
            }
        }

        private string GetText()
        {
            if (BreadCrumb.TextProperty == null)
            {
                return string.Empty;
            }
            else
            {
                return type
                    .GetProperty(BreadCrumb.TextProperty)
                    .GetValue(Item, null)
                    .ToString();
            }
        }
    }
}

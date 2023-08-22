using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data.Common;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenDataList component.
    /// </summary>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenDataList @data=@orders TItem="Order" AllowPaging="true" WrapItems="true"&gt;
    ///     &lt;Template&gt;
    ///         @context.OrderId
    ///     &lt;/Template&gt;
    /// &lt;/RadzenDataList&gt;
    /// </code>
    /// </example>
    public partial class RadzenDataList<TItem> : PagedDataBoundComponent<TItem>
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-datalist-content";
        }

        /// <summary>
        /// Gets or sets a value indicating whether to wrap items.
        /// </summary>
        /// <value><c>true</c> if wrap items; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool WrapItems { get; set; }
#if NET5_0_OR_GREATER

        /// <summary>
        /// Gets or sets a value indicating whether this instance is virtualized.
        /// </summary>
        /// <value><c>true</c> if this instance is virtualized; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowVirtualization { get; set; }

        internal Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<TItem> virtualize;

        /// <summary>
        /// Gets Virtualize component reference.
        /// </summary>
        public Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<TItem> Virtualize
        {
            get
            {
                return virtualize;
            }
        }

        private async ValueTask<Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderResult<TItem>> LoadItems(Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderRequest request)
        {
            var view = AllowPaging ? PagedView : View;
            var top = request.Count;

            if(top <= 0)
            {
                top = PageSize;
            }

            await LoadData.InvokeAsync(new Radzen.LoadDataArgs()
            {
                Skip = request.StartIndex,
                Top = top
            });
            
            var totalItemsCount = LoadData.HasDelegate ? Count : view.Count();

            var virtualDataItems = (LoadData.HasDelegate ? Data : view.Skip(request.StartIndex).Take(top))?.ToList();

            return new Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderResult<TItem>(virtualDataItems, totalItemsCount);
        }
#endif
        RenderFragment DrawDataListRows()
        {
            return new RenderFragment(builder =>
            {
#if NET5_0_OR_GREATER
                if (AllowVirtualization)
                {
                    builder.OpenComponent(0, typeof(Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<TItem>));
                    builder.AddAttribute(1, "ItemsProvider", new Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderDelegate<TItem>(LoadItems));
                    
                    builder.AddAttribute(2, "ChildContent", (RenderFragment<TItem>)((context) =>
                    {
                        return (RenderFragment)((b) =>
                        {
                            DrawRow(b, context);
                        });
                    }));

                    builder.AddComponentReferenceCapture(4, c => { virtualize = (Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<TItem>)c; });

                    builder.CloseComponent();
                }
                else
                {
                    DrawRows(builder);
                }
#else
                DrawRows(builder);
#endif
            });
        }

        /// <summary>
        /// Reloads this instance.
        /// </summary>
        public async override Task Reload()
        {
            await base.Reload();
#if NET5_0_OR_GREATER
            if (virtualize != null)
            {
                await virtualize.RefreshDataAsync();
            }
#endif
        }

        internal void DrawRows(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            foreach (var item in LoadData.HasDelegate ? Data : PagedView)
            {
                DrawRow(builder, item);
            }
        }

        internal void DrawRow(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, TItem item)
        {
            builder.OpenComponent<RadzenDataListRow<TItem>>(0);
            builder.AddAttribute(1, "DataList", this);
            builder.AddAttribute(2, "Item", item);
            builder.SetKey(item);
            builder.CloseComponent();
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance loading indicator is shown.
        /// </summary>
        /// <value><c>true</c> if this instance loading indicator is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool IsLoading { get; set; }
    }
}

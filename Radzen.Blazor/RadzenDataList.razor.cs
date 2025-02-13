﻿using Microsoft.AspNetCore.Components;
using System.Linq;
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
#if NET6_0_OR_GREATER
    [CascadingTypeParameter(nameof(TItem))]
#endif
    public partial class RadzenDataList<TItem> : PagedDataBoundComponent<TItem>
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-datalist-content";
        }

        /// <summary>
        /// Gets or sets a value indicating whether DataList should show empty message.
        /// </summary>
        [Parameter]
        public bool ShowEmptyMessage { get; set; }

        private string _emptyText = "No records to display.";
        /// <summary>
        /// Gets or sets the empty text shown when Data is empty collection.
        /// </summary>
        /// <value>The empty text.</value>
        [Parameter]
        public string EmptyText
        {
            get { return _emptyText; }
            set
            {
                if (value != _emptyText)
                {
                    _emptyText = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the empty template shown when Data is empty collection.
        /// </summary>
        /// <value>The empty template.</value>
        [Parameter]
        public RenderFragment EmptyTemplate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to wrap items.
        /// </summary>
        /// <value><c>true</c> if wrap items; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool WrapItems { get; set; }

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
        RenderFragment DrawDataListRows()
        {
            return new RenderFragment(builder =>
            {
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
            });
        }

        /// <summary>
        /// Reloads this instance.
        /// </summary>
        public async override Task Reload()
        {
            await base.Reload();

            if (virtualize != null)
            {
                await virtualize.RefreshDataAsync();
            }
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

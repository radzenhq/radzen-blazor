using Microsoft.AspNetCore.Components;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A data list component for displaying collections of items using custom templates, with support for paging and virtualization.
    /// RadzenDataList provides a flexible way to render data in cards, tiles, or custom layouts instead of traditional table rows.
    /// Ideal when you need more control over item presentation than a traditional table provides. Perfect for product catalogs, image galleries, card-based dashboards, or any non-tabular data display.
    /// Supports complete control over item rendering via Template parameter, built-in paging with configurable page size, item wrapping to multiple columns/rows based on container width,
    /// efficient rendering for large datasets via virtualization, customizable message or template for empty state when no data exists, and on-demand data loading for server-side paging via LoadData.
    /// Use Template to define how each item should be rendered. The template receives the item as @context. Combine with RadzenRow/RadzenColumn for grid-based layouts or RadzenCard for card designs.
    /// </summary>
    /// <typeparam name="TItem">The type of data items in the list. Each item is rendered using the Template.</typeparam>
    /// <example>
    /// Basic data list with card template:
    /// <code>
    /// &lt;RadzenDataList Data=@products TItem="Product" AllowPaging="true" PageSize="12"&gt;
    ///     &lt;Template Context="product"&gt;
    ///         &lt;RadzenCard Style="width: 250px; margin: 1rem;"&gt;
    ///             &lt;RadzenImage Path=@product.ImageUrl Style="width: 100%;" /&gt;
    ///             &lt;RadzenText TextStyle="TextStyle.H6"&gt;@product.Name&lt;/RadzenText&gt;
    ///             &lt;RadzenText&gt;@product.Price.ToString("C")&lt;/RadzenText&gt;
    ///         &lt;/RadzenCard&gt;
    ///     &lt;/Template&gt;
    /// &lt;/RadzenDataList&gt;
    /// </code>
    /// Data list with empty state:
    /// <code>
    /// &lt;RadzenDataList Data=@items TItem="Item" WrapItems="true" ShowEmptyMessage="true" EmptyText="No items found"&gt;
    ///     &lt;Template Context="item"&gt;
    ///         &lt;div class="item-card"&gt;@item.Name&lt;/div&gt;
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
        /// Gets or sets whether to display an empty message when the data source has no items.
        /// Enable this to show EmptyText or EmptyTemplate when the list is empty, providing user feedback.
        /// </summary>
        /// <value><c>true</c> to show empty message; <c>false</c> to show nothing. Default is <c>false</c>.</value>
        [Parameter]
        public bool ShowEmptyMessage { get; set; }

        private string emptyText = "No records to display.";
        /// <summary>
        /// Gets or sets the text message displayed when the data source is empty.
        /// Only shown if <see cref="ShowEmptyMessage"/> is true and no <see cref="EmptyTemplate"/> is specified.
        /// </summary>
        /// <value>The empty state message text. Default is "No records to display."</value>
        [Parameter]
        public string EmptyText
        {
            get { return emptyText; }
            set
            {
                if (value != emptyText)
                {
                    emptyText = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a custom template for rendering the empty state when the data source has no items.
        /// Takes precedence over <see cref="EmptyText"/> when both are set.
        /// Use this for rich empty states with images, icons, or action buttons.
        /// </summary>
        /// <value>The empty state template render fragment.</value>
        [Parameter]
        public RenderFragment EmptyTemplate { get; set; }

        /// <summary>
        /// Gets or sets whether items should wrap to multiple rows based on their width and the container size.
        /// When true, items flow horizontally and wrap like words in a paragraph. When false, items stack vertically.
        /// </summary>
        /// <value><c>true</c> to enable wrapping (horizontal flow); <c>false</c> for vertical stacking. Default is <c>false</c>.</value>
        [Parameter]
        public bool WrapItems { get; set; }

        /// <summary>
        /// Gets or sets whether the DataList uses virtualization to improve performance with large datasets.
        /// When enabled, only visible items are rendered in the DOM, significantly improving performance for long lists.
        /// </summary>
        /// <value><c>true</c> to enable virtualization; <c>false</c> for standard rendering. Default is <c>false</c>.</value>
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

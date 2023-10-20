using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenPager component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenPager Count="100" PageSize="10" PageNumbersCount="5" PageChanged=@(args => Console.WriteLine($"Skip: {args.Skip}, Top: {args.Top}")) /&gt;
    /// </code>
    /// </example>
    public partial class RadzenPager : RadzenComponent
    {
        static readonly IDictionary<HorizontalAlign, string> HorizontalAlignCssClasses = new Dictionary<HorizontalAlign, string>
        {
            {HorizontalAlign.Center, "rz-align-center"},
            {HorizontalAlign.Left, "rz-align-left"},
            {HorizontalAlign.Right, "rz-align-right"},
            {HorizontalAlign.Justify, "rz-align-justify"}
        };

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            var additionalClasses = new List<string>();

            if (Density == Density.Compact)
            {
                additionalClasses.Add("rz-density-compact");
            }

            return $"rz-paginator rz-unselectable-text rz-helper-clearfix {HorizontalAlignCssClasses[HorizontalAlign]} {String.Join(" ", additionalClasses)}";
        }

        /// <summary>
        /// Gets or sets the pager's first page button's title attribute.
        /// </summary>
        [Parameter]
        public string FirstPageTitle { get; set; } = "First page.";

        /// <summary>
        /// Gets or sets the pager's first page button's aria-label attribute.
        /// </summary>
        [Parameter]
        public string FirstPageAriaLabel { get; set; } = "Go to first page.";

        /// <summary>
        /// Gets or sets the pager's previous page button's title attribute.
        /// </summary>
        [Parameter]
        public string PrevPageTitle { get; set; } = "Previous page";

        /// <summary>
        /// Gets or sets the pager's previous page button's aria-label attribute.
        /// </summary>
        [Parameter]
        public string PrevPageAriaLabel { get; set; } = "Go to previous page.";

        /// <summary>
        /// Gets or sets the pager's last page button's title attribute.
        /// </summary>
        [Parameter]
        public string LastPageTitle { get; set; } = "Last page";

        /// <summary>
        /// Gets or sets the pager's last page button's aria-label attribute.
        /// </summary>
        [Parameter]
        public string LastPageAriaLabel { get; set; } = "Go to last page.";

        /// <summary>
        /// Gets or sets the pager's next page button's title attribute.
        /// </summary>
        [Parameter]
        public string NextPageTitle { get; set; } = "Next page";

        /// <summary>
        /// Gets or sets the pager's next page button's aria-label attribute.
        /// </summary>
        [Parameter]
        public string NextPageAriaLabel { get; set; } = "Go to next page.";
        
        /// <summary>
        /// Gets or sets the pager's numeric page number buttons' title attributes.
        /// </summary>
        [Parameter]
        public string PageTitleFormat { get; set; } = "Page {0}";
        
        /// <summary>
        /// Gets or sets the pager's numeric page number buttons' aria-label attributes.
        /// </summary>
        [Parameter]
        public string PageAriaLabelFormat { get; set; } = "Go to page {0}.";
        
        /// <summary>
        /// Gets or sets the horizontal align.
        /// </summary>
        /// <value>The horizontal align.</value>
        [Parameter]
        public HorizontalAlign HorizontalAlign { get; set; } = HorizontalAlign.Justify;

        /// <summary>
        /// Gets or sets a value indicating Pager density.
        /// </summary>
        [Parameter]
        public Density Density { get; set; } = Density.Default;

        /// <summary>
        /// Gets or sets the page size.
        /// </summary>
        /// <value>The page size.</value>
        [Parameter]
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the page size changed callback.
        /// </summary>
        /// <value>The page size changed callback.</value>
        [Parameter]
        public EventCallback<int> PageSizeChanged { get; set; }

        /// <summary>
        /// Gets or sets the page size options.
        /// </summary>
        /// <value>The page size options.</value>
        [Parameter]
        public IEnumerable<int> PageSizeOptions { get; set; }

        /// <summary>
        /// Gets or sets the page size description text.
        /// </summary>
        /// <value>The page size description text.</value>
        [Parameter]
        public string PageSizeText { get; set; } = "items per page";

        /// <summary>
        /// Gets or sets the pager summary visibility.
        /// </summary>
        /// <value>The pager summary visibility.</value>
        [Parameter]
        public bool ShowPagingSummary { get; set; }

        /// <summary>
        /// Gets or sets the pager summary format.
        /// </summary>
        /// <value>The pager summary format.</value>
        [Parameter]
        public string PagingSummaryFormat { get; set; } = "Page {0} of {1} ({2} items)";

        /// <summary>
        /// Gets or sets the page numbers count.
        /// </summary>
        /// <value>The page numbers count.</value>
        [Parameter]
        public int PageNumbersCount { get; set; } = 5;

        /// <summary>
        /// Gets or sets the total items count.
        /// </summary>
        /// <value>The total items count.</value>
        [Parameter]
        public int Count { get; set; }

        /// <summary>
        /// Gets the current page.
        /// </summary>
        /// <value>The current page.</value>
        public int CurrentPage
        {
            get
            {
                return GetPage();
            }
        }

        /// <summary>
        /// Gets the visible.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected bool GetVisible()
        {
            return Visible && (AlwaysVisible || Count > PageSize || (PageSizeOptions != null && PageSizeOptions.Any()));
        }

        /// <summary>
        /// Gets or sets a value indicating whether pager is visible even when not enough data for paging.
        /// </summary>
        /// <value><c>true</c> if pager is visible even when not enough data for paging otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AlwaysVisible { get; set; }

        /// <summary>
        /// Gets or sets the page changed callback.
        /// </summary>
        /// <value>The page changed callback.</value>
        [Parameter]
        public EventCallback<PagerEventArgs> PageChanged { get; set; }

        /// <summary>
        /// Reloads this instance.
        /// </summary>
        public async virtual Task Reload()
        {
            await InvokeAsync(CalculatePager);
        }

        /// <summary>
        /// Called when parameters set asynchronous.
        /// </summary>
        /// <returns>Task.</returns>
        protected override Task OnParametersSetAsync()
        {
            if (GetVisible())
            {
                InvokeAsync(Reload);
            }

            return base.OnParametersSetAsync();
        }

        /// <summary>
        /// Called when page size changed.
        /// </summary>
        /// <param name="value">The value.</param>
        protected async Task OnPageSizeChanged(object value)
        {
            bool isFirstPage = CurrentPage == 0;
            bool isLastPage = CurrentPage == numberOfPages - 1 && numberOfPages > 1;
            int prevSkip = skip;
            PageSize = (int)value;
            await InvokeAsync(Reload);
            await PageSizeChanged.InvokeAsync((int)value);
            if (isLastPage)
            {
                await LastPage();
            }
            else if (!isFirstPage)
            {
                int newPage = (int)Math.Floor((decimal)(prevSkip / (PageSize > 0 ? PageSize : 10)));
                await GoToPage(newPage, true);
            }
        }

        /// <summary>
        /// Gets or sets number of recrods to skip.
        /// </summary>
        protected int skip;

        /// <summary>
        /// Gets or sets number of page links.
        /// </summary>
        protected int numberOfPageLinks = 5;

        /// <summary>
        /// Gets or sets start page.
        /// </summary>
        protected int startPage;

        /// <summary>
        /// Gets or sets end page.
        /// </summary>
        protected int endPage;

        /// <summary>
        /// Gets or sets number of pages.
        /// </summary>
        protected int numberOfPages;

        /// <summary>
        /// Calculates the pager.
        /// </summary>
        protected void CalculatePager()
        {
            var pageSize = PageSize > 0 ? PageSize : 10;
            numberOfPages = (int)Math.Ceiling((decimal)Count / pageSize);

            if (numberOfPages < 1)
            {
                numberOfPages = 1;
            }

            int visiblePages = Math.Min(PageNumbersCount, numberOfPages);

            startPage = (int)Math.Max(0, Math.Ceiling((decimal)(CurrentPage - (visiblePages / 2))));
            endPage = Math.Min(numberOfPages - 1, startPage + visiblePages - 1);

            var delta = PageNumbersCount - (endPage - startPage + 1);
            startPage = Math.Max(0, startPage - delta);

            if (skip == Count)
            {
                skip = pageSize * (numberOfPages - 1);
                //InvokeAsync(Reload);
                //PageChanged.InvokeAsync(new PagerEventArgs() { Skip = skip, Top = PageSize, PageIndex = CurrentPage });
            }
        }

        /// <summary>
        /// Gets the page.
        /// </summary>
        /// <returns>System.Int32.</returns>
        protected int GetPage()
        {
            return skip / (PageSize > 0 ? PageSize : 10);
        }

        /// <summary>
        /// Goes to specified page.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="forceReload">if set to <c>true</c> [force reload].</param>
        public async Task GoToPage(int page, bool forceReload = false)
        {
            if (CurrentPage != page || forceReload)
            {
                skip = page * PageSize;
                await InvokeAsync(Reload);
                await PageChanged.InvokeAsync(new PagerEventArgs() { Skip = skip, Top = PageSize, PageIndex = CurrentPage });
            }
        }

        internal void SetCurrentPage(int page)
        {
            if (CurrentPage != page)
            {
                skip = page * PageSize;
            }
        }

        internal void SetCount(int count)
        {
            Count = count;
            CalculatePager();
        }

        /// <summary>
        /// Goes to first page.
        /// </summary>
        /// <param name="forceReload">if set to <c>true</c> [force reload].</param>
        public async Task FirstPage(bool forceReload = false)
        {
            if (CurrentPage != 0 || forceReload)
            {
                skip = 0;
                await InvokeAsync(Reload);
                await PageChanged.InvokeAsync(new PagerEventArgs() { Skip = skip, Top = PageSize, PageIndex = CurrentPage });
            }
        }

        /// <summary>
        /// Goes to previous page.
        /// </summary>
        public async Task PrevPage()
        {
            var newskip = skip - PageSize < 0 ? 0 : skip - PageSize;
            if (newskip != skip)
            {
                skip = newskip;
                await InvokeAsync(Reload);
                await PageChanged.InvokeAsync(new PagerEventArgs() { Skip = skip, Top = PageSize, PageIndex = CurrentPage });
            }
        }

        /// <summary>
        /// Goes to next page.
        /// </summary>
        public async Task NextPage()
        {
            var newskip = PageSize * (CurrentPage < (numberOfPages - 1) ? CurrentPage + 1 : numberOfPages - 1);
            if (newskip != skip)
            {
                skip = newskip;
                await InvokeAsync(Reload);
                await PageChanged.InvokeAsync(new PagerEventArgs() { Skip = skip, Top = PageSize, PageIndex = CurrentPage });
            }
        }

        /// <summary>
        /// Goes to last page.
        /// </summary>
        public async Task LastPage()
        {
            var newskip = PageSize * (numberOfPages - 1);
            if (newskip != skip)
            {
                skip = newskip;
                await InvokeAsync(Reload);
                await PageChanged.InvokeAsync(new PagerEventArgs() { Skip = skip, Top = PageSize, PageIndex = CurrentPage });
            }
        }
    }
}

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// A pagination component that provides navigation controls for paged data display.
    /// RadzenPager displays page numbers, navigation buttons, and optional page size selector for navigating through large datasets.
    /// Works standalone or integrated with data components like RadzenDataGrid and RadzenDataList.
    /// Provides navigation buttons (First/Previous/Next/Last page with customizable labels and icons), clickable page number buttons with configurable count,
    /// optional dropdown to change items per page, summary display ("Page X of Y" or custom summary text), alignment controls (left/center/right) via HorizontalAlign,
    /// compact or default spacing density, and ARIA labels for all buttons for accessibility.
    /// The PageChanged event provides Skip and Top values for loading the correct page of data.
    /// Use Count to specify total items, PageSize for items per page, and PageNumbersCount for visible page buttons.
    /// </summary>
    /// <example>
    /// Basic pager:
    /// <code>
    /// &lt;RadzenPager Count=@totalCount PageSize="20" PageNumbersCount="5" PageChanged=@LoadPage /&gt;
    /// @code {
    ///     int totalCount = 250;
    ///     async Task LoadPage(PagerEventArgs args) 
    ///     {
    ///         // Load items from args.Skip, take args.Top
    ///     }
    /// }
    /// </code>
    /// Pager with page size selector:
    /// <code>
    /// &lt;RadzenPager Count=@totalCount PageSize=@pageSize PageSizeChanged=@OnPageSizeChanged
    ///              PageSizeOptions=@(new int[] { 10, 20, 50, 100 }) ShowPagingSummary="true" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenPager : RadzenComponent
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass() => ClassList.Create("rz-pager rz-unselectable-text rz-helper-clearfix")
                                                                     .Add("rz-density-compact", Density == Density.Compact)
                                                                     .AddHorizontalAlign(HorizontalAlign)
                                                                     .ToString();

        /// <summary>
        /// Gets or sets the pager's first page button's title attribute.
        /// </summary>
        [Parameter]
        public string FirstPageTitle { get; set; } = "First page";

        /// <summary>
        /// Gets or sets the pager's first page button's aria-label attribute.
        /// </summary>
        [Parameter]
        public string FirstPageAriaLabel { get; set; } = "Go to first page.";

        /// <summary>
        /// Gets or sets the pager's optional previous page button's label text.
        /// </summary>
        [Parameter]
        public string PrevPageLabel { get; set; }

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
        /// Gets or sets the pager's optional next page button's label text.
        /// </summary>
        [Parameter]
        public string NextPageLabel { get; set; }

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
        /// Gets or sets the pager summary format. <see cref="PagingSummaryTemplate" /> has preference over this property.
        /// </summary>
        /// <value>The pager summary format.</value>
        [Parameter]
        public string PagingSummaryFormat { get; set; } = "Page {0} of {1} ({2} items)";

#nullable enable
        /// <summary>
        /// Gets or sets the pager summary template. Has preference over <see cref="PagingSummaryFormat" />.
        /// </summary>
        [Parameter]
		public RenderFragment<PagingInformation>? PagingSummaryTemplate { get; set; }
#nullable restore

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

        async Task OnFirstPageClick()
        {
            focusedIndex = -2;

            await FirstPage();

            if (skip == 0)
            {
                focusedIndex = focusedIndex + 2;
            }
        }

        async Task OnPrevPageClick()
        {
            focusedIndex = -1;

            await PrevPage();

            if (skip == 0)
            {
                focusedIndex++;
            }
        }

        async Task OnPageClick(int i, int startPage)
        {
            focusedIndex = i - startPage;
            await GoToPage(i);
        }

        async Task OnNextPageClick(int endPage)
        {
            focusedIndex = Math.Min(endPage + 1, PageNumbersCount);

            await NextPage();

            if (CurrentPage == numberOfPages - 1)
            {
                focusedIndex--;
            }
        }

        async Task OnLastPageClick(int endPage)
        {
            focusedIndex = Math.Min(endPage + 1, PageNumbersCount) + 1;

            await LastPage();

            if (CurrentPage == numberOfPages - 1)
            {
                focusedIndex = focusedIndex - 2;
            }
        }

        internal void ChangeState()
        {
            StateHasChanged();
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

        bool preventKeyDown = false;
        int focusedIndex = -3;

        /// <summary>
        /// Handles the <see cref="E:KeyDown" /> event.
        /// </summary>
        /// <param name="args">The <see cref="KeyboardEventArgs"/> instance containing the event data.</param>
        protected virtual async Task OnKeyDown(KeyboardEventArgs args)
        {
            var key = args.Code != null ? args.Code : args.Key;

            var numberOfDisplayedPages = Math.Min(endPage + 1, PageNumbersCount);

            if (key == "ArrowLeft" || key == "ArrowRight")
            {
                preventKeyDown = true;

                focusedIndex = Math.Clamp(focusedIndex + (key == "ArrowLeft" ? -1 : 1), -2, numberOfDisplayedPages + 1);

                if (CurrentPage == 0 && focusedIndex < 0)
                {
                    focusedIndex = 0;
                }
                else if (CurrentPage == numberOfPages - 1 && focusedIndex > numberOfDisplayedPages - 1)
                {
                    focusedIndex = numberOfDisplayedPages - 1;
                }
            }
            else if (key == "Space" || key == "Enter")
            {
                preventKeyDown = true;

                if (focusedIndex == -2)
                {
                    await FirstPage();
                    shouldFocus = true;
                }
                else if (focusedIndex == -1)
                {
                    await PrevPage();
                    shouldFocus = true;
                }
                else if (focusedIndex == numberOfDisplayedPages)
                {
                    await NextPage();
                    shouldFocus = true;
                }
                else if (focusedIndex == numberOfDisplayedPages + 1)
                {
                    await LastPage();
                    shouldFocus = true;
                }
                else 
                {
                    await GoToPage(focusedIndex + startPage);
                    shouldFocus = true;
                }

                if (CurrentPage == 0 && focusedIndex < 0)
                {
                    focusedIndex = 0;
                }
                else if (CurrentPage == numberOfPages - 1 && focusedIndex > numberOfDisplayedPages - 1)
                {
                    focusedIndex = numberOfDisplayedPages - 1;
                }
            }
            else
            {
                preventKeyDown = false;
                shouldFocus = false;
            }
        }

        bool shouldFocus;

        void OnFocus()
        {
            focusedIndex = focusedIndex == -3 ? 0 : focusedIndex;

            if (CurrentPage == 0 && focusedIndex < 0)
            {
                focusedIndex = 0;
            }
            else if (CurrentPage == numberOfPages - 1 && focusedIndex > numberOfPages - 1)
            {
                focusedIndex = numberOfPages - 1;
            }
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (shouldFocus)
            {
                shouldFocus = false;
                await JSRuntime.InvokeVoidAsync("Radzen.focusElement", GetId());
            }    
        }
    }
}

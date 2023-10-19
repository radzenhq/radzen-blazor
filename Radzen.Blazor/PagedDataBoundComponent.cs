using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;

namespace Radzen
{
    /// <summary>
    /// Base classes of components that support paging.
    /// </summary>
    /// <typeparam name="T">The type of the data item</typeparam>
    public class PagedDataBoundComponent<T> : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the pager position. Set to <c>PagerPosition.Bottom</c> by default.
        /// </summary>
        /// <value>The pager position.</value>
        [Parameter]
        public PagerPosition PagerPosition { get; set; } = PagerPosition.Bottom;

        /// <summary>
        /// Gets or sets a value indicating whether pager is visible even when not enough data for paging.
        /// </summary>
        /// <value><c>true</c> if pager is visible even when not enough data for paging otherwise, <c>false</c>.</value>
        [Parameter]
        public bool PagerAlwaysVisible { get; set; }

        /// <summary>
        /// Gets or sets the horizontal align.
        /// </summary>
        /// <value>The horizontal align.</value>
        [Parameter]
        public HorizontalAlign PagerHorizontalAlign { get; set; } = HorizontalAlign.Justify;

        /// <summary>
        /// Gets or sets a value indicating pager density.
        /// </summary>
        [Parameter]
        public Density Density { get; set; } = Density.Default;

        /// <summary>
        /// Gets or sets a value indicating whether paging is allowed. Set to <c>false</c> by default.
        /// </summary>
        /// <value><c>true</c> if paging is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowPaging { get; set; }

        int _PageSize = 10;
        /// <summary>
        /// Gets or sets the size of the page.
        /// </summary>
        /// <value>The size of the page.</value>
        [Parameter]
        public int PageSize 
        {
            get
            {
                return pageSize ?? _PageSize;
            }
            set
            {
                if (_PageSize != value)
                {
                    _PageSize = value;
                    InvokeAsync(() => OnPageSizeChanged(value));
                }
            }
        }

        internal int GetPageSize()
        {
            return _PageSize;
        }

        internal void SetPageSize(int value)
        {
            _PageSize = value;
        }

        /// <summary>
        /// Gets or sets the page numbers count.
        /// </summary>
        /// <value>The page numbers count.</value>
        [Parameter]
        public int PageNumbersCount { get; set; } = 5;

        /// <summary>
        /// Gets or sets the count.
        /// </summary>
        /// <value>The count.</value>
        [Parameter]
        public int Count { get; set; }
        /// <summary>
        /// Gets or sets the current page.
        /// </summary>
        /// <value>The current page.</value>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The template.</value>
        [Parameter]
        public RenderFragment<T> Template { get; set; }

        /// <summary>
        /// Gets or sets the loading template.
        /// </summary>
        /// <value>The loading template.</value>
        [Parameter]
        public RenderFragment LoadingTemplate { get; set; }

        /// <summary>
        /// The data
        /// </summary>
        IEnumerable<T> _data;

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        [Parameter]
        public IEnumerable<T> Data
        {
            get
            {
                return _data;
            }
            set
            {
                if (_data != value)
                {
                    _data = value;

                    if (_data != null && _data is INotifyCollectionChanged)
                    {
                        ((INotifyCollectionChanged)_data).CollectionChanged += OnCollectionChanged;
                    }

                    OnDataChanged();
                    StateHasChanged();
                }
            }
        }

        /// <summary>
        /// Called when INotifyCollectionChanged CollectionChanged is raised.
        /// </summary>
        protected virtual void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {

        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            if (_data != null && _data is INotifyCollectionChanged)
            {
                ((INotifyCollectionChanged)_data).CollectionChanged -= OnCollectionChanged;
            }
        }

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
        public bool ShowPagingSummary { get; set; } = false;

        /// <summary>
        /// Gets or sets the pager summary format.
        /// </summary>
        /// <value>The pager summary format.</value>
        [Parameter]
        public string PagingSummaryFormat { get; set; } = "Page {0} of {1} ({2} items)";

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
        
        internal IQueryable<T> _view = null;
        /// <summary>
        /// Gets the paged view.
        /// </summary>
        /// <value>The paged view.</value>
        public virtual IQueryable<T> PagedView
        {
            get
            {
                if (_view == null)
                {
                    _view = (AllowPaging && !LoadData.HasDelegate ? View.Skip(skip).Take(PageSize) : View).ToList().AsQueryable();
                }
                return _view;
            }
        }

        /// <summary>
        /// Gets the view.
        /// </summary>
        /// <value>The view.</value>
        public virtual IQueryable<T> View
        {
            get
            {
                return Data != null ? Data.AsQueryable() : Enumerable.Empty<T>().AsQueryable();
            }
        }

        /// <summary>
        /// Gets or sets the load data.
        /// </summary>
        /// <value>The load data.</value>
        [Parameter]
        public EventCallback<Radzen.LoadDataArgs> LoadData { get; set; }

        /// <summary>
        /// Reloads this instance.
        /// </summary>
        public async virtual Task Reload()
        {
            _view = null;

            if (Data != null && !LoadData.HasDelegate)
            {
                Count = View.Count();
            }

            await LoadData.InvokeAsync(new Radzen.LoadDataArgs() { Skip = skip, Top = PageSize });

            CalculatePager();

            if (!LoadData.HasDelegate)
            {
                StateHasChanged();
            }
        }

        /// <summary>
        /// Called when [data changed].
        /// </summary>
        protected virtual void OnDataChanged()
        {

        }

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            bool pageSizeChanged = parameters.DidParameterChange(nameof(PageSize), PageSize) &&
                PageSize != pageSize;

            await base.SetParametersAsync(parameters);

            if (pageSizeChanged && !firstRender)
            {
               await InvokeAsync(Reload);
            }
        }

        /// <summary>
        /// Called when [parameters set asynchronous].
        /// </summary>
        /// <returns>Task.</returns>
        protected override Task OnParametersSetAsync()
        {
            if (Visible && !LoadData.HasDelegate)
            {
                InvokeAsync(Reload);
            }
            else
            {
                CalculatePager();
            }

            return base.OnParametersSetAsync();
        }

        /// <summary>
        /// The first render
        /// </summary>
        bool firstRender = true;
        /// <summary>
        /// Called when [after render asynchronous].
        /// </summary>
        /// <param name="firstRender">if set to <c>true</c> [first render].</param>
        /// <returns>Task.</returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            this.firstRender = firstRender;

            if (firstRender)
            {
                await ReloadOnFirstRender();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        internal virtual async Task ReloadOnFirstRender()
        {
            if (firstRender && Visible && (LoadData.HasDelegate && Data == null))
            {
                await InvokeAsync(Reload);
                StateHasChanged();
            }
        }

        /// <summary>
        /// The skip
        /// </summary>
        protected int skip;

        /// <summary>
        /// The top pager
        /// </summary>
        protected RadzenPager topPager;
        /// <summary>
        /// The bottom pager
        /// </summary>
        protected RadzenPager bottomPager;

        /// <summary>
        /// Gets or sets the page callback.
        /// </summary>
        /// <value>The page callback.</value>
        [Parameter]
        public EventCallback<PagerEventArgs> Page { get; set; }

        /// <summary>
        /// Handles the <see cref="E:PageChanged" /> event.
        /// </summary>
        /// <param name="args">The <see cref="PagerEventArgs"/> instance containing the event data.</param>
        protected async Task OnPageChanged(PagerEventArgs args)
        {
            skip = args.Skip;
            CurrentPage = args.PageIndex;

            await Page.InvokeAsync(args);

            await InvokeAsync(Reload);
        }

        internal int? pageSize;

        /// <summary>
        /// Called when [page size changed].
        /// </summary>
        /// <param name="value">The value.</param>
        protected virtual async Task OnPageSizeChanged(int value)
        {
            if (pageSize != value && !this.firstRender)
            {
                pageSize = value;
                await InvokeAsync(Reload);
            }
        }

        /// <summary>
        /// Calculates the pager.
        /// </summary>
        protected void CalculatePager()
        {
            if (topPager != null)
            {
                topPager.SetCount(Count);
                topPager.SetCurrentPage(CurrentPage);
            }

            if (bottomPager != null)
            {
                bottomPager.SetCount(Count);
                bottomPager.SetCurrentPage(CurrentPage);
            }
        }

        /// <summary>
        /// Goes to page.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="forceReload">if set to <c>true</c> [force reload].</param>
        public async Task GoToPage(int page, bool forceReload = false)
        {
            if (topPager != null)
            {
                await topPager.GoToPage(page, forceReload);
            }

            if (bottomPager != null)
            {
                await bottomPager.GoToPage(page, forceReload);
            }
        }

        /// <summary>
        /// Firsts the page.
        /// </summary>
        /// <param name="forceReload">if set to <c>true</c> [force reload].</param>
        public async Task FirstPage(bool forceReload = false)
        {
            var shouldReload = forceReload && CurrentPage == 0;

            if (topPager != null)
            {
                await topPager.FirstPage();
            }

            if (bottomPager != null)
            {
                await bottomPager.FirstPage();
            }

            if (shouldReload)
            {
                await InvokeAsync(Reload);
            }
        }

        /// <summary>
        /// Previouses the page.
        /// </summary>
        public async Task PrevPage()
        {
            if (topPager != null)
            {
                await topPager.PrevPage();
            }

            if (bottomPager != null)
            {
                await bottomPager.PrevPage();
            }
        }

        /// <summary>
        /// Nexts the page.
        /// </summary>
        public async Task NextPage()
        {
            if (topPager != null)
            {
                await topPager.NextPage();
            }

            if (bottomPager != null)
            {
                await bottomPager.NextPage();
            }
        }

        /// <summary>
        /// Lasts the page.
        /// </summary>
        public async Task LastPage()
        {
            if (topPager != null)
            {
                await topPager.LastPage();
            }

            if (bottomPager != null)
            {
                await bottomPager.LastPage();
            }
        }
    }
}

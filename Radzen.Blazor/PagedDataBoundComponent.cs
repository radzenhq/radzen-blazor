using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;

namespace Radzen
{
    /// <summary>
    /// Class PagedDataBoundComponent.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="Radzen.RadzenComponent" />
    public class PagedDataBoundComponent<T> : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the pager position.
        /// </summary>
        /// <value>The pager position.</value>
        [Parameter]
        public PagerPosition PagerPosition { get; set; } = PagerPosition.Bottom;

        /// <summary>
        /// Gets or sets a value indicating whether [allow paging].
        /// </summary>
        /// <value><c>true</c> if [allow paging]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowPaging { get; set; }

        /// <summary>
        /// Gets or sets the size of the page.
        /// </summary>
        /// <value>The size of the page.</value>
        [Parameter]
        public int PageSize { get; set; } = 10;

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
                    OnDataChanged();
                    StateHasChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the page size options.
        /// </summary>
        /// <value>The page size options.</value>
        [Parameter]
        public IEnumerable<int> PageSizeOptions { get; set; }
        
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
        /// The view
        /// </summary>
        protected IQueryable<T> _view = null;
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
            bool pageSizeChanged = parameters.DidParameterChange(nameof(PageSize), PageSize);

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
        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            this.firstRender = firstRender;
            if (firstRender && Visible && (LoadData.HasDelegate && Data == null))
            {
                InvokeAsync(Reload);
                StateHasChanged();
            }

            return base.OnAfterRenderAsync(firstRender);
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
        /// Handles the <see cref="E:PageChanged" /> event.
        /// </summary>
        /// <param name="args">The <see cref="PagerEventArgs"/> instance containing the event data.</param>
        protected async Task OnPageChanged(PagerEventArgs args)
        {
            skip = args.Skip;
            CurrentPage = args.PageIndex;
            await InvokeAsync(Reload);
        }

        /// <summary>
        /// Called when [page size changed].
        /// </summary>
        /// <param name="value">The value.</param>
        protected async Task OnPageSizeChanged(int value)
        {
            PageSize = value;
            await InvokeAsync(Reload);
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
        public async Task GoToPage(int page)
        {
            if (topPager != null)
            {
                await topPager.GoToPage(page);
            }

            if (bottomPager != null)
            {
                await bottomPager.GoToPage(page);
            }
        }

        /// <summary>
        /// Firsts the page.
        /// </summary>
        /// <param name="forceReload">if set to <c>true</c> [force reload].</param>
        public async Task FirstPage(bool forceReload = false)
        {
            if (topPager != null)
            {
                await topPager.FirstPage(forceReload);
            }

            if (bottomPager != null)
            {
                await bottomPager.FirstPage(forceReload);
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

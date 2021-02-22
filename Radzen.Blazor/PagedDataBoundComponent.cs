using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;

namespace Radzen
{
    public class PagedDataBoundComponent<T> : RadzenComponent
    {
        [Parameter]
        public PagerPosition PagerPosition { get; set; } = PagerPosition.Bottom;

        [Parameter]
        public bool AllowPaging { get; set; }

        [Parameter]
        public int PageSize { get; set; } = 10;

        [Parameter]
        public int PageNumbersCount { get; set; } = 5;

        [Parameter]
        public int Count { get; set; }
        public int CurrentPage { get; set; }

        [Parameter]
        public RenderFragment<T> Template { get; set; }

        IEnumerable<T> _data;

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

        protected IQueryable<T> _view = null;
        public virtual IQueryable<T> PagedView
        {
            get
            {
                if (_view == null)
                {
                    _view = (AllowPaging ? View.Skip(skip).Take(PageSize) : View).ToList().AsQueryable();
                }
                return _view;
            }
        }

        public virtual IQueryable<T> View
        {
            get
            {
                return Data != null ? Data.AsQueryable() : Enumerable.Empty<T>().AsQueryable();
            }
        }

        [Parameter]
        public EventCallback<Radzen.LoadDataArgs> LoadData { get; set; }

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

        protected virtual void OnDataChanged()
        {

        }

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

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && Visible && (LoadData.HasDelegate && Data == null))
            {
                InvokeAsync(Reload);
                StateHasChanged();
            }

            return base.OnAfterRenderAsync(firstRender);
        }

        protected int skip;

        protected RadzenPager topPager;
        protected RadzenPager bottomPager;

        protected async Task OnPageChanged(PagerEventArgs args)
        {
            skip = args.Skip;
            CurrentPage = args.PageIndex;
            await InvokeAsync(Reload);
        }

        protected void CalculatePager()
        {
            if (topPager != null)
            {
                topPager.GoToPage(CurrentPage).Wait();
                InvokeAsync(topPager.Reload);
            }

            if (bottomPager != null)
            {
                bottomPager.GoToPage(CurrentPage).Wait();
                InvokeAsync(bottomPager.Reload);
            }
        }

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

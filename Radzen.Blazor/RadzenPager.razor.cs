using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    public partial class RadzenPager : RadzenComponent
    {
        protected override string GetComponentCssClass()
        {
            return "rz-paginator rz-unselectable-text rz-helper-clearfix";
        }

        [Parameter]
        public int PageSize { get; set; } = 10;

        [Parameter]
        public EventCallback<int> PageSizeChanged { get; set; }

        [Parameter]
        public IEnumerable<int> PageSizeOptions { get; set; }

        [Parameter]
        public int PageNumbersCount { get; set; } = 5;

        [Parameter]
        public int Count { get; set; }

        public int CurrentPage
        {
            get
            {
                return GetPage();
            }
        }

        protected bool GetVisible()
        {
            return Visible && (Count > PageSize || (PageSizeOptions != null && PageSizeOptions.Any()));
        }

        [Parameter]
        public EventCallback<PagerEventArgs> PageChanged { get; set; }

        public async virtual Task Reload()
        {
            await InvokeAsync(CalculatePager);
        }

        protected override Task OnParametersSetAsync()
        {
            if (GetVisible())
            {
                InvokeAsync(Reload);
            }

            return base.OnParametersSetAsync();
        }

        protected async Task OnPageSizeChanged(object value)
        {
            bool isFirstPage = CurrentPage == 0;
            bool isLastPage = CurrentPage == numberOfPages - 1;
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

        protected int skip;
        protected int numberOfPageLinks = 5;
        protected int startPage;
        protected int endPage;
        protected int numberOfPages;

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
            }
        }

        protected int GetPage()
        {
            return (int)Math.Floor((decimal)(skip / (PageSize > 0 ? PageSize : 10)));
        }

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

        public async Task FirstPage(bool forceReload = false)
        {
            if (CurrentPage != 0 || forceReload)
            {
                skip = 0;
                await InvokeAsync(Reload);
                await PageChanged.InvokeAsync(new PagerEventArgs() { Skip = skip, Top = PageSize, PageIndex = CurrentPage });
            }
        }

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
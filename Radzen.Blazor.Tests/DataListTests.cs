using Bunit;
using Radzen.Blazor.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataListTests
    {
        [Fact]
        public void DataList_Renders_CssClass()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenDataList<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, new[] { 1,2,3}));

            Assert.Contains(@$"rz-datalist-content", component.Markup);
        }

        [Fact]
        public void DataList_Renders_AllowPagingParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenDataList<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.AllowPaging, true));

            Assert.Contains(@$"rz-paginator-bottom", component.Markup);
        }

        [Fact]
        public void DataList_Renders_PagerPositionTopParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenDataList<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            component.SetParametersAndRender(parameters => {
                parameters.Add<bool>(p => p.AllowPaging, true);
                parameters.Add<PagerPosition>(p => p.PagerPosition, PagerPosition.Top); 
            });

            Assert.Contains(@$"rz-paginator", component.Markup);
            Assert.DoesNotContain(@$"rz-paginator-bottom", component.Markup);
        }

        [Fact]
        public void DataList_Renders_PagerPositionTopAndBottomParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenDataList<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            component.SetParametersAndRender(parameters => {
                parameters.Add<bool>(p => p.AllowPaging, true);
                parameters.Add<PagerPosition>(p => p.PagerPosition, PagerPosition.TopAndBottom);
            });

            Assert.Contains(@$"rz-paginator", component.Markup);
            Assert.Contains(@$"rz-paginator-bottom", component.Markup);
        }

        [Fact]
        public void DataList_Renders_PagerDensityDefault()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenDataList<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.AllowPaging, true);
                parameters.Add<PagerPosition>(p => p.PagerPosition, PagerPosition.Top);
                parameters.Add<Density>(p => p.Density, Density.Default);
            });

            Assert.DoesNotContain(@$"rz-density-compact", component.Markup);
        }

        [Fact]
        public void DataList_Renders_PagerDensityCompact()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenDataList<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.AllowPaging, true);
                parameters.Add<PagerPosition>(p => p.PagerPosition, PagerPosition.Top);
                parameters.Add<Density>(p => p.Density, Density.Compact);
            });

            Assert.Contains(@$"rz-density-compact", component.Markup);
        }

        [Fact]
        public void DataList_Renders_WrapItemsParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenDataList<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, new[] { 1, 2, 3 }));

            component.SetParametersAndRender(parameters => {
                parameters.Add<bool>(p => p.WrapItems, true);
            });

            Assert.Contains(@$"rz-g", component.Markup);
        }

        [Fact]
        public void DataList_Raises_LoadDataEventOnNextPageClick()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenDataList<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            var raised = false;
            LoadDataArgs newArgs = null;

            component.SetParametersAndRender(parameters => {
                parameters.Add<bool>(p => p.AllowPaging, true);
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; newArgs = args; });
            });

            component.Find(".rz-paginator-next").Click();

            Assert.True(raised);
            Assert.True(newArgs.Skip == 10);
            Assert.True(newArgs.Top == 10);
        }

        [Fact]
        public void DataList_Raises_LoadDataEventOnLastPageClick()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenDataList<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            var raised = false;
            LoadDataArgs newArgs = null;

            component.SetParametersAndRender(parameters => {
                parameters.Add<bool>(p => p.AllowPaging, true);
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; newArgs = args; });
            });

            component.Find(".rz-paginator-last").Click();

            Assert.True(raised);
            Assert.True(newArgs.Skip == 90);
            Assert.True(newArgs.Top == 10);
        }

        [Fact]
        public void DataList_Raises_LoadDataEventOnPrevPageClick()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenDataList<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            var raised = false;
            LoadDataArgs newArgs = null;

            component.SetParametersAndRender(parameters => {
                parameters.Add<bool>(p => p.AllowPaging, true);
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; newArgs = args; });
            });

            component.Find(".rz-paginator-next").Click();
            component.Find(".rz-paginator-prev").Click();

            Assert.True(raised);
            Assert.True(newArgs.Skip == 0);
            Assert.True(newArgs.Top == 10);
        }

        [Fact]
        public void DataList_Raises_LoadDataEventOnFirstPageClick()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenDataList<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            var raised = false;
            LoadDataArgs newArgs = null;

            component.SetParametersAndRender(parameters => {
                parameters.Add<bool>(p => p.AllowPaging, true);
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; newArgs = args; });
            });

            component.Find(".rz-paginator-next").Click();
            component.Find(".rz-paginator-first").Click();

            Assert.True(raised);
            Assert.True(newArgs.Skip == 0);
            Assert.True(newArgs.Top == 10);
        }

        [Fact]
        public void DataList_NotRaises_LoadDataEventOnFirstPageClickWhenOnFirstPage()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenDataList<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            var raised = false;

            component.SetParametersAndRender(parameters => {
                parameters.Add<bool>(p => p.AllowPaging, true);
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; });
            });

            component.Find(".rz-paginator-first").Click();

            Assert.False(raised);
        }

        [Fact]
        public void DataList_NotRaises_LoadDataEventOnPrevPageClickWhenOnFirstPage()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenDataList<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            var raised = false;

            component.SetParametersAndRender(parameters => {
                parameters.Add<bool>(p => p.AllowPaging, true);
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; });
            });

            component.Find(".rz-paginator-prev").Click();

            Assert.False(raised);
        }

        [Fact]
        public void DataList_NotRaises_LoadDataEventOnLastPageClickWhenOnLastPage()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenDataList<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            var raised = false;

            component.SetParametersAndRender(parameters => {
                parameters.Add<bool>(p => p.AllowPaging, true);
            });

            component.Find(".rz-paginator-last").Click();

            component.SetParametersAndRender(parameters => {
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; });
            });

            component.Find(".rz-paginator-last").Click();

            Assert.False(raised);
        }

        [Fact]
        public void DataList_NotRaises_LoadDataEventOnNextPageClickWhenOnLastPage()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenDataList<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            var raised = false;

            component.SetParametersAndRender(parameters => {
                parameters.Add<bool>(p => p.AllowPaging, true);
            });

            component.Find(".rz-paginator-last").Click();

            component.SetParametersAndRender(parameters => {
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; });
            });

            component.Find(".rz-paginator-next").Click();

            Assert.False(raised);
        }

        [Fact]
        public void DataList_Respects_PageSizeParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenDataList<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            var raised = false;
            LoadDataArgs newArgs = null;

            component.SetParametersAndRender(parameters => {
                parameters.Add<bool>(p => p.AllowPaging, true);
                parameters.Add<int>(p => p.PageSize, 20);
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; newArgs = args; });
            });

            component.Find(".rz-paginator-next").Click();

            Assert.True(raised);
            Assert.True(newArgs.Skip == 20);
            Assert.True(newArgs.Top == 20);
        }
    }
}

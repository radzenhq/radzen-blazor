using Bunit;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor.Tests
{
    public class SearchTextProgrammaticTests
    {
        static readonly List<string> Fruits = new() { "Apple", "Orange", "Pear", "Grape" };

        static int ItemCount(IRenderedComponent<IComponent> component, string selector) => component.FindAll(selector).Count;

        [Fact]
        public void ListBox_ProgrammaticSearchText_RefiltersList()
        {
            using var ctx = new BunitContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.Render<RadzenListBox<string>>(parameters =>
            {
                parameters.Add(p => p.Data, Fruits);
                parameters.Add(p => p.AllowFiltering, true);
                parameters.Add(p => p.SearchText, "Ap");
            });

            Assert.Equal(1, ItemCount(component, "li[role=option]"));

            component.Render(parameters => parameters.Add(p => p.SearchText, null));

            Assert.Equal(4, ItemCount(component, "li[role=option]"));

            component.Render(parameters => parameters.Add(p => p.SearchText, "Pe"));

            Assert.Equal(1, ItemCount(component, "li[role=option]"));
            Assert.Contains("Pear", component.Markup);
        }

        [Fact]
        public void DropDown_ProgrammaticSearchText_RefiltersList()
        {
            using var ctx = new BunitContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.Render<RadzenDropDown<string>>(parameters =>
            {
                parameters.Add(p => p.Data, Fruits);
                parameters.Add(p => p.AllowFiltering, true);
                parameters.Add(p => p.SearchText, "Ap");
            });

            Assert.Equal(1, ItemCount(component, "li[role=option]"));

            component.Render(parameters => parameters.Add(p => p.SearchText, null));

            Assert.Equal(4, ItemCount(component, "li[role=option]"));
        }

        [Fact]
        public void DropDownDataGrid_ProgrammaticSearchText_RefiltersGrid()
        {
            using var ctx = new BunitContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.Render<RadzenDropDownDataGrid<string>>(parameters =>
            {
                parameters.Add(p => p.Data, Fruits);
                parameters.Add(p => p.AllowFiltering, true);
            });

            Assert.Equal(4, ItemCount(component, "tbody tr[role=row]:not(.rz-datatable-emptymessage-row)"));

            component.Render(parameters => parameters.Add(p => p.SearchText, "Ap"));

            Assert.Equal(1, ItemCount(component, "tbody tr[role=row]:not(.rz-datatable-emptymessage-row)"));
            Assert.Contains("Apple", component.Markup);

            component.Render(parameters => parameters.Add(p => p.SearchText, null));

            Assert.Equal(4, ItemCount(component, "tbody tr[role=row]:not(.rz-datatable-emptymessage-row)"));
        }

        [Fact]
        public void ListBox_ProgrammaticSearchText_WithLoadData_ReloadsWithNewFilter()
        {
            using var ctx = new BunitContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var filters = new List<string>();
            IEnumerable<string> data = Fruits;

            var component = ctx.Render<RadzenListBox<string>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.AllowFiltering, true);
                parameters.Add(p => p.SearchText, "Ap");
                parameters.Add(p => p.LoadData, EventCallback.Factory.Create<LoadDataArgs>(this, args => filters.Add(args.Filter)));
            });

            filters.Clear();

            component.Render(parameters => parameters.Add(p => p.SearchText, null));

            Assert.Contains(null, filters);
        }
    }
}

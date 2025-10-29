using Bunit;
using Xunit;
using System.Collections.Generic;

namespace Radzen.Blazor.Tests
{
    public class DropDownDataGridTests
    {
        class Customer
        {
            public int Id { get; set; }
            public string CompanyName { get; set; }
            public string ContactName { get; set; }
        }

        [Fact]
        public void DropDownDataGrid_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenDropDownDataGrid<int>>();

            Assert.Contains(@"rz-dropdown", component.Markup);
        }

        [Fact]
        public void DropDownDataGrid_Renders_DropdownTrigger()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenDropDownDataGrid<int>>();

            Assert.Contains("rz-dropdown-trigger", component.Markup);
            Assert.Contains("rzi-chevron-down", component.Markup);
        }

        [Fact]
        public void DropDownDataGrid_Renders_WithData()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Item1", "Item2", "Item3" };

            var component = ctx.RenderComponent<RadzenDropDownDataGrid<string>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
            });

            Assert.Contains("rz-lookup-panel", component.Markup);
        }

        [Fact]
        public void DropDownDataGrid_Renders_WithCustomTextProperty()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<Customer>
            {
                new Customer { Id = 1, CompanyName = "Acme Corp" }
            };

            var component = ctx.RenderComponent<RadzenDropDownDataGrid<int>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.TextProperty, "CompanyName");
                parameters.Add(p => p.ValueProperty, "Id");
            });

            Assert.Contains("rz-lookup-panel", component.Markup);
        }

        [Fact]
        public void DropDownDataGrid_Renders_AllowFiltering()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Item1", "Item2" };

            var component = ctx.RenderComponent<RadzenDropDownDataGrid<string>>(parameters =>
            {
                parameters.Add(p => p.AllowFiltering, true);
                parameters.Add(p => p.Data, data);
            });

            Assert.Contains("rz-lookup-search", component.Markup);
        }

        [Fact]
        public void DropDownDataGrid_Renders_Placeholder()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenDropDownDataGrid<int>>(parameters =>
            {
                parameters.Add(p => p.Placeholder, "Select an item");
            });

            Assert.Contains("Select an item", component.Markup);
            Assert.Contains("rz-placeholder", component.Markup);
        }

        [Fact]
        public void DropDownDataGrid_Renders_AllowClear()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Item1" };

            var component = ctx.RenderComponent<RadzenDropDownDataGrid<string>>(parameters =>
            {
                parameters.Add(p => p.AllowClear, true);
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.Value, "Item1");
            });

            Assert.Contains("rz-dropdown-clear-icon", component.Markup);
        }

        [Fact]
        public void DropDownDataGrid_Renders_Disabled()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenDropDownDataGrid<int>>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
            });

            Assert.Contains("disabled", component.Markup);
            Assert.Contains("rz-state-disabled", component.Markup);
        }

        [Fact]
        public void DropDownDataGrid_Renders_Multiple_WithChips()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenDropDownDataGrid<IEnumerable<int>>>(parameters =>
            {
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.Chips, true);
            });

            Assert.Contains("rz-multiselect-panel", component.Markup);
        }
    }
}


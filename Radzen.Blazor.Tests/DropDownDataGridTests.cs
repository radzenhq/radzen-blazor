using Bunit;
using Xunit;
using System.Collections.Generic;
using System.Linq;

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
        public void DropDownDataGrid_Renders_WithCustomTextValueProperties()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<Customer>
            {
                new Customer { Id = 1, CompanyName = "Acme Corp", ContactName = "John Doe" },
                new Customer { Id = 2, CompanyName = "Tech Inc", ContactName = "Jane Smith" }
            };

            var component = ctx.RenderComponent<RadzenDropDownDataGrid<int>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.TextProperty, "CompanyName");
                parameters.Add(p => p.ValueProperty, "Id");
            });

            Assert.Contains("rz-lookup-panel", component.Markup);
            Assert.Contains("rz-data-grid", component.Markup);
        }

        [Fact]
        public void DropDownDataGrid_Renders_DataGrid()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Item1" };

            var component = ctx.RenderComponent<RadzenDropDownDataGrid<string>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
            });

            // DropDownDataGrid embeds a DataGrid
            Assert.Contains("rz-data-grid", component.Markup);
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
            Assert.Contains("rz-lookup-search-input", component.Markup);
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
        public void DropDownDataGrid_Renders_AllowClear_WithValue()
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
        public void DropDownDataGrid_DoesNotRender_AllowClear_WhenNotAllowed()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Item1" };

            var component = ctx.RenderComponent<RadzenDropDownDataGrid<string>>(parameters =>
            {
                parameters.Add(p => p.AllowClear, false);
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.Value, "Item1");
            });

            Assert.DoesNotContain("rz-dropdown-clear-icon", component.Markup);
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
        public void DropDownDataGrid_Renders_Multiple_Panel()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenDropDownDataGrid<IEnumerable<int>>>(parameters =>
            {
                parameters.Add(p => p.Multiple, true);
            });

            Assert.Contains("rz-multiselect-panel", component.Markup);
        }

        [Fact]
        public void DropDownDataGrid_Renders_Multiple_WithChips()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Item1", "Item2" };
            var selectedItems = new List<string> { "Item1" };

            var component = ctx.RenderComponent<RadzenDropDownDataGrid<IEnumerable<string>>>(parameters =>
            {
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.Chips, true);
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.Value, selectedItems);
            });

            Assert.Contains("rz-dropdown-chips-wrapper", component.Markup);
            Assert.Contains("rz-chip", component.Markup);
        }

        [Fact]
        public void DropDownDataGrid_Renders_Multiple_WithChips_SelectedItemsTitle()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<Customer>
            {
                new Customer { Id = 1, CompanyName = "Company1" },
                new Customer { Id = 2, CompanyName = "Company2" },
                new Customer { Id = 3, CompanyName = "Company3" }
            };
            var selectedItems = new List<int> { 1, 2 };

            var component = ctx.RenderComponent<RadzenDropDownDataGrid<IEnumerable<int>>>(parameters =>
            {
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.Chips, true);
                parameters.Add(p => p.TextProperty, "CompanyName");
                parameters.Add(p => p.ValueProperty, "Id");
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.Value, selectedItems);
            });

            var wrapper = component.Find(".rz-dropdown-chips-wrapper");
            Assert.Equal("Company1,Company2", wrapper.GetAttribute("title"));
        }

        [Fact]
        public void DropDownDataGrid_Renders_Multiple_WithLabel_SelectedItemsTitle()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<Customer>
            {
                new Customer { Id = 1, CompanyName = "Company1" },
                new Customer { Id = 2, CompanyName = "Company2" },
                new Customer { Id = 3, CompanyName = "Company3" }
            };
            var selectedItems = new List<int> { 1, 2 };

            var component = ctx.RenderComponent<RadzenDropDownDataGrid<IEnumerable<int>>>(parameters =>
            {
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.Chips, false);
                parameters.Add(p => p.TextProperty, "CompanyName");
                parameters.Add(p => p.ValueProperty, "Id");
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.Value, selectedItems);
            });

            var label = component.Find("label.rz-dropdown-label");
            Assert.Equal("Company1,Company2", label.GetAttribute("title"));
        }

        [Fact]
        public void DropDownDataGrid_DoesNotRender_Multiple_SelectedItemsTitle_WithoutTextProperty()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Item1", "Item2", "Item3" };
            var selectedItems = new List<string> { "Item1", "Item3" };

            var component = ctx.RenderComponent<RadzenDropDownDataGrid<IEnumerable<string>>>(parameters =>
            {
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.Chips, true);
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.Value, selectedItems);
            });

            var wrapper = component.Find(".rz-dropdown-chips-wrapper");
            Assert.False(wrapper.HasAttribute("title"));
        }

        [Fact]
        public void DropDownDataGrid_Renders_Multiple_SelectedItemsTitle_CustomSeparator()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<Customer>
            {
                new Customer { Id = 1, CompanyName = "Company1" },
                new Customer { Id = 2, CompanyName = "Company2" },
                new Customer { Id = 3, CompanyName = "Company3" }
            };
            var selectedItems = new List<int> { 1, 2, 3 };

            var component = ctx.RenderComponent<RadzenDropDownDataGrid<IEnumerable<int>>>(parameters =>
            {
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.Chips, false);
                parameters.Add(p => p.Separator, "; ");
                parameters.Add(p => p.MaxSelectedLabels, 1);
                parameters.Add(p => p.TextProperty, "CompanyName");
                parameters.Add(p => p.ValueProperty, "Id");
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.Value, selectedItems);
            });

            var label = component.Find("label.rz-dropdown-label");
            Assert.Equal("Company1; Company2; Company3", label.GetAttribute("title"));
        }

        [Fact]
        public void DropDownDataGrid_Renders_AllowSorting()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<Customer>
            {
                new Customer { Id = 1, CompanyName = "Acme" }
            };

            var component = ctx.RenderComponent<RadzenDropDownDataGrid<int>>(parameters =>
            {
                parameters.Add(p => p.AllowSorting, true);
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.TextProperty, "CompanyName");
            });

            Assert.Contains("rz-data-grid", component.Markup);
        }

        [Fact]
        public void DropDownDataGrid_Renders_SearchTextPlaceholder()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Item1" };

            var component = ctx.RenderComponent<RadzenDropDownDataGrid<string>>(parameters =>
            {
                parameters.Add(p => p.AllowFiltering, true);
                parameters.Add(p => p.SearchTextPlaceholder, "Type to filter...");
                parameters.Add(p => p.Data, data);
            });

            Assert.Contains("placeholder=\"Type to filter...\"", component.Markup);
        }

        [Fact]
        public void DropDownDataGrid_Renders_EmptyText()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenDropDownDataGrid<string>>(parameters =>
            {
                parameters.Add(p => p.Data, new List<string>());
                parameters.Add(p => p.EmptyText, "No items found");
            });

            Assert.Contains("No items found", component.Markup);
        }

        [Fact]
        public void DropDownDataGrid_Renders_PageSize()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = Enumerable.Range(1, 20).Select(i => $"Item {i}").ToList();

            var component = ctx.RenderComponent<RadzenDropDownDataGrid<string>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.PageSize, 10);
            });

            // DataGrid with paging should be present
            Assert.Contains("rz-data-grid", component.Markup);
        }

        [Fact]
        public void DropDownDataGrid_Renders_AllowRowSelectOnRowClick()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Item1", "Item2" };

            var component = ctx.RenderComponent<RadzenDropDownDataGrid<string>>(parameters =>
            {
                parameters.Add(p => p.AllowRowSelectOnRowClick, true);
                parameters.Add(p => p.Data, data);
            });

            Assert.Contains("rz-data-grid", component.Markup);
        }

        // Regression test for https://github.com/radzenhq/radzen-blazor/issues/2546
        [Fact]
        public void DropDownDataGrid_LoadData_ReceivesSorts_OnColumnHeaderClick()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var data = new List<Customer>
            {
                new Customer { Id = 1, CompanyName = "Acme Corp", ContactName = "John Doe" }
            };

            LoadDataArgs capturedArgs = null;

            var component = ctx.RenderComponent<RadzenDropDownDataGrid<int>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.Count, 1);
                parameters.Add(p => p.TextProperty, "CompanyName");
                parameters.Add(p => p.ValueProperty, "Id");
                parameters.Add(p => p.AllowSorting, true);
                parameters.Add(p => p.LoadData, args => { capturedArgs = args; });
            });

            component.Find(".rz-sortable-column").FirstElementChild.Click();

            Assert.NotNull(capturedArgs);
            Assert.NotNull(capturedArgs.Sorts);
            Assert.Single(capturedArgs.Sorts);
            Assert.Equal("CompanyName", capturedArgs.Sorts.First().Property);
            Assert.Equal(SortOrder.Ascending, capturedArgs.Sorts.First().SortOrder);
            Assert.Equal("CompanyName asc", capturedArgs.OrderBy);
        }

        [Fact]
        public void DropDownDataGrid_LoadData_ReceivesFilter_OnSearchInputChange()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");
            ctx.JSInterop.Setup<string>("Radzen.getInputValue", _ => true).SetResult("foo");

            var data = new List<Customer>
            {
                new Customer { Id = 1, CompanyName = "Acme Corp", ContactName = "John Doe" }
            };

            var loadDataCalls = new List<LoadDataArgs>();

            var component = ctx.RenderComponent<RadzenDropDownDataGrid<int>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.Count, 1);
                parameters.Add(p => p.TextProperty, "CompanyName");
                parameters.Add(p => p.ValueProperty, "Id");
                parameters.Add(p => p.AllowFiltering, true);
                parameters.Add(p => p.LoadData, args => loadDataCalls.Add(args));
            });

            component.Find(".rz-lookup-search-input").Change("foo");

            Assert.Contains(loadDataCalls, c => c.Filter == "foo");
        }
    }
}


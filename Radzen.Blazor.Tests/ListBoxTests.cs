using Bunit;
using Xunit;
using System.Collections.Generic;

namespace Radzen.Blazor.Tests
{
    public class ListBoxTests
    {
        class Item
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Fact]
        public void ListBox_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenListBox<int>>();

            Assert.Contains(@"rz-listbox", component.Markup);
        }

        [Fact]
        public void ListBox_Renders_WithData_SimpleList()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Apple", "Banana", "Cherry" };
            
            var component = ctx.RenderComponent<RadzenListBox<string>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
            });

            Assert.Contains("Apple", component.Markup);
            Assert.Contains("Banana", component.Markup);
            Assert.Contains("Cherry", component.Markup);
        }

        [Fact]
        public void ListBox_Renders_WithData_CustomTextValueProperties()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<Item>
            {
                new Item { Id = 1, Name = "First Item" },
                new Item { Id = 2, Name = "Second Item" }
            };
            
            var component = ctx.RenderComponent<RadzenListBox<int>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.TextProperty, "Name");
                parameters.Add(p => p.ValueProperty, "Id");
            });

            Assert.Contains("First Item", component.Markup);
            Assert.Contains("Second Item", component.Markup);
        }

        [Fact]
        public void ListBox_Renders_AllowFiltering()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenListBox<int>>(parameters =>
            {
                parameters.Add(p => p.AllowFiltering, true);
                parameters.Add(p => p.Data, new List<string> { "Item1", "Item2" });
            });

            Assert.Contains("rz-listbox-filter", component.Markup);
            Assert.Contains("rz-listbox-header", component.Markup);
        }

        [Fact]
        public void ListBox_Renders_Disabled_Attribute()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenListBox<int>>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
            });

            Assert.Contains("disabled", component.Markup);
        }

        [Fact]
        public void ListBox_Renders_Multiple_WithSelectAll()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenListBox<IEnumerable<int>>>(parameters =>
            {
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.SelectAllText, "Select All Items");
                parameters.Add(p => p.Data, new List<string> { "Item1", "Item2" });
            });

            Assert.Contains("Select All Items", component.Markup);
            Assert.Contains("rz-chkbox", component.Markup);
        }

        [Fact]
        public void ListBox_Renders_FilterPlaceholder()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenListBox<int>>(parameters =>
            {
                parameters.Add(p => p.Placeholder, "Select an item");
                parameters.Add(p => p.AllowFiltering, true);
                parameters.Add(p => p.Data, new List<string> { "Item1", "Item2" });
            });

            Assert.Contains("Select an item", component.Markup);
        }

        [Fact]
        public void ListBox_Renders_Multiple_WithCheckboxes()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Item1", "Item2" };

            var component = ctx.RenderComponent<RadzenListBox<IEnumerable<string>>>(parameters =>
            {
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.Data, data);
            });

            // Multiple selection shows checkboxes in header
            Assert.Contains("rz-listbox-header-w-checkbox", component.Markup);
        }

        [Fact]
        public void ListBox_Renders_ReadOnly()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenListBox<int>>(parameters =>
            {
                parameters.Add(p => p.ReadOnly, true);
            });

            Assert.Contains("readonly", component.Markup);
        }

        [Fact]
        public void ListBox_Renders_TabIndex()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenListBox<int>>();

            Assert.Contains("tabindex=", component.Markup);
        }

        [Fact]
        public void ListBox_Renders_ListWrapper()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Item1" };

            var component = ctx.RenderComponent<RadzenListBox<string>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
            });

            Assert.Contains("rz-listbox-list-wrapper", component.Markup);
            Assert.Contains("rz-listbox-list", component.Markup);
        }

        [Fact]
        public void ListBox_Renders_SearchAriaLabel()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenListBox<int>>(parameters =>
            {
                parameters.Add(p => p.AllowFiltering, true);
                parameters.Add(p => p.SearchAriaLabel, "Search items");
                parameters.Add(p => p.Data, new List<string> { "Item1" });
            });

            Assert.Contains("aria-label=\"Search items\"", component.Markup);
        }
    }
}




using Bunit;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor.Tests
{
    public class PickListTests
    {
        class Item
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Fact]
        public void PickList_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenPickList<Item>>();

            Assert.Contains(@"rz-picklist", component.Markup);
        }

        [Fact]
        public void PickList_Renders_SourceAndTargetLists()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenPickList<Item>>();

            Assert.Contains("rz-picklist-source-wrapper", component.Markup);
            Assert.Contains("rz-picklist-target-wrapper", component.Markup);
        }

        [Fact]
        public void PickList_Renders_TransferButtons()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenPickList<Item>>();

            Assert.Contains("rz-picklist-buttons", component.Markup);
        }

        [Fact]
        public void PickList_Renders_Orientation_Horizontal()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenPickList<Item>>(parameters =>
            {
                parameters.Add(p => p.Orientation, Orientation.Horizontal);
            });

            Assert.Contains("rz-flex-row", component.Markup);
        }

        [Fact]
        public void PickList_Renders_Orientation_Vertical()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenPickList<Item>>(parameters =>
            {
                parameters.Add(p => p.Orientation, Orientation.Vertical);
            });

            Assert.Contains("rz-flex-column", component.Markup);
        }

        [Fact]
        public void PickList_Renders_AllowFiltering()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<Item>
            {
                new Item { Id = 1, Name = "Item 1" }
            };

            var component = ctx.RenderComponent<RadzenPickList<Item>>(parameters =>
            {
                parameters.Add(p => p.AllowFiltering, true);
                parameters.Add(p => p.Source, data);
                parameters.Add(p => p.TextProperty, "Name");
            });

            Assert.Contains("rz-listbox-filter", component.Markup);
        }

        [Fact]
        public void PickList_Renders_SourceData()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<Item>
            {
                new Item { Id = 1, Name = "Source Item 1" },
                new Item { Id = 2, Name = "Source Item 2" }
            };

            var component = ctx.RenderComponent<RadzenPickList<Item>>(parameters =>
            {
                parameters.Add(p => p.Source, data);
                parameters.Add(p => p.TextProperty, "Name");
            });

            Assert.Contains("Source Item 1", component.Markup);
            Assert.Contains("Source Item 2", component.Markup);
        }

        [Fact]
        public void PickList_Renders_ShowHeader()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenPickList<Item>>(parameters =>
            {
                parameters.Add(p => p.ShowHeader, true);
                parameters.Add(p => p.SourceHeader, builder => builder.AddContent(0, "Available Items"));
                parameters.Add(p => p.TargetHeader, builder => builder.AddContent(0, "Selected Items"));
            });

            Assert.Contains("Available Items", component.Markup);
            Assert.Contains("Selected Items", component.Markup);
        }

        [Fact]
        public void PickList_Renders_AllowMoveAll_Buttons()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<Item> { new Item { Id = 1, Name = "Item" } };

            var component = ctx.RenderComponent<RadzenPickList<Item>>(parameters =>
            {
                parameters.Add(p => p.AllowMoveAll, true);
                parameters.Add(p => p.Source, data);
            });

            // Should have 4 buttons when AllowMoveAll is true
            var buttonCount = System.Text.RegularExpressions.Regex.Matches(component.Markup, "rz-button").Count;
            Assert.True(buttonCount >= 4);
        }

        [Fact]
        public void PickList_Renders_Disabled()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenPickList<Item>>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
            });

            Assert.Contains("disabled", component.Markup);
        }

        [Fact]
        public void PickList_GetSelectedSources_Respects_ValueProperty_Single()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var data = new List<Item>
            {
                new Item { Id = 1, Name = "A" },
                new Item { Id = 2, Name = "B" }
            };

            var component = ctx.RenderComponent<RadzenPickList<Item>>(parameters =>
            {
                parameters.Add(p => p.Source, data);
                parameters.Add(p => p.TextProperty, "Name");
                parameters.Add(p => p.ValueProperty, "Id");
                parameters.Add(p => p.Multiple, false);
            });

            // Simulate ListBox selection when ValueProperty is set: selectedSourceItems becomes the value (Id)
            var field = typeof(RadzenPickList<Item>).GetField("selectedSourceItems", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(component.Instance, 2);

            var selected = component.Instance.GetSelectedSources();
            Assert.Single(selected);
            Assert.Equal(2, selected.First().Id);
        }

        [Fact]
        public void PickList_GetSelectedSources_Respects_ValueProperty_Multiple()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var data = new List<Item>
            {
                new Item { Id = 1, Name = "A" },
                new Item { Id = 2, Name = "B" },
                new Item { Id = 3, Name = "C" }
            };

            var component = ctx.RenderComponent<RadzenPickList<Item>>(parameters =>
            {
                parameters.Add(p => p.Source, data);
                parameters.Add(p => p.TextProperty, "Name");
                parameters.Add(p => p.ValueProperty, "Id");
                parameters.Add(p => p.Multiple, true);
            });

            // Simulate ListBox selection when ValueProperty is set: selectedSourceItems becomes IEnumerable of values (Ids)
            var field = typeof(RadzenPickList<Item>).GetField("selectedSourceItems", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(component.Instance, new[] { 1, 3 });

            var selected = component.Instance.GetSelectedSources().Select(i => i.Id).OrderBy(i => i).ToArray();
            Assert.Equal(new[] { 1, 3 }, selected);
        }
    }
}


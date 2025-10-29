using Bunit;
using Xunit;
using System.Collections.Generic;

namespace Radzen.Blazor.Tests
{
    public class CheckBoxListTests
    {
        class Item
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool Disabled { get; set; }
        }

        [Fact]
        public void CheckBoxList_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenCheckBoxList<int>>();

            Assert.Contains(@"rz-checkbox-list", component.Markup);
        }

        [Fact]
        public void CheckBoxList_Renders_WithData()
        {
            using var ctx = new TestContext();
            var data = new List<string> { "Option 1", "Option 2", "Option 3" };

            var component = ctx.RenderComponent<RadzenCheckBoxList<string>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
            });

            Assert.Contains("Option 1", component.Markup);
            Assert.Contains("Option 2", component.Markup);
            Assert.Contains("Option 3", component.Markup);
        }

        [Fact]
        public void CheckBoxList_Renders_WithCustomTextValueProperties()
        {
            using var ctx = new TestContext();
            var data = new List<Item>
            {
                new Item { Id = 1, Name = "First" },
                new Item { Id = 2, Name = "Second" }
            };

            var component = ctx.RenderComponent<RadzenCheckBoxList<int>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.TextProperty, "Name");
                parameters.Add(p => p.ValueProperty, "Id");
            });

            Assert.Contains("First", component.Markup);
            Assert.Contains("Second", component.Markup);
        }

        [Fact]
        public void CheckBoxList_Renders_Orientation_Horizontal()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenCheckBoxList<int>>(parameters =>
            {
                parameters.Add(p => p.Orientation, Orientation.Horizontal);
            });

            Assert.Contains("rz-flex-row", component.Markup);
        }

        [Fact]
        public void CheckBoxList_Renders_Orientation_Vertical()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenCheckBoxList<int>>(parameters =>
            {
                parameters.Add(p => p.Orientation, Orientation.Vertical);
            });

            Assert.Contains("rz-flex-column", component.Markup);
        }

        [Fact]
        public void CheckBoxList_Renders_Disabled()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenCheckBoxList<int>>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
            });

            Assert.Contains("disabled", component.Markup);
            Assert.Contains("rz-state-disabled", component.Markup);
        }

        [Fact]
        public void CheckBoxList_Renders_AllowSelectAll()
        {
            using var ctx = new TestContext();
            var data = new List<string> { "Option 1", "Option 2" };

            var component = ctx.RenderComponent<RadzenCheckBoxList<string>>(parameters =>
            {
                parameters.Add(p => p.AllowSelectAll, true);
                parameters.Add(p => p.Data, data);
            });

            Assert.Contains("rz-multiselect-header", component.Markup);
        }

        [Fact]
        public void CheckBoxList_Renders_SelectAllText()
        {
            using var ctx = new TestContext();
            var data = new List<string> { "Option 1", "Option 2" };

            var component = ctx.RenderComponent<RadzenCheckBoxList<string>>(parameters =>
            {
                parameters.Add(p => p.AllowSelectAll, true);
                parameters.Add(p => p.SelectAllText, "Select All Options");
                parameters.Add(p => p.Data, data);
            });

            Assert.Contains("Select All Options", component.Markup);
        }

        [Fact]
        public void CheckBoxList_Renders_CheckboxInputs()
        {
            using var ctx = new TestContext();
            var data = new List<string> { "Option 1", "Option 2" };

            var component = ctx.RenderComponent<RadzenCheckBoxList<string>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
            });

            Assert.Contains("type=\"checkbox\"", component.Markup);
            Assert.Contains("rz-chkbox", component.Markup);
        }

        [Fact]
        public void CheckBoxList_Renders_DisabledItems()
        {
            using var ctx = new TestContext();
            var data = new List<Item>
            {
                new Item { Id = 1, Name = "Enabled", Disabled = false },
                new Item { Id = 2, Name = "Disabled", Disabled = true }
            };

            var component = ctx.RenderComponent<RadzenCheckBoxList<int>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.TextProperty, "Name");
                parameters.Add(p => p.ValueProperty, "Id");
                parameters.Add(p => p.DisabledProperty, "Disabled");
            });

            Assert.Contains("Enabled", component.Markup);
            Assert.Contains("Disabled", component.Markup);
        }
    }
}


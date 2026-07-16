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

        [Fact]
        public void CheckBoxList_Renders_GroupRole()
        {
            using var ctx = new TestContext();
            var data = new List<string> { "Option 1", "Option 2" };

            var component = ctx.RenderComponent<RadzenCheckBoxList<string>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
            });

            var group = component.Find("div.rz-checkbox-list");

            Assert.Equal("group", group.GetAttribute("role"));
            Assert.DoesNotContain("checkboxgroup", component.Markup);
        }

        [Fact]
        public void CheckBoxList_Renders_GroupAccessibleName()
        {
            using var ctx = new TestContext();
            var data = new List<string> { "Option 1", "Option 2" };

            var component = ctx.RenderComponent<RadzenCheckBoxList<string>>(parameters =>
            {
                parameters.Add(p => p.Name, "Notifications");
                parameters.Add(p => p.Data, data);
            });

            var group = component.Find("div.rz-checkbox-list");

            Assert.Equal("Notifications", group.GetAttribute("aria-label"));
        }

        [Fact]
        public void CheckBoxList_Renders_CheckboxRoleAndState()
        {
            using var ctx = new TestContext();
            var data = new List<string> { "Option 1", "Option 2" };

            var component = ctx.RenderComponent<RadzenCheckBoxList<string>>(parameters =>
            {
                parameters.Add(p => p.Value, new List<string> { "Option 1" });
                parameters.Add(p => p.Data, data);
            });

            var options = component.FindAll("div[role=\"checkbox\"]");

            Assert.Equal(2, options.Count);
            Assert.Equal("true", options[0].GetAttribute("aria-checked"));
            Assert.Equal("false", options[1].GetAttribute("aria-checked"));
            Assert.Equal("Option 1", options[0].GetAttribute("aria-label"));
        }

        [Fact]
        public void CheckBoxList_Updates_AriaChecked_OnSelect()
        {
            using var ctx = new TestContext();
            var data = new List<string> { "Option 1", "Option 2" };

            var component = ctx.RenderComponent<RadzenCheckBoxList<string>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
            });

            var option = component.FindAll("div[role=\"checkbox\"]")[0];
            Assert.Equal("false", option.GetAttribute("aria-checked"));

            option.Click();

            Assert.Equal("true", component.FindAll("div[role=\"checkbox\"]")[0].GetAttribute("aria-checked"));
        }

        [Fact]
        public void CheckBoxList_Sets_ActiveDescendant_OnFocus()
        {
            using var ctx = new TestContext();
            var data = new List<string> { "Option 1", "Option 2" };

            var component = ctx.RenderComponent<RadzenCheckBoxList<string>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
            });

            var group = component.Find("div.rz-checkbox-list");

            Assert.True(string.IsNullOrEmpty(group.GetAttribute("aria-activedescendant")));

            group.Focus();

            var firstId = component.FindAll("div[role=\"checkbox\"]")[0].GetAttribute("id");

            Assert.Equal(firstId, component.Find("div.rz-checkbox-list").GetAttribute("aria-activedescendant"));
        }

        [Fact]
        public void CheckBoxList_Moves_ActiveDescendant_OnArrowKey()
        {
            using var ctx = new TestContext();
            var data = new List<string> { "Option 1", "Option 2", "Option 3" };

            var component = ctx.RenderComponent<RadzenCheckBoxList<string>>(parameters =>
            {
                parameters.Add(p => p.Orientation, Orientation.Horizontal);
                parameters.Add(p => p.Data, data);
            });

            var group = component.Find("div.rz-checkbox-list");
            group.Focus();
            group.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowRight" });

            var secondId = component.FindAll("div[role=\"checkbox\"]")[1].GetAttribute("id");

            Assert.Equal(secondId, component.Find("div.rz-checkbox-list").GetAttribute("aria-activedescendant"));
        }

        [Fact]
        public void CheckBoxList_NullableValue_RendersAndSelectsItemsDeclaredWithNonNullableValueType()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            IEnumerable<int?> value = null;

            var component = ctx.RenderComponent<RadzenCheckBoxList<int?>>(parameters =>
            {
                parameters.Add(p => p.ValueChanged, (IEnumerable<int?> v) => value = v);
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenCheckBoxListItem<int>>(0);
                    builder.AddAttribute(1, "Text", "Orders");
                    builder.AddAttribute(2, "Value", 1);
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenCheckBoxListItem<int>>(3);
                    builder.AddAttribute(4, "Text", "Employees");
                    builder.AddAttribute(5, "Value", 2);
                    builder.CloseComponent();
                });
            });

            var checkboxes = component.FindAll("[role=checkbox]");
            Assert.Equal(2, checkboxes.Count);

            checkboxes[0].Click();

            Assert.Contains((int?)1, value);
            Assert.Equal("true", component.FindAll("[role=checkbox]")[0].GetAttribute("aria-checked"));
        }
    }
}


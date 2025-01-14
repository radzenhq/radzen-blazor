using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DropDownTests
    {
        class DataItem
        {
            public string Text { get; set; }
            public int Id { get; set; }
        }

        private static IRenderedComponent<RadzenDropDown<T>> DropDown<T>(TestContext ctx, Action<ComponentParameterCollectionBuilder<RadzenDropDown<T>>> configure = null)
        {
            var data = new [] {
                new DataItem { Text = "Item 1", Id = 1 },
                new DataItem { Text = "Item 2", Id = 2 },
            };

            var component = ctx.RenderComponent<RadzenDropDown<T>>();

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.TextProperty, nameof(DataItem.Text));

                if (configure != null)
                {
                    configure.Invoke(parameters);
                }
                else
                {
                    parameters.Add(p => p.ValueProperty, nameof(DataItem.Id));
                }
            });

            return component;
        }

        [Fact]
        public async Task Dropdown_SelectItem_Method_Should_Not_Throw()
        {
            using var ctx = new TestContext();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = DropDown<int>(ctx);

            var items = component.FindAll(".rz-dropdown-item");

            Assert.Equal(2, items.Count);

            //this throws
            await component.InvokeAsync(async () => await component.Instance.SelectItem(1));
        }

        [Fact]
        public void DropDown_RendersItems()
        {
            using var ctx = new TestContext();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = DropDown<int>(ctx);

            var items = component.FindAll(".rz-dropdown-item");

            Assert.Equal(2, items.Count);
        }

        [Fact]
        public void DropDown_AppliesSelectionStyleForIntValue()
        {
            using var ctx = new TestContext();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = DropDown<int>(ctx);

            var items = component.FindAll(".rz-dropdown-item");

            items[0].Click();

            component.Render();

            items = component.FindAll(".rz-dropdown-item");

            Assert.Contains("rz-state-highlight", items[0].ClassList);
        }

        [Fact]
        public void DropDown_AppliesSelectionStyleForStringValue()
        {
            using var ctx = new TestContext();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = DropDown<string>(ctx, parameters => {
                parameters.Add(p => p.ValueProperty, nameof(DataItem.Text));
            });

            var items = component.FindAll(".rz-dropdown-item");

            items[0].Click();

            component.Render();

            items = component.FindAll(".rz-dropdown-item");

            Assert.Contains("rz-state-highlight", items[0].ClassList);
        }

        [Fact]
        public void DropDown_Respects_ItemEqualityComparer()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            List<DataItem> boundCollection = [new() { Text = "Item 2" }];

            var component = DropDown<string>(ctx, parameters => {
                parameters.Add(p => p.ItemComparer, new DataItemComparer());
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.Value, boundCollection);
            });

            var selectedItems = component.FindAll(".rz-state-highlight");
            Assert.Equal(1, selectedItems.Count);
            Assert.Equal("Item 2", selectedItems[0].TextContent.Trim());

            // select Item 1 in list
            var items = component.FindAll(".rz-multiselect-item");
            items[0].Click();
            component.Render();

            selectedItems = component.FindAll(".rz-state-highlight");
            Assert.Equal(2, selectedItems.Count);
            Assert.Equal("Item 1", selectedItems[0].TextContent.Trim());
        }

        [Fact]
        public void DropDown_AppliesSelectionStyleWhenMultipleSelectionIsEnabled()
        {
            using var ctx = new TestContext();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = DropDown<string>(ctx, parameters => {
                parameters.Add(p => p.ValueProperty, nameof(DataItem.Text));
                parameters.Add(p => p.Multiple, true);
            });

            var items = component.FindAll(".rz-multiselect-item");

            items[0].Click();

            component.Render();

            items = component.FindAll(".rz-multiselect-item");

            items[1].Click();

            component.Render();

            var selectedItems = component.FindAll(".rz-state-highlight");

            Assert.Equal(2, selectedItems.Count);
        }

        [Fact]
        public void DropDown_AppliesValueTemplateOnMultipleSelection()
        {
            using var ctx = new TestContext();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var valueTemplateFragment = (RenderFragment<dynamic>)(_context =>
            builder =>
            {
                builder.AddContent(0, $"value: {_context.Text}");
            });

            var component = DropDown<string>(ctx, parameters =>
            {
                parameters.Add(p => p.Multiple, true)
                    .Add(p => p.ValueTemplate, valueTemplateFragment);
            });

            var items = component.FindAll(".rz-multiselect-item");

            items[0].Click();
            items[1].Click();

            component.Render();

            var selectedItems = component.Find(".rz-inputtext");

            Assert.Contains("value: Item 1,value: Item 2", selectedItems.Text());
        }

        [Fact]
        public void DropDown_AppliesValueTemplateWhenTepmlateDefined()
        {
            using var ctx = new TestContext();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var valueTemplateFragment = (RenderFragment<dynamic>)(_context =>
            builder =>
            {
                builder.AddContent(0, $"value: {_context.Text}");
            });

            var templateFragment = (RenderFragment<dynamic>)(_context =>
            builder =>
            {
                builder.AddContent(0, $"template: {_context.Text}");
            });

            var component = DropDown<string>(ctx, parameters =>
            {
                parameters.Add(p => p.Multiple, true)
                    .Add(p => p.ValueTemplate, valueTemplateFragment)
                    .Add(p => p.Template, templateFragment);
            });

            var items = component.FindAll(".rz-multiselect-item");

            items[0].Click();
            items[1].Click();

            component.Render();

            var selectedItems = component.Find(".rz-inputtext");
            var itemsText = component.FindAll(".rz-multiselect-item-content");

            Assert.Collection(itemsText, item => Assert.Contains("template: Item 1", item.Text()), item => Assert.Contains("template: Item 2", item.Text()));
            Assert.Contains("value: Item 1,value: Item 2", selectedItems.Text());
        }

        [Fact]
        public void DropDown_AppliesValueTemplateOnMultipleSelectionChips()
        {
            using var ctx = new TestContext();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var valueTemplateFragment = (RenderFragment<dynamic>)(_context =>
            builder =>
            {
                builder.AddContent(0, $"value: {_context.Text}");
            });

            var component = DropDown<string>(ctx, parameters =>
            {
                parameters.Add(p => p.Multiple, true)
                    .Add(p => p.ValueTemplate, valueTemplateFragment)
                    .Add(p => p.Chips, true);
            });

            var items = component.FindAll(".rz-multiselect-item");

            items[0].Click();
            items[1].Click();

            component.Render();

            var selectedItems = component.FindAll(".rz-chip-text");

            Assert.Collection(selectedItems, item => Assert.Contains("value: Item 1", item.Text()), item => Assert.Contains("value: Item 2", item.Text()));
        }


        class DataItemComparer : IEqualityComparer<DataItem>, IEqualityComparer<object>
        {
            public bool Equals(DataItem x, DataItem y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x is null) return false;
                if (y is null) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Text == y.Text;
            }

            public int GetHashCode(DataItem obj)
            {
                return obj.Text.GetHashCode();
            }

            public new bool Equals(object x, object y)
            {
                return Equals((DataItem)x, (DataItem)y);
            }

            public int GetHashCode(object obj)
            {
                return GetHashCode((DataItem)obj);

            }
        }
    }
}

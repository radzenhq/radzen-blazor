using System;
using System.Threading.Tasks;
using Bunit;
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
    }
}
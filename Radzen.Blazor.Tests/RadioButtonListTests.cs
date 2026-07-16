using Bunit;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class RadioButtonListTests
    {
        private static void AddThreeItems(ComponentParameterCollectionBuilder<RadzenRadioButtonList<int>> parameters)
        {
            parameters.Add(p => p.Items, builder =>
            {
                builder.OpenComponent<RadzenRadioButtonListItem<int>>(0);
                builder.AddAttribute(1, "Text", "Option A");
                builder.AddAttribute(2, "Value", 1);
                builder.CloseComponent();

                builder.OpenComponent<RadzenRadioButtonListItem<int>>(3);
                builder.AddAttribute(4, "Text", "Option B");
                builder.AddAttribute(5, "Value", 2);
                builder.CloseComponent();

                builder.OpenComponent<RadzenRadioButtonListItem<int>>(6);
                builder.AddAttribute(7, "Text", "Option C");
                builder.AddAttribute(8, "Value", 3);
                builder.CloseComponent();
            });
        }

        [Fact]
        public void RadioButtonList_Renders_RadioGroupRole()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenRadioButtonList<int>>(AddThreeItems);

            var group = component.Find("[role=radiogroup]");
            Assert.Equal("radiogroup", group.GetAttribute("role"));

            var radios = component.FindAll("[role=radio]");
            Assert.Equal(3, radios.Count);
            Assert.All(radios, radio => Assert.NotNull(radio.GetAttribute("aria-checked")));
        }

        [Fact]
        public void RadioButtonList_Renders_AriaCheckedReflectsValue()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenRadioButtonList<int>>(parameters =>
            {
                parameters.Add(p => p.Value, 2);
                AddThreeItems(parameters);
            });

            var radios = component.FindAll("[role=radio]");
            Assert.Equal("false", radios[0].GetAttribute("aria-checked"));
            Assert.Equal("true", radios[1].GetAttribute("aria-checked"));
            Assert.Equal("false", radios[2].GetAttribute("aria-checked"));
        }

        [Fact]
        public void RadioButtonList_Sets_GroupAriaLabelFromName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenRadioButtonList<int>>(parameters =>
            {
                parameters.Add(p => p.Name, "Shipping options");
                AddThreeItems(parameters);
            });

            var group = component.Find("[role=radiogroup]");
            Assert.Equal("Shipping options", group.GetAttribute("aria-label"));
        }

        [Fact]
        public void RadioButtonList_Exposes_ActiveDescendantOnFocus()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenRadioButtonList<int>>(parameters =>
            {
                parameters.Add(p => p.Value, 1);
                AddThreeItems(parameters);
            });

            var group = component.Find("[role=radiogroup]");
            group.Focus();

            group = component.Find("[role=radiogroup]");
            var active = group.GetAttribute("aria-activedescendant");
            Assert.False(string.IsNullOrEmpty(active));

            var radios = component.FindAll("[role=radio]");
            Assert.Equal(active, radios[0].GetAttribute("id"));
        }

        [Fact]
        public void RadioButtonList_Arrow_MovesActiveDescendantAndChecks()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var value = 1;
            var component = ctx.RenderComponent<RadzenRadioButtonList<int>>(parameters =>
            {
                parameters.Add(p => p.Value, value);
                parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<int>(this, v => value = v));
                AddThreeItems(parameters);
            });

            var group = component.Find("[role=radiogroup]");
            group.Focus();

            group = component.Find("[role=radiogroup]");
            group.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowRight" });

            Assert.Equal(2, value);

            var radios = component.FindAll("[role=radio]");
            Assert.Equal("true", radios[1].GetAttribute("aria-checked"));

            group = component.Find("[role=radiogroup]");
            Assert.Equal(radios[1].GetAttribute("id"), group.GetAttribute("aria-activedescendant"));
        }

        [Fact]
        public void RadioButtonList_Arrow_WrapsAtEnds()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var value = 1;
            var component = ctx.RenderComponent<RadzenRadioButtonList<int>>(parameters =>
            {
                parameters.Add(p => p.Value, value);
                parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<int>(this, v => value = v));
                AddThreeItems(parameters);
            });

            var group = component.Find("[role=radiogroup]");
            group.Focus();

            group = component.Find("[role=radiogroup]");
            group.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowLeft" });

            Assert.Equal(3, value);
        }

        [Fact]
        public void RadioButtonList_Arrow_BothAxesWork()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var value = 1;
            var component = ctx.RenderComponent<RadzenRadioButtonList<int>>(parameters =>
            {
                parameters.Add(p => p.Orientation, Orientation.Horizontal);
                parameters.Add(p => p.Value, value);
                parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<int>(this, v => value = v));
                AddThreeItems(parameters);
            });

            var group = component.Find("[role=radiogroup]");
            group.Focus();

            group = component.Find("[role=radiogroup]");
            group.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowDown" });

            Assert.Equal(2, value);
        }

        [Fact]
        public void RadioButtonList_HomeEnd_SelectFirstAndLast()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var value = 2;
            var component = ctx.RenderComponent<RadzenRadioButtonList<int>>(parameters =>
            {
                parameters.Add(p => p.Value, value);
                parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<int>(this, v => value = v));
                AddThreeItems(parameters);
            });

            var group = component.Find("[role=radiogroup]");
            group.Focus();

            group = component.Find("[role=radiogroup]");
            group.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "End" });
            Assert.Equal(3, value);

            group = component.Find("[role=radiogroup]");
            group.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "Home" });
            Assert.Equal(1, value);
        }

        [Fact]
        public void RadioButtonList_Arrow_SkipsDisabledItems()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var value = 1;
            var component = ctx.RenderComponent<RadzenRadioButtonList<int>>(parameters =>
            {
                parameters.Add(p => p.Value, value);
                parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<int>(this, v => value = v));
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenRadioButtonListItem<int>>(0);
                    builder.AddAttribute(1, "Text", "Option A");
                    builder.AddAttribute(2, "Value", 1);
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenRadioButtonListItem<int>>(3);
                    builder.AddAttribute(4, "Text", "Option B");
                    builder.AddAttribute(5, "Value", 2);
                    builder.AddAttribute(6, "Disabled", true);
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenRadioButtonListItem<int>>(7);
                    builder.AddAttribute(8, "Text", "Option C");
                    builder.AddAttribute(9, "Value", 3);
                    builder.CloseComponent();
                });
            });

            var group = component.Find("[role=radiogroup]");
            group.Focus();

            group = component.Find("[role=radiogroup]");
            group.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowRight" });

            Assert.Equal(3, value);
        }

        [Fact]
        public void RadioButtonList_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRadioButtonList<int>>();

            Assert.Contains(@"rz-radio-button-list", component.Markup);
        }

        [Fact]
        public void RadioButtonList_Renders_Orientation()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRadioButtonList<int>>(parameters =>
            {
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenRadioButtonListItem<int>>(0);
                    builder.AddAttribute(1, "Text", "Option 1");
                    builder.AddAttribute(2, "Value", 1);
                    builder.CloseComponent();
                });
            });

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Orientation, Orientation.Horizontal));
            // Orientation is applied via RadzenStack which uses flex-direction
            Assert.Contains("rz-flex-row", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Orientation, Orientation.Vertical));
            Assert.Contains("rz-flex-column", component.Markup);
        }

        [Fact]
        public void RadioButtonList_Renders_Disabled()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRadioButtonList<int>>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenRadioButtonListItem<int>>(0);
                    builder.AddAttribute(1, "Text", "Option 1");
                    builder.AddAttribute(2, "Value", 1);
                    builder.CloseComponent();
                });
            });

            // Disabled class is on the radio button items
            Assert.Contains("rz-state-disabled", component.Markup);
        }

        [Fact]
        public void RadioButtonList_Renders_Items()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRadioButtonList<int>>(parameters =>
            {
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenRadioButtonListItem<int>>(0);
                    builder.AddAttribute(1, "Text", "Option A");
                    builder.AddAttribute(2, "Value", 1);
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenRadioButtonListItem<int>>(3);
                    builder.AddAttribute(4, "Text", "Option B");
                    builder.AddAttribute(5, "Value", 2);
                    builder.CloseComponent();
                });
            });

            Assert.Contains("Option A", component.Markup);
            Assert.Contains("Option B", component.Markup);
        }

        [Fact]
        public void RadioButtonList_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRadioButtonList<int>>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("rz-radio-button-list", component.Markup);
        }

        [Fact]
        public void RadioButtonList_Renders_StyleParameter()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRadioButtonList<int>>(parameters =>
            {
                parameters.Add(p => p.Style, "margin:1rem");
            });

            Assert.Contains("margin:1rem", component.Markup);
        }

        private static void AddNullableMismatchItems(ComponentParameterCollectionBuilder<RadzenRadioButtonList<bool?>> parameters)
        {
            parameters.Add(p => p.Items, builder =>
            {
                builder.OpenComponent<RadzenRadioButtonListItem<bool>>(0);
                builder.AddAttribute(1, "Text", "Yes");
                builder.AddAttribute(2, "Value", true);
                builder.CloseComponent();

                builder.OpenComponent<RadzenRadioButtonListItem<bool>>(3);
                builder.AddAttribute(4, "Text", "No");
                builder.AddAttribute(5, "Value", false);
                builder.CloseComponent();
            });
        }

        [Fact]
        public void RadioButtonList_NullableValue_RendersItemsDeclaredWithNonNullableValueType()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenRadioButtonList<bool?>>(AddNullableMismatchItems);

            Assert.Equal(2, component.FindAll("[role=radio]").Count);
        }

        [Fact]
        public void RadioButtonList_NullableValue_SelectsItemDeclaredWithNonNullableValueType()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            bool? value = null;

            var component = ctx.RenderComponent<RadzenRadioButtonList<bool?>>(parameters =>
            {
                parameters.Add(p => p.Value, value);
                parameters.Add(p => p.ValueChanged, (bool? v) => value = v);
                AddNullableMismatchItems(parameters);
            });

            component.FindAll("[role=radio]")[0].Click();

            Assert.True(value);
            Assert.Equal("true", component.FindAll("[role=radio]")[0].GetAttribute("aria-checked"));
        }
    }
}


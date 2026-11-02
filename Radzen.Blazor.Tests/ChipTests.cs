using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ChipTests
    {
        [Fact]
        public void Chip_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenChip>();

            Assert.Contains("rz-chip", component.Markup);
        }

        [Fact]
        public void Chip_Renders_TextParameter()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenChip>(parameters =>
            {
                parameters.Add(p => p.Text, "Hello");
            });

            Assert.Contains("Hello", component.Markup);
            Assert.Contains("rz-chip-text", component.Markup);
        }

        [Fact]
        public void Chip_Renders_IconParameter()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenChip>(parameters =>
            {
                parameters.Add(p => p.Icon, "home");
            });

            Assert.Contains("rz-chip-icon", component.Markup);
            Assert.Contains("home", component.Markup);
        }

        [Fact]
        public void Chip_Renders_ChildContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenChip>(parameters =>
            {
                parameters.AddChildContent("CustomContent");
            });

            Assert.Contains("CustomContent", component.Markup);
        }

        [Fact]
        public void Chip_Renders_ChipStyle()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenChip>(parameters =>
            {
                parameters.Add(p => p.ChipStyle, BadgeStyle.Success);
            });

            Assert.Contains("rz-chip-success", component.Markup);
        }

        [Fact]
        public void Chip_Renders_Variant()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenChip>(parameters =>
            {
                parameters.Add(p => p.Variant, Variant.Outlined);
            });

            Assert.Contains("rz-variant-outlined", component.Markup);
        }

        [Fact]
        public void Chip_Renders_SmallSize()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenChip>(parameters =>
            {
                parameters.Add(p => p.Size, ChipSize.Small);
            });

            Assert.Contains("rz-chip-sm", component.Markup);
        }

        [Fact]
        public void Chip_Renders_ExtraSmallSize()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenChip>(parameters =>
            {
                parameters.Add(p => p.Size, ChipSize.ExtraSmall);
            });

            Assert.Contains("rz-chip-xs", component.Markup);
        }

        [Fact]
        public void Chip_Renders_SelectedState()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenChip>(parameters =>
            {
                parameters.Add(p => p.Selected, true);
            });

            Assert.Contains("rz-chip-selected", component.Markup);
        }

        [Fact]
        public void Chip_Renders_DisabledState()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenChip>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
            });

            Assert.Contains("rz-state-disabled", component.Markup);
            Assert.Contains(@"aria-disabled=""true""", component.Markup);
        }

        [Fact]
        public void Chip_Renders_TabIndex()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenChip>(parameters =>
            {
                parameters.Add(p => p.TabIndex, 5);
            });

            Assert.Contains(@"tabindex=""5""", component.Markup);
        }

        [Fact]
        public void Chip_Disabled_TabIndex_IsMinusOne()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenChip>(parameters =>
            {
                parameters.Add(p => p.TabIndex, 5);
                parameters.Add(p => p.Disabled, true);
            });

            Assert.Contains(@"tabindex=""-1""", component.Markup);
        }

        [Fact]
        public void Chip_Renders_CloseButton_WhenCloseHasDelegate()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenChip>(parameters =>
            {
                parameters.Add(p => p.Close, new Microsoft.AspNetCore.Components.EventCallback<MouseEventArgs>(null, () => { }));
            });

            Assert.Contains("close", component.Markup);
        }

        [Fact]
        public void Chip_DoesNotRender_CloseButton_WhenNoCloseDelegate()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenChip>(parameters =>
            {
                parameters.Add(p => p.Text, "Tag");
            });

            // Should not have a close button when Close has no delegate
            var buttons = component.FindAll("button");
            Assert.Empty(buttons);
        }

        [Fact]
        public void Chip_Renders_RemoveChipTitle()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenChip>(parameters =>
            {
                parameters.Add(p => p.RemoveChipTitle, "Delete this");
                parameters.Add(p => p.Close, new Microsoft.AspNetCore.Components.EventCallback<MouseEventArgs>(null, () => { }));
            });

            Assert.Contains("Delete this", component.Markup);
        }

        [Fact]
        public void Chip_Click_DoesNotFire_WhenDisabled()
        {
            using var ctx = new TestContext();
            var clicked = false;

            var component = ctx.RenderComponent<RadzenChip>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
                parameters.Add(p => p.Click, new Microsoft.AspNetCore.Components.EventCallback<MouseEventArgs>(null, () => { clicked = true; }));
            });

            var instance = component.Instance;
            instance.OnClick(new MouseEventArgs()).GetAwaiter().GetResult();

            Assert.False(clicked);
        }

        [Fact]
        public void Chip_Close_DoesNotFire_WhenDisabled()
        {
            using var ctx = new TestContext();
            var closed = false;

            var component = ctx.RenderComponent<RadzenChip>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
                parameters.Add(p => p.Close, new Microsoft.AspNetCore.Components.EventCallback<MouseEventArgs>(null, () => { closed = true; }));
            });

            var instance = component.Instance;
            instance.OnCloseClick(new MouseEventArgs()).GetAwaiter().GetResult();

            Assert.False(closed);
        }

        [Fact]
        public void Chip_KeyDown_Enter_InvokesClick()
        {
            using var ctx = new TestContext();
            var clicked = false;

            var component = ctx.RenderComponent<RadzenChip>(parameters =>
            {
                parameters.Add(p => p.Click, new Microsoft.AspNetCore.Components.EventCallback<MouseEventArgs>(null, () => { clicked = true; }));
            });

            var instance = component.Instance;
            instance.OnKeyDown(new KeyboardEventArgs { Key = "Enter" }).GetAwaiter().GetResult();

            Assert.True(clicked);
        }

        [Fact]
        public void Chip_KeyDown_Space_InvokesClick()
        {
            using var ctx = new TestContext();
            var clicked = false;

            var component = ctx.RenderComponent<RadzenChip>(parameters =>
            {
                parameters.Add(p => p.Click, new Microsoft.AspNetCore.Components.EventCallback<MouseEventArgs>(null, () => { clicked = true; }));
            });

            var instance = component.Instance;
            instance.OnKeyDown(new KeyboardEventArgs { Key = "Space" }).GetAwaiter().GetResult();

            Assert.True(clicked);
        }

        [Fact]
        public void Chip_KeyDown_Delete_InvokesClose()
        {
            using var ctx = new TestContext();
            var closed = false;

            var component = ctx.RenderComponent<RadzenChip>(parameters =>
            {
                parameters.Add(p => p.Close, new Microsoft.AspNetCore.Components.EventCallback<MouseEventArgs>(null, () => { closed = true; }));
            });

            var instance = component.Instance;
            instance.OnKeyDown(new KeyboardEventArgs { Key = "Delete" }).GetAwaiter().GetResult();

            Assert.True(closed);
        }

        [Fact]
        public void Chip_KeyDown_Backspace_InvokesClose()
        {
            using var ctx = new TestContext();
            var closed = false;

            var component = ctx.RenderComponent<RadzenChip>(parameters =>
            {
                parameters.Add(p => p.Close, new Microsoft.AspNetCore.Components.EventCallback<MouseEventArgs>(null, () => { closed = true; }));
            });

            var instance = component.Instance;
            instance.OnKeyDown(new KeyboardEventArgs { Key = "Backspace" }).GetAwaiter().GetResult();

            Assert.True(closed);
        }

        [Fact]
        public void Chip_KeyDown_Disabled_DoesNotInvokeClick()
        {
            using var ctx = new TestContext();
            var clicked = false;

            var component = ctx.RenderComponent<RadzenChip>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
                parameters.Add(p => p.Click, new Microsoft.AspNetCore.Components.EventCallback<MouseEventArgs>(null, () => { clicked = true; }));
            });

            var instance = component.Instance;
            instance.OnKeyDown(new KeyboardEventArgs { Key = "Enter" }).GetAwaiter().GetResult();

            Assert.False(clicked);
        }

        [Fact]
        public void Chip_Renders_RoleButton()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenChip>();

            Assert.Contains(@"role=""button""", component.Markup);
        }

        [Fact]
        public void Chip_Renders_Shade()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenChip>(parameters =>
            {
                parameters.Add(p => p.Shade, Shade.Lighter);
            });

            Assert.Contains("rz-shade-lighter", component.Markup);
        }

        [Fact]
        public void Chip_DefaultStyle_IsBase()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenChip>();

            Assert.Contains("rz-chip-base", component.Markup);
        }
    

        [Fact]
        public void Chip_Keydown_DoesNotRenderPreventDefault()
        {
            using var ctx = new TestContext();
            var clicked = 0;
            var component = ctx.RenderComponent<RadzenChip>(parameters => parameters
                .Add(p => p.Text, "Hello")
                .Add(p => p.Click, args => clicked++));

            var chip = component.Find(".rz-chip");
            Assert.Equal("button", chip.GetAttribute("role"));
            Assert.Equal("0", chip.GetAttribute("tabindex"));
            Assert.DoesNotContain(chip.Attributes, attr => attr.Name.Contains("preventdefault", System.StringComparison.OrdinalIgnoreCase));

            chip.KeyDown(new KeyboardEventArgs { Code = "Enter" });

            Assert.Equal(1, clicked);
            chip = component.Find(".rz-chip");
            Assert.DoesNotContain(chip.Attributes, attr => attr.Name.Contains("preventdefault", System.StringComparison.OrdinalIgnoreCase));
        }
    

        [Fact]
        public void Chip_RemoveAffordance_IsNotFocusable_AndRaisesCloseWithoutClick()
        {
            using var ctx = new TestContext();
            var clicked = 0;
            var closed = 0;

            var component = ctx.RenderComponent<RadzenChip>(parameters => parameters
                .Add(p => p.Text, "Tag")
                .Add(p => p.RemoveChipTitle, "Remove tag")
                .Add(p => p.Click, args => clicked++)
                .Add(p => p.Close, args => closed++));

            var chip = component.Find(".rz-chip");

            Assert.Empty(chip.QuerySelectorAll("button, [tabindex]"));

            var remove = component.Find(".rz-chip-remove");

            Assert.Equal("true", remove.GetAttribute("aria-hidden"));
            Assert.Equal("Remove tag", remove.GetAttribute("title"));

            remove.Click();

            Assert.Equal(1, closed);
            Assert.Equal(0, clicked);
        }
    }
}

using Bunit;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor.Tests
{
    public class ChipListTests
    {
        class SelfRenderingChipList<TValue> : RadzenChipList<TValue>
        {
            public Task Rerender() => InvokeAsync(StateHasChanged);
        }

        [Fact]
        public async Task ChipList_SameCountValueMutation_ReflectedOnInternalRender()
        {
            using var ctx = new TestContext();
            var value = new List<int> { 1, 2 };

            var component = ctx.RenderComponent<SelfRenderingChipList<IEnumerable<int>>>(parameters =>
            {
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.Value, value);
                parameters.Add(p => p.Items, builder =>
                {
                    for (var i = 1; i <= 3; i++)
                    {
                        builder.OpenComponent<RadzenChipItem>(i * 3);
                        builder.AddAttribute(i * 3 + 1, "Text", $"Option {i}");
                        builder.AddAttribute(i * 3 + 2, "Value", i);
                        builder.CloseComponent();
                    }
                });
            });

            string Selected(int i) => component.FindAll("[role=option]")[i].GetAttribute("aria-selected");

            Assert.Equal("true", Selected(0));
            Assert.Equal("false", Selected(2));

            value[0] = 3;
            await component.Instance.Rerender();

            Assert.Equal("false", Selected(0));
            Assert.Equal("true", Selected(1));
            Assert.Equal("true", Selected(2));
        }

        [Theory]
        [InlineData(true, "true")]
        [InlineData(false, "false")]
        public void ChipList_Renders_AriaMultiselectable(bool multiple, string expected)
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenChipList<IEnumerable<string>>>(parameters =>
            {
                parameters.Add(p => p.Multiple, multiple);
                parameters.Add(p => p.Data, new List<string> { "Option 1", "Option 2" });
            });

            Assert.Equal(expected, component.Find("[role=listbox]").GetAttribute("aria-multiselectable"));
        }

        [Fact]
        public void ChipList_Renders_UniqueOptionIds()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenChipList<string>>(parameters =>
            {
                parameters.Add(p => p.Data, new List<string> { "Option 1", "Option 2", "Option 3" });
            });

            var ids = component.FindAll("[role=option]").Select(o => o.GetAttribute("id")).ToList();

            Assert.Equal(3, ids.Count);
            Assert.All(ids, id => Assert.False(string.IsNullOrEmpty(id)));
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }

        [Fact]
        public void ChipList_Sets_ActiveDescendant_OnFocus()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenChipList<string>>(parameters =>
            {
                parameters.Add(p => p.Data, new List<string> { "Option 1", "Option 2" });
            });

            var listbox = component.Find("[role=listbox]");

            Assert.False(listbox.HasAttribute("aria-activedescendant"));

            listbox.Focus();

            var firstId = component.FindAll("[role=option]")[0].GetAttribute("id");

            Assert.False(string.IsNullOrEmpty(firstId));
            Assert.Equal(firstId, component.Find("[role=listbox]").GetAttribute("aria-activedescendant"));

            component.Find("[role=listbox]").Blur();

            Assert.False(component.Find("[role=listbox]").HasAttribute("aria-activedescendant"));
        }

        [Fact]
        public void ChipList_Moves_ActiveDescendant_OnArrowKey()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenChipList<string>>(parameters =>
            {
                parameters.Add(p => p.Orientation, Orientation.Horizontal);
                parameters.Add(p => p.Data, new List<string> { "Option 1", "Option 2", "Option 3" });
            });

            var listbox = component.Find("[role=listbox]");
            listbox.Focus();
            listbox.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowRight" });

            var options = component.FindAll("[role=option]");
            var secondId = options[1].GetAttribute("id");

            Assert.False(string.IsNullOrEmpty(secondId));
            Assert.Contains("rz-state-focused", options[1].ClassList);
            Assert.Equal(secondId, component.Find("[role=listbox]").GetAttribute("aria-activedescendant"));
        }
    

        [Fact]
        public void ChipList_Keydown_DoesNotRenderPreventDefault()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenChipList<string>>(parameters =>
            {
                parameters.Add(p => p.Orientation, Orientation.Horizontal);
                parameters.Add(p => p.Data, new List<string> { "Option 1", "Option 2", "Option 3" });
            });

            var listbox = component.Find("[role=listbox]");
            Assert.DoesNotContain(listbox.Attributes, attr => attr.Name.Contains("preventdefault", System.StringComparison.OrdinalIgnoreCase));

            listbox.Focus();
            component.Find("[role=listbox]").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowRight" });

            listbox = component.Find("[role=listbox]");
            Assert.Contains("rz-state-focused", component.FindAll("[role=option]")[1].ClassList);
            Assert.DoesNotContain(listbox.Attributes, attr => attr.Name.Contains("preventdefault", System.StringComparison.OrdinalIgnoreCase));
        }
    

        [Fact]
        public void ChipList_Options_HaveNoFocusableContent()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenChipList<IEnumerable<string>>>(parameters =>
            {
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.AllowDelete, true);
                parameters.Add(p => p.Data, new List<string> { "Option 1", "Option 2" });
            });

            var options = component.FindAll("[role=option]");

            Assert.Equal(2, options.Count);
            Assert.All(options, option =>
            {
                Assert.False(option.HasAttribute("tabindex"));
                Assert.Empty(option.QuerySelectorAll("[tabindex], [role=button], button"));
                Assert.NotNull(option.QuerySelector(".rz-chip-remove[aria-hidden=true]"));
            });
        }

        [Fact]
        public void ChipList_RemoveAffordanceClick_RaisesChipRemoved_WithoutSelecting()
        {
            using var ctx = new TestContext();
            object? removed = null;
            var changed = false;

            var component = ctx.RenderComponent<RadzenChipList<IEnumerable<string>>>(parameters =>
            {
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.AllowDelete, true);
                parameters.Add(p => p.Data, new List<string> { "Option 1", "Option 2" });
                parameters.Add(p => p.ChipRemoved, value => removed = value);
                parameters.Add(p => p.ValueChanged, value => changed = true);
            });

            component.FindAll(".rz-chip-remove")[1].Click();

            Assert.Equal("Option 2", removed);
            Assert.False(changed);
        }

        [Fact]
        public void ChipList_ClickOnOption_SetsActiveDescendant()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenChipList<string>>(parameters =>
            {
                parameters.Add(p => p.Data, new List<string> { "Option 1", "Option 2" });
            });

            component.Find("[role=listbox]").Focus();
            component.FindAll("[role=option]")[1].Click();

            var options = component.FindAll("[role=option]");

            Assert.Equal("true", options[1].GetAttribute("aria-selected"));
            Assert.Contains("rz-state-focused", options[1].ClassList);
            Assert.Equal(options[1].Id, component.Find("[role=listbox]").GetAttribute("aria-activedescendant"));
        }

        [Fact]
        public void ChipList_InsideFormField_IsLabelledByTheField()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenFormField>(parameters => parameters
                .Add(p => p.Text, "Category")
                .AddChildContent<RadzenChipList<string>>(list => list.Add(p => p.Data, new List<string> { "Option 1" })));

            var label = component.Find("label");

            Assert.False(string.IsNullOrEmpty(label.Id));
            Assert.Equal(label.Id, component.Find("[role=listbox]").GetAttribute("aria-labelledby"));
            Assert.DoesNotContain(ctx.JSInterop.Invocations, i => i.Identifier == "Radzen.linkLabel");
        }

        [Fact]
        public void ChipList_WithName_LinksLabelOnClient()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            ctx.RenderComponent<RadzenChipList<string>>(parameters =>
            {
                parameters.Add(p => p.Name, "category");
                parameters.Add(p => p.Data, new List<string> { "Option 1" });
            });

            var invocation = ctx.JSInterop.VerifyInvoke("Radzen.linkLabel");

            Assert.Equal("category", invocation.Arguments[1]);
        }
    }
}

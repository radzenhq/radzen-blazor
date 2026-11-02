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
    }
}

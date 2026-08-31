using Bunit;
using Xunit;
using System.Collections.Generic;
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
    }
}

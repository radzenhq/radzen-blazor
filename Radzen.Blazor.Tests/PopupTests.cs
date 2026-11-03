using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class PopupTests
    {
        [Fact]
        public async Task Popup_Dispose_DestroysPopup()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenPopup>(p => p
                .Add(x => x.Lazy, true)
                .AddChildContent("<span>content</span>"));

            await component.InvokeAsync(() => component.Instance.ToggleAsync(default(ElementReference)));

            ctx.DisposeComponents();

            Assert.Contains(ctx.JSInterop.Invocations, i => i.Identifier == "Radzen.destroyPopup");
        }

        [Fact]
        public void Popup_Visible_False_Renders_Nothing_And_Hiding_Destroys_The_Popup()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenPopup>(p => p
                .Add(x => x.Visible, false)
                .AddChildContent("<span>content</span>"));

            Assert.Equal(string.Empty, component.Markup.Trim());
            Assert.DoesNotContain(ctx.JSInterop.Invocations, i => i.Identifier == "Radzen.destroyPopup");

            component.SetParametersAndRender(p => p.Add(x => x.Visible, true));

            Assert.Contains("<span>content</span>", component.Markup);

            component.SetParametersAndRender(p => p.Add(x => x.Visible, false));

            Assert.Equal(string.Empty, component.Markup.Trim());
            Assert.Single(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.destroyPopup"));
        }

        [Fact]
        public async Task Popup_Visible_False_Toggle_Does_Not_Open()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenPopup>(p => p
                .Add(x => x.Visible, false)
                .AddChildContent("<span>content</span>"));

            await component.InvokeAsync(() => component.Instance.ToggleAsync(default(ElementReference)));

            Assert.False(component.Instance.IsOpen);
            Assert.DoesNotContain(ctx.JSInterop.Invocations, i => i.Identifier == "Radzen.openPopup");
        }
    }
}

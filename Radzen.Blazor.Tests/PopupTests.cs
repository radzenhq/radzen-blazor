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
            using var ctx = new BunitContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.Render<RadzenPopup>(p => p
                .Add(x => x.Lazy, true)
                .AddChildContent("<span>content</span>"));

            await component.InvokeAsync(() => component.Instance.ToggleAsync(default(ElementReference)));

            await ctx.DisposeComponentsAsync();

            Assert.Contains(ctx.JSInterop.Invocations, i => i.Identifier == "Radzen.destroyPopup");
        }
    }
}

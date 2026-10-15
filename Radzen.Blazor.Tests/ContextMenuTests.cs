using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ContextMenuTests
    {
        [Fact]
        public void ContextMenu_Open_RaisesOptions()
        {
            var service = new ContextMenuService(null);
            var options = new ContextMenuOptions { CloseOnScroll = true };
            ContextMenuOptions opened = null;
            service.OnOpen += (_, value) => opened = value;

            service.Open(new MouseEventArgs(), options);

            Assert.Same(options, opened);
        }

        [Fact]
        public void ContextMenu_CloseOnScroll_DefaultsToFalse()
        {
            Assert.False(new ContextMenuOptions().CloseOnScroll);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ContextMenu_ForwardsCloseOnScroll(bool closeOnScroll)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var service = new ContextMenuService(null);
            ctx.Services.AddSingleton(service);
            var component = ctx.RenderComponent<RadzenContextMenu>();

            service.Open(new MouseEventArgs { ClientX = 10, ClientY = 20 }, new ContextMenuOptions
            {
                Items = new List<ContextMenuItem> { new ContextMenuItem { Text = "Item" } },
                CloseOnScroll = closeOnScroll
            });

            component.WaitForAssertion(() =>
            {
                var invocation = Assert.Single(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.openContextMenu"));
                Assert.Equal(closeOnScroll, Assert.IsType<bool>(invocation.Arguments[5]));
            });
        }
    }
}

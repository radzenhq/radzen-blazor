using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class MediaQueryTests
    {
        [Fact]
        public void MediaQuery_Renders()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMediaQuery>(parameters =>
            {
                parameters.Add(p => p.Query, "(max-width: 768px)");
            });

            Assert.NotNull(component.Instance);
        }

        [Fact]
        public void MediaQuery_HasQueryParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var query = "(max-width: 1024px)";
            var component = ctx.RenderComponent<RadzenMediaQuery>(parameters =>
            {
                parameters.Add(p => p.Query, query);
            });

            Assert.Equal(query, component.Instance.Query);
        }

        [Fact]
        public void MediaQuery_InvokesChangeCallback()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            bool changeInvoked = false;
            bool matchResult = false;

            var component = ctx.RenderComponent<RadzenMediaQuery>(parameters =>
            {
                parameters.Add(p => p.Query, "(max-width: 768px)");
                parameters.Add(p => p.Change, EventCallback.Factory.Create<bool>(this, (matches) =>
                {
                    changeInvoked = true;
                    matchResult = matches;
                }));
            });

            // Invoke the JSInvokable method directly
            component.Instance.OnChange(true);

            Assert.True(changeInvoked);
            Assert.True(matchResult);
        }
    }
}


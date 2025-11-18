using Bunit;
using System.Collections.Generic;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class TocTests
    {
        [Fact]
        public void TocItem_Renders_With_Attributes()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTocItem>(parameters =>
            {
                parameters.Add(p => p.Attributes, new Dictionary<string, object>
                {
                    { "data-enhance-nav", "false" },
                    { "aria-label", "Table of Contents Item" }
                });
            });

            Assert.Contains("data-enhance-nav=\"false\"", component.Markup);
            Assert.Contains("aria-label=\"Table of Contents Item\"", component.Markup);
        }
    }
}


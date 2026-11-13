using Bunit;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class AIPromptTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RadzenAIPrompt_AllowHtml_ControlsHtmlInOutput(bool allowHtml)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddSingleton(new HttpClient());
            ctx.Services.AddScoped<IAIChatService, AIChatService>();

            var component = ctx.RenderComponent<RadzenAIPrompt>(parameters => parameters
                .Add(p => p.Output, "<b>x</b>")
                .Add(p => p.AllowHtml, allowHtml));

            if (allowHtml)
            {
                Assert.Equal("x", component.Find(".rz-aiprompt-output-content b").TextContent);
            }
            else
            {
                Assert.Empty(component.FindAll(".rz-aiprompt-output-content b"));
                Assert.Contains("&lt;b&gt;x&lt;/b&gt;", component.Markup);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ChatButtonTooltipTests
    {
        static TestContext CreateContext(bool tooltipService)
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddSingleton(new HttpClient());
            ctx.Services.AddScoped<IAIChatService, AIChatService>();

            if (tooltipService)
            {
                ctx.Services.AddScoped<TooltipService>();
            }

            return ctx;
        }

        static IRenderedComponent<RadzenAIChat> RenderAIChat(TestContext ctx, string sendButtonText = null)
        {
            var chat = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.AllowAttachments, true)
                .Add(p => p.SendButtonText, sendButtonText));

            chat.InvokeAsync(() => chat.Instance.AddAttachment(new ChatAttachment { Name = "notes.txt", MediaType = "text/plain", Data = new byte[] { 1 } }));

            return chat;
        }

        static IRenderedComponent<RadzenChat> RenderChat(TestContext ctx)
        {
            return ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, new List<ChatUser>())
                .Add(p => p.Messages, new List<ChatMessage>()));
        }

        static IRenderedComponent<RadzenButton> Button(IRenderedComponent<Microsoft.AspNetCore.Components.IComponent> chat, string cssClass)
        {
            return chat.FindComponents<RadzenButton>().Single(button => (button.Instance.Attributes?["class"] as string)?.Contains(cssClass) == true);
        }

        static readonly (string CssClass, string Hint, TooltipPosition Position)[] AIChatButtons =
        {
            ("rz-chat-header-clear", "Clear chat history", TooltipPosition.Bottom),
            ("rz-chat-scroll-end", "Scroll to the latest message", TooltipPosition.Top),
            ("rz-chat-attachment-remove", "Remove attachment", TooltipPosition.Top),
            ("rz-chat-attach-btn", "Attach image", TooltipPosition.Top),
            ("rz-chat-send-btn", "Send message", TooltipPosition.Top),
        };

        [Fact]
        public void AIChat_WithoutTooltipService_RendersItsButtonsWithTitlesAndListensToNoHover()
        {
            using var ctx = CreateContext(tooltipService: false);

            var chat = RenderAIChat(ctx);

            foreach (var (cssClass, hint, _) in AIChatButtons)
            {
                var button = chat.Find($".{cssClass}");
                Assert.Equal(hint, button.GetAttribute("title"));
                Assert.False(button.HasAttribute("aria-label"));
                Assert.False(Button(chat, cssClass).Instance.MouseEnter.HasDelegate);
                Assert.False(Button(chat, cssClass).Instance.MouseLeave.HasDelegate);
            }

            chat.Find(".rz-chat-attach-btn").Click();
            chat.Find(".rz-chat-attachment-remove").Click();

            Assert.Empty(chat.FindAll(".rz-chat-attachment-remove"));
        }

        [Fact]
        public async Task AIChat_WithTooltipServiceButNothingToDisplayTooltips_KeepsTheTitles()
        {
            using var ctx = CreateContext(tooltipService: true);
            var closed = 0;
            ctx.Services.GetRequiredService<TooltipService>().OnClose += () => closed++;

            var chat = RenderAIChat(ctx);

            foreach (var (cssClass, hint, _) in AIChatButtons)
            {
                var button = chat.Find($".{cssClass}");
                Assert.Equal(hint, button.GetAttribute("title"));
                Assert.False(button.HasAttribute("aria-label"));

                await chat.InvokeAsync(Button(chat, cssClass).Instance.RaiseMouseEnter);
                await chat.InvokeAsync(Button(chat, cssClass).Instance.RaiseMouseLeave);
            }

            chat.Find(".rz-chat-attach-btn").Click();

            Assert.Equal(0, closed);
        }

        [Fact]
        public async Task AIChat_WithTooltipServiceAndHost_ShowsRadzenTooltipsThatLeavingOrAClickCloses()
        {
            using var ctx = CreateContext(tooltipService: true);
            var opened = new List<(string Text, TooltipPosition Position)>();
            var closed = 0;
            var service = ctx.Services.GetRequiredService<TooltipService>();
            service.OnOpen += (_, _, options) => opened.Add((options.Text, options.Position));
            service.OnClose += () => closed++;

            var chat = RenderAIChat(ctx);

            foreach (var (cssClass, hint, _) in AIChatButtons)
            {
                var button = chat.Find($".{cssClass}");
                Assert.False(button.HasAttribute("title"));
                Assert.Equal(hint, button.GetAttribute("aria-label"));

                await chat.InvokeAsync(Button(chat, cssClass).Instance.RaiseMouseEnter);
                await chat.InvokeAsync(Button(chat, cssClass).Instance.RaiseMouseLeave);
            }

            Assert.Equal(AIChatButtons.Select(button => (button.Hint, button.Position)), opened);
            Assert.Equal(AIChatButtons.Length, closed);

            chat.Find(".rz-chat-attach-btn").Click();
            Assert.Equal(AIChatButtons.Length + 1, closed);

            chat.Find(".rz-chat-attachment-remove").Click();
            Assert.Equal(AIChatButtons.Length + 2, closed);

            chat.Find(".rz-chat-header-clear").Click();
            Assert.Equal(AIChatButtons.Length + 3, closed);

            chat.Find("textarea").Input("Hello");
            chat.Find(".rz-chat-send-btn").Click();
            Assert.Equal(AIChatButtons.Length + 4, closed);
        }

        [Fact]
        public async Task AIChat_PointingAtItsButtons_DoesNotRenderTheChatAgain()
        {
            using var ctx = CreateContext(tooltipService: true);
            ctx.Services.GetRequiredService<TooltipService>().OnOpen += (_, _, _) => { };

            var chat = RenderAIChat(ctx);
            var renders = chat.RenderCount;

            foreach (var (cssClass, _, _) in AIChatButtons)
            {
                await chat.InvokeAsync(Button(chat, cssClass).Instance.RaiseMouseEnter);
                await chat.InvokeAsync(Button(chat, cssClass).Instance.RaiseMouseLeave);
            }

            Assert.Equal(renders, chat.RenderCount);
        }

        [Fact]
        public async Task AIChat_SendButtonWithText_KeepsItsTextAsItsNameAndStillShowsTheTooltip()
        {
            using var ctx = CreateContext(tooltipService: true);
            var opened = new List<string>();
            ctx.Services.GetRequiredService<TooltipService>().OnOpen += (_, _, options) => opened.Add(options.Text);

            var chat = RenderAIChat(ctx, sendButtonText: "Go");

            await chat.InvokeAsync(Button(chat, "rz-chat-send-btn").Instance.RaiseMouseEnter);

            var send = chat.Find(".rz-chat-send-btn");
            Assert.False(send.HasAttribute("aria-label"));
            Assert.False(send.HasAttribute("title"));
            Assert.Equal(new[] { "Send message" }, opened);
        }

        [Fact]
        public void Chat_WithoutTooltipService_RendersItsButtonsWithTitles()
        {
            using var ctx = CreateContext(tooltipService: false);

            var chat = RenderChat(ctx);

            Assert.Equal("Clear chat history", chat.Find(".rz-chat-header-clear").GetAttribute("title"));
            Assert.Equal("Send message", chat.Find(".rz-chat-send-btn").GetAttribute("title"));
            Assert.False(Button(chat, "rz-chat-send-btn").Instance.MouseEnter.HasDelegate);
        }

        [Fact]
        public async Task Chat_WithTooltipServiceAndHost_ShowsRadzenTooltipsWithoutRenderingTheChatAgain()
        {
            using var ctx = CreateContext(tooltipService: true);
            var opened = new List<(string Text, TooltipPosition Position)>();
            var closed = 0;
            var service = ctx.Services.GetRequiredService<TooltipService>();
            service.OnOpen += (_, _, options) => opened.Add((options.Text, options.Position));
            service.OnClose += () => closed++;

            var chat = RenderChat(ctx);
            var renders = chat.RenderCount;

            foreach (var cssClass in new[] { "rz-chat-header-clear", "rz-chat-send-btn" })
            {
                Assert.False(chat.Find($".{cssClass}").HasAttribute("title"));
                await chat.InvokeAsync(Button(chat, cssClass).Instance.RaiseMouseEnter);
                await chat.InvokeAsync(Button(chat, cssClass).Instance.RaiseMouseLeave);
            }

            Assert.Equal(new[] { ("Clear chat history", TooltipPosition.Bottom), ("Send message", TooltipPosition.Top) }, opened);
            Assert.Equal(2, closed);
            Assert.Equal(renders, chat.RenderCount);
            Assert.Equal("Clear chat history", chat.Find(".rz-chat-header-clear").GetAttribute("aria-label"));
            Assert.Equal("Send message", chat.Find(".rz-chat-send-btn").GetAttribute("aria-label"));

            chat.Find(".rz-chat-header-clear").Click();
            Assert.Equal(3, closed);
        }
    }
}

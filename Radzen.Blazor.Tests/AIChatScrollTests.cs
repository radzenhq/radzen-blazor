using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class AIChatScrollTests
    {
        sealed class TwoPartChatClient : IChatClient
        {
            public Task<ChatResponse> GetResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions options = null, CancellationToken cancellationToken = default)
            {
                return GetStreamingResponseAsync(messages, options, cancellationToken).ToChatResponseAsync(cancellationToken);
            }

            public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                yield return new ChatResponseUpdate(ChatRole.Assistant, "Hello ") { ResponseId = "r0", MessageId = "r0" };
                yield return new ChatResponseUpdate(ChatRole.Assistant, "there") { ResponseId = "r0", MessageId = "r0" };
            }

            public object GetService(Type serviceType, object serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;

            public void Dispose()
            {
            }
        }

        static TestContext CreateContext()
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddSingleton<IChatClient>(new TwoPartChatClient());
            ctx.Services.AddAIChatService();
            return ctx;
        }

        static List<bool> ToEnd(TestContext ctx)
        {
            return ctx.JSInterop.Invocations["Radzen.chatScrollAfterRender"].Select(invocation => (bool)invocation.Arguments[1]).ToList();
        }

        [Fact]
        public void TheInitialRender_CallsNoScript()
        {
            using var ctx = CreateContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Strict;

            ctx.RenderComponent<RadzenAIChat>();

            Assert.Empty(ctx.JSInterop.Invocations);
        }

        [Fact]
        public void TheFirstRenderAfterTheInitialOne_MovesItsOwnMessageListToTheEndAndLaterOnesOnlyFollow()
        {
            using var ctx = CreateContext();
            var chat = ctx.RenderComponent<RadzenAIChat>();

            chat.Render();
            chat.Render();
            chat.Find("textarea").Input("Hello");

            var invocations = ctx.JSInterop.Invocations["Radzen.chatScrollAfterRender"];
            Assert.All(invocations, invocation => Assert.IsType<ElementReference>(invocation.Arguments[0]));
            Assert.All(invocations, invocation => Assert.Equal(100, invocation.Arguments[2]));
            Assert.Equal(new[] { true, false, false }, ToEnd(ctx));
        }

        [Fact]
        public async Task AMessageTheApplicationAddsForTheAssistant_OnlyFollowsTheEnd()
        {
            using var ctx = CreateContext();
            var chat = ctx.RenderComponent<RadzenAIChat>();
            chat.Render();

            await chat.InvokeAsync(() => chat.Instance.AddMessage("A status line"));

            chat.WaitForAssertion(() => Assert.Contains("A status line", chat.Markup));
            Assert.Equal(new[] { true, false }, ToEnd(ctx));
        }

        [Fact]
        public async Task AMessageTheApplicationAddsForTheUser_MovesToTheEnd()
        {
            using var ctx = CreateContext();
            var chat = ctx.RenderComponent<RadzenAIChat>();
            chat.Render();
            chat.Render();

            await chat.InvokeAsync(() => chat.Instance.AddMessage("A question", isUser: true));

            chat.WaitForAssertion(() => Assert.Contains("A question", chat.Markup));
            Assert.Equal(new[] { true, false, true }, ToEnd(ctx));
        }

        [Fact]
        public async Task SendingAMessage_MovesToTheEndOnceAndTheAnswerOnlyFollows()
        {
            using var ctx = CreateContext();
            var chat = ctx.RenderComponent<RadzenAIChat>();
            chat.Render();
            var before = ToEnd(ctx).Count;

            await chat.InvokeAsync(() => chat.Instance.SendMessage("Hi"));

            chat.WaitForAssertion(() => Assert.Contains("Hello there", chat.Markup));
            var afterSending = ToEnd(ctx).Skip(before).ToList();
            Assert.True(afterSending[0]);
            Assert.NotEmpty(afterSending.Skip(1));
            Assert.All(afterSending.Skip(1), toEnd => Assert.False(toEnd));
        }

        [Fact]
        public void TheScrollToEndButton_IsInsideTheMessageListAndMovesToTheEnd()
        {
            using var ctx = CreateContext();
            var chat = ctx.RenderComponent<RadzenAIChat>();
            chat.Render();
            chat.Render();

            chat.Find(".rz-chat-messages > .rz-chat-scroll-end").Click();

            chat.WaitForAssertion(() => Assert.Equal(new[] { true, false, true }, ToEnd(ctx)));
        }

        [Fact]
        public async Task ScrollToEnd_MovesToTheEnd()
        {
            using var ctx = CreateContext();
            var chat = ctx.RenderComponent<RadzenAIChat>();
            chat.Render();
            chat.Render();

            await chat.InvokeAsync(() => chat.Instance.ScrollToEnd());

            chat.WaitForAssertion(() => Assert.Equal(new[] { true, false, true }, ToEnd(ctx)));
        }

        [Fact]
        public async Task LoadingAConversation_MovesToTheEnd()
        {
            using var ctx = CreateContext();
            var chat = ctx.RenderComponent<RadzenAIChat>();

            chat.Render();
            chat.Render();

            await chat.InvokeAsync(() => chat.Instance.LoadMessages(new[] { new ChatMessage { Content = "Earlier", IsUser = true } }));

            chat.WaitForAssertion(() => Assert.Contains("Earlier", chat.Markup));
            Assert.Equal(new[] { true, false, true }, ToEnd(ctx));
        }
    }
}

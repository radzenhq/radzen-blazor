using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class AIChatLayoutTests
    {
        private sealed class ScriptedChatClient : IChatClient
        {
            public List<List<Microsoft.Extensions.AI.ChatMessage>> Requests { get; } = new();

            public Task<ChatResponse> GetResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            {
                return GetStreamingResponseAsync(messages, options, cancellationToken).ToChatResponseAsync(cancellationToken);
            }

            public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                var list = messages.ToList();
                Requests.Add(list);

                await Task.Yield();

                var result = list.SelectMany(message => message.Contents.OfType<FunctionResultContent>()).LastOrDefault();

                if (result != null)
                {
                    yield return new ChatResponseUpdate(ChatRole.Assistant, "Done: ") { ResponseId = "r2", MessageId = "r2" };
                    yield return new ChatResponseUpdate(ChatRole.Assistant, result.Result?.ToString() ?? string.Empty) { ResponseId = "r2", MessageId = "r2" };
                    yield break;
                }

                if (options?.Tools is { Count: > 0 } tools)
                {
                    yield return new ChatResponseUpdate(ChatRole.Assistant, "Checking【1†source】. ") { ResponseId = "r1", MessageId = "r1" };
                    yield return new ChatResponseUpdate(ChatRole.Assistant, [new FunctionCallContent("call_1", tools.OfType<AIFunctionDeclaration>().First().Name, new Dictionary<string, object?> { ["city"] = "Sofia" })]) { ResponseId = "r1", MessageId = "r1", FinishReason = ChatFinishReason.ToolCalls };
                    yield break;
                }

                if (options?.Instructions == "slow")
                {
                    for (var i = 0; i < 50; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.Delay(20, cancellationToken);

                        if (EmitUnknownMarker && i == 5)
                        {
                            yield return new ChatResponseUpdate(ChatRole.User, "leak") { ResponseId = "r0", MessageId = "nope" };
                        }

                        var unread = i >= 10 ? Queue?.Taken.FirstOrDefault(taken => !Read.Contains(taken.MessageId!)) : null;

                        if (unread != null)
                        {
                            Read.Add(unread.MessageId!);
                            yield return new ChatResponseUpdate(ChatRole.User, []) { ResponseId = "r0", MessageId = unread.MessageId };
                            yield return new ChatResponseUpdate(ChatRole.Assistant, "after ") { ResponseId = "r0", MessageId = "r1" };
                            continue;
                        }

                        yield return new ChatResponseUpdate(ChatRole.Assistant, "word ") { ResponseId = "r0", MessageId = "r0" };
                    }

                    yield break;
                }

                if (options?.Instructions == "files")
                {
                    yield return new ChatResponseUpdate(ChatRole.Assistant, "Edited the page") { ResponseId = "r0", MessageId = "r0", AdditionalProperties = new AdditionalPropertiesDictionary { ["changedFiles"] = new[] { "Pages/Home.razor" } } };
                    yield return new ChatResponseUpdate(ChatRole.Assistant, " and the layout") { ResponseId = "r0", MessageId = "r0", AdditionalProperties = new AdditionalPropertiesDictionary { ["credits"] = 2.5m } };
                    yield break;
                }

                if (options?.Instructions == "credits")
                {
                    yield return new ChatResponseUpdate(ChatRole.Assistant, "Hello there") { ResponseId = "r0", MessageId = "r0" };
                    yield return new ChatResponseUpdate(ChatRole.Assistant, [new UsageContent(new UsageDetails { TotalTokenCount = 12, AdditionalCounts = new AdditionalPropertiesDictionary<long> { ["credits"] = 3 } })]) { ResponseId = "r0", MessageId = "r0" };
                    yield break;
                }

                yield return new ChatResponseUpdate(ChatRole.Assistant, "Hello there") { ResponseId = "r0", MessageId = "r0" };
            }

            public QueueingClient? Queue { get; set; }

            public HashSet<string> Read { get; } = new();

            public bool EmitUnknownMarker { get; set; }

            public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : serviceType == typeof(IChatMessageQueue) ? Queue : null;

            public void Dispose()
            {
            }
        }

        private sealed class QueueingClient : IChatMessageQueue
        {
            public List<Microsoft.Extensions.AI.ChatMessage> Taken { get; } = new();

            public Task<bool> TryQueueMessageAsync(Microsoft.Extensions.AI.ChatMessage message, string? sessionId, CancellationToken cancellationToken = default)
            {
                Taken.Add(message);
                return Task.FromResult(true);
            }
        }

        private static (TestContext Context, ScriptedChatClient Client) CreateContext()
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var client = new ScriptedChatClient();
            ctx.Services.AddSingleton<IChatClient>(client);
            ctx.Services.AddAIChatService();
            return (ctx, client);
        }

        [Fact]
        public async Task RadzenAIChat_KeepsTextAndToolCallsInOrder()
        {
            var (ctx, _) = CreateContext();
            using var __ = ctx;

            var tool = AIFunctionFactory.Create((string city) => $"Sunny in {city}", "get_weather");

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters.Add(p => p.Tools, new AITool[] { tool }));

            await component.InvokeAsync(() => component.Instance.SendMessage("Weather?"));

            component.WaitForAssertion(() => Assert.Contains("Done: Sunny in Sofia", component.Markup));

            var assistant = component.Instance.GetMessages()[1];
            Assert.Equal("Checking[1]. Done: Sunny in Sofia", assistant.Content);

            var call = Assert.Single(assistant.ToolCalls);
            Assert.Equal("Checking[1]. ".Length, call.TextPosition);

            var parts = assistant.GetParts();
            Assert.Equal(3, parts.Count);
            Assert.Equal("Checking[1]. ", parts[0].Text);
            Assert.Same(call, parts[1].ToolCall);
            Assert.Equal("Done: Sunny in Sofia", parts[2].Text);

            var markup = component.Markup;
            var first = markup.IndexOf("Checking[1].", StringComparison.Ordinal);
            var callIndex = markup.IndexOf("rz-chat-tool-call-completed", StringComparison.Ordinal);
            var second = markup.IndexOf("Done: Sunny in Sofia", StringComparison.Ordinal);
            Assert.True(first > 0 && first < callIndex && callIndex < second, $"text {first}, call {callIndex}, text {second}");
            Assert.Equal(2, component.FindAll(".rz-chat-message-assistant .rz-chat-message-text").Count);

            var session = ctx.Services.GetRequiredService<IAIChatService>().GetOrCreateSession(component.Instance.GetSessionId());
            var stored = Assert.Single(session.Messages[1].ToolCalls);
            Assert.Equal(call.TextPosition, stored.TextPosition);
        }

        [Fact]
        public void ChatMessage_GetParts_PlacesCallsWithoutPositionFirst()
        {
            var message = new ChatMessage
            {
                Content = "Answer",
                ToolCalls = [new ChatToolCall { CallId = "a", Name = "first" }, new ChatToolCall { CallId = "b", Name = "second" }]
            };

            var parts = message.GetParts();

            Assert.Equal(3, parts.Count);
            Assert.Equal("first", parts[0].ToolCall!.Name);
            Assert.Equal("second", parts[1].ToolCall!.Name);
            Assert.True(parts[2].IsText);
            Assert.Equal("Answer", parts[2].Text);

            message.ToolCalls[1].TextPosition = 100;
            parts = message.GetParts();
            Assert.Equal("Answer", parts[1].Text);
            Assert.Equal("second", parts[2].ToolCall!.Name);
        }

        [Fact]
        public void ChatCitation_NormalizeMarkers_MovesToolCallPositions()
        {
            var message = new ChatMessage
            {
                Content = "See【1†source】 here. More【2】 text.",
                ToolCalls = [new ChatToolCall { CallId = "a", TextPosition = "See【1†source】 here. ".Length }, new ChatToolCall { CallId = "b", TextPosition = 1000 }]
            };

            ChatCitation.NormalizeMarkers(message);

            Assert.Equal("See[1] here. More[2] text.", message.Content);
            Assert.Equal("See[1] here. ".Length, message.ToolCalls[0].TextPosition);
            Assert.Equal(message.Content.Length, message.ToolCalls[1].TextPosition);
        }

        [Fact]
        public async Task RadzenAIChat_QueuesMessagesSentWhileResponding()
        {
            var (ctx, client) = CreateContext();
            using var __ = ctx;

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.SystemPrompt, "slow")
                .Add(p => p.QueueMessages, true));

            var send = component.InvokeAsync(() => component.Instance.SendMessage("First"));

            component.WaitForAssertion(() => Assert.Contains("rz-chat-stop-btn", component.Markup));

            await component.InvokeAsync(() => component.Instance.SendMessage("Second"));

            Assert.Equal(3, component.Instance.GetMessages().Count);
            Assert.Equal("Second", Assert.Single(component.Instance.QueuedMessages).Content);
            component.WaitForAssertion(() => Assert.Contains("rz-chat-message-queued", component.Markup));

            await component.InvokeAsync(() => component.Instance.SendMessage("Third"));
            Assert.Equal(2, component.Instance.QueuedMessages.Count);

            await send;

            component.WaitForAssertion(() => Assert.Equal(6, component.Instance.GetMessages().Count));
            component.WaitForAssertion(() => Assert.False(component.Instance.GetMessages()[5].IsStreaming));

            Assert.Empty(component.Instance.QueuedMessages);
            Assert.DoesNotContain("rz-chat-message-queued", component.Markup);
            Assert.Equal(3, client.Requests.Count);
            Assert.Equal("Second", client.Requests[1].Last(message => message.Role == ChatRole.User).Text);
            Assert.Equal("Third", client.Requests[2].Last(message => message.Role == ChatRole.User).Text);

            var messages = component.Instance.GetMessages();
            Assert.Equal(new[] { true, false, true, false, true, false }, messages.Select(message => message.IsUser));
            Assert.Equal("Second", messages[2].Content);
            Assert.Equal("Third", messages[4].Content);

            var session = ctx.Services.GetRequiredService<IAIChatService>().GetOrCreateSession(component.Instance.GetSessionId());
            Assert.Equal(6, session.Messages.Count);
            Assert.Equal("Second", session.Messages[2].Content);
            Assert.Equal("Third", session.Messages[4].Content);
        }

        [Fact]
        public async Task RadzenAIChat_IgnoresMessagesSentWhileRespondingByDefault()
        {
            var (ctx, client) = CreateContext();
            using var __ = ctx;

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters.Add(p => p.SystemPrompt, "slow"));

            var send = component.InvokeAsync(() => component.Instance.SendMessage("First"));

            component.WaitForAssertion(() => Assert.Contains("rz-chat-stop-btn", component.Markup));

            await component.InvokeAsync(() => component.Instance.SendMessage("Second"));

            Assert.Equal(2, component.Instance.GetMessages().Count);

            await component.InvokeAsync(() => component.Instance.Stop());
            await send;

            Assert.Single(client.Requests);
        }

        [Fact]
        public async Task RadzenAIChat_StopDropsQueuedMessagesAndRestoresTheirText()
        {
            var (ctx, client) = CreateContext();
            using var __ = ctx;

            var input = string.Empty;

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.SystemPrompt, "slow")
                .Add(p => p.QueueMessages, true)
                .Add(p => p.InputChanged, value => input = value));

            var send = component.InvokeAsync(() => component.Instance.SendMessage("First"));

            component.WaitForAssertion(() => Assert.Contains("word word", component.Instance.GetMessages()[1].Content));

            await component.InvokeAsync(() => component.Instance.AddAttachment(new ChatAttachment { Name = "notes.txt", MediaType = "text/plain", Data = new byte[] { 1 } }));
            await component.InvokeAsync(() => component.Instance.SendMessage("Second"));
            Assert.Empty(component.Instance.PendingAttachments);

            await component.InvokeAsync(() => component.Instance.Stop());
            await send;

            Assert.Equal(2, component.Instance.GetMessages().Count);
            Assert.Equal("First", component.Instance.GetMessages()[0].Content);
            Assert.StartsWith("word ", component.Instance.GetMessages()[1].Content);
            Assert.Empty(component.Instance.QueuedMessages);
            Assert.Equal("Second", input);
            Assert.Equal("Second", component.Instance.Input);
            Assert.Single(component.Instance.PendingAttachments);
            component.WaitForAssertion(() => Assert.Equal("Second", component.Find("textarea").GetAttribute("value")));
            Assert.Single(client.Requests);
        }

        [Fact]
        public async Task RadzenAIChat_BindsInput()
        {
            var (ctx, _) = CreateContext();
            using var __ = ctx;

            var changes = new List<string>();

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.Input, "Offered prompt")
                .Add(p => p.InputChanged, value => changes.Add(value)));

            Assert.Equal("Offered prompt", component.Find("textarea").GetAttribute("value"));
            Assert.False(component.Find(".rz-chat-send-btn").HasAttribute("disabled"));

            component.Find("textarea").Input("Typed");
            Assert.Equal("Typed", Assert.Single(changes));
            Assert.Equal("Typed", component.Instance.Input);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Input, "Typed"));
            Assert.Equal("Typed", component.Find("textarea").GetAttribute("value"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Input, "Replaced"));
            Assert.Equal("Replaced", component.Find("textarea").GetAttribute("value"));

            await component.InvokeAsync(() => component.Instance.SendMessage("Replaced"));
            component.WaitForAssertion(() => Assert.Contains("Hello there", component.Markup));

            Assert.Equal(string.Empty, changes.Last());
            Assert.Equal(string.Empty, component.Instance.Input);
            Assert.Equal(string.Empty, component.Find("textarea").GetAttribute("value"));
        }

        [Fact]
        public void RadzenAIChat_RendersHeaderAndInputSlots()
        {
            var (ctx, _) = CreateContext();
            using var __ = ctx;

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.ShowClearButton, false)
                .Add(p => p.HeaderTemplate, builder => builder.AddMarkupContent(0, "<select class=\"chat-switcher\"></select>"))
                .Add(p => p.InputHeaderTemplate, builder => builder.AddMarkupContent(0, "<span class=\"run-status\">Building</span>"))
                .Add(p => p.InputFooterTemplate, builder => builder.AddMarkupContent(0, "<button class=\"publish\">Publish</button>")));

            Assert.NotNull(component.Find(".rz-chat-header .rz-chat-header-content .chat-switcher"));
            Assert.Empty(component.FindAll(".rz-chat-header-title"));
            Assert.NotNull(component.Find(".rz-chat-input .rz-chat-input-header .run-status"));
            Assert.NotNull(component.Find(".rz-chat-input .rz-chat-input-footer .publish"));

            var input = component.Find(".rz-chat-input");
            var header = input.QuerySelector(".rz-chat-input-header")!;
            var inner = input.QuerySelector(".rz-chat-input-inner")!;
            var footer = input.QuerySelector(".rz-chat-input-footer")!;
            Assert.True(header.CompareDocumentPosition(inner).HasFlag(AngleSharp.Dom.DocumentPositions.Following));
            Assert.True(inner.CompareDocumentPosition(footer).HasFlag(AngleSharp.Dom.DocumentPositions.Following));

            var plain = ctx.RenderComponent<RadzenAIChat>(parameters => parameters.Add(p => p.ShowClearButton, false));
            Assert.Empty(plain.FindAll(".rz-chat-header"));
        }

        [Fact]
        public async Task RadzenAIChat_HidesAvatarsAndLabelsTheSendButton()
        {
            var (ctx, _) = CreateContext();
            using var __ = ctx;

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.ShowAvatars, false)
                .Add(p => p.SendButtonText, "Build it")
                .Add(p => p.SendButtonIcon, "bolt"));

            var send = component.Find(".rz-chat-send-btn");
            Assert.Contains("Build it", send.TextContent);
            Assert.Contains("bolt", send.QuerySelector(".rzi")!.TextContent);

            await component.InvokeAsync(() => component.Instance.SendMessage("Hi"));
            component.WaitForAssertion(() => Assert.Contains("Hello there", component.Markup));

            Assert.Empty(component.FindAll(".rz-chat-message-avatar"));
            Assert.Equal(2, component.FindAll(".rz-chat-message").Count);

            var withAvatars = ctx.RenderComponent<RadzenAIChat>();
            await withAvatars.InvokeAsync(() => withAvatars.Instance.SendMessage("Hi"));
            withAvatars.WaitForAssertion(() => Assert.Equal(2, withAvatars.FindAll(".rz-chat-message-avatar").Count));
            Assert.Contains("send", withAvatars.Find(".rz-chat-send-btn .rzi").TextContent);
        }

        [Fact]
        public async Task RadzenAIChat_RendersUsageAndMessageFooterTemplates()
        {
            var (ctx, _) = CreateContext();
            using var __ = ctx;

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.SystemPrompt, "credits")
                .Add(p => p.ShowUsage, true)
                .Add(p => p.UsageTemplate, message => builder => builder.AddMarkupContent(0, $"<em class=\"credits\">{message.Usage!.AdditionalCounts!["credits"]} credits</em>"))
                .Add(p => p.MessageFooterTemplate, message => builder => builder.AddMarkupContent(0, $"<span class=\"changed-files\">{(message.IsUser ? "user" : "assistant")} footer</span>")));

            await component.InvokeAsync(() => component.Instance.SendMessage("Hi"));
            component.WaitForAssertion(() => Assert.Contains("Hello there", component.Markup));

            Assert.Equal("3 credits", component.Find(".rz-chat-message-usage .credits").TextContent);
            Assert.DoesNotContain("12 tokens", component.Markup);

            var footers = component.FindAll(".rz-chat-message-footer .changed-files");
            Assert.Equal(2, footers.Count);
            Assert.Equal("user footer", footers[0].TextContent);
            Assert.Equal("assistant footer", footers[1].TextContent);

            var assistant = component.Find(".rz-chat-message-assistant");
            var text = assistant.QuerySelector(".rz-chat-message-text")!;
            var footer = assistant.QuerySelector(".rz-chat-message-footer")!;
            Assert.True(text.CompareDocumentPosition(footer).HasFlag(AngleSharp.Dom.DocumentPositions.Following));
        }

        [Fact]
        public async Task RadzenAIChat_EnforcesTotalAttachmentBudget()
        {
            var (ctx, _) = CreateContext();
            using var __ = ctx;

            var errors = new List<Exception>();

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.AllowAttachments, true)
                .Add(p => p.MaxTotalAttachmentSize, 10)
                .Add(p => p.Error, error => errors.Add(error)));

            var inputFile = component.FindComponent<InputFile>();

            await component.InvokeAsync(() => inputFile.UploadFiles(InputFileContent.CreateFromText("12345678", "first.txt")));

            Assert.Single(component.Instance.PendingAttachments);
            Assert.Empty(errors);

            await component.InvokeAsync(() => inputFile.UploadFiles(InputFileContent.CreateFromText("12345", "second.txt")));

            Assert.Single(component.Instance.PendingAttachments);
            var error = Assert.Single(errors);
            Assert.Contains("second.txt", error.Message);
            Assert.Contains("total attachment limit of 10 bytes", error.Message);

            await component.InvokeAsync(() => inputFile.UploadFiles(InputFileContent.CreateFromText("12", "third.txt")));

            Assert.Equal(2, component.Instance.PendingAttachments.Count);
            Assert.Single(errors);
        }

        private sealed class TakingChatService(IServiceProvider serviceProvider, Microsoft.Extensions.Options.IOptions<AIChatServiceOptions> options) : AIChatService(serviceProvider, options), IAIChatService
        {
            public List<Microsoft.Extensions.AI.ChatMessage> Taken { get; } = new();

            public Task<bool> TryQueueMessageAsync(Microsoft.Extensions.AI.ChatMessage message, string? sessionId, CancellationToken cancellationToken = default)
            {
                Taken.Add(message);
                return Task.FromResult(true);
            }
        }

        [Fact]
        public async Task RadzenAIChat_HandsQueuedMessagesToAServiceThatTakesThem()
        {
            var (ctx, client) = CreateContext();
            using var __ = ctx;

            ctx.Services.AddScoped<IAIChatService, TakingChatService>();

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.SystemPrompt, "slow")
                .Add(p => p.QueueMessages, true));

            var send = component.InvokeAsync(() => component.Instance.SendMessage("First"));

            component.WaitForAssertion(() => Assert.Contains("word word", component.Instance.GetMessages()[1].Content));

            Assert.NotNull(component.Find(".rz-chat-stop-btn"));
            Assert.NotNull(component.Find(".rz-chat-send-btn"));

            await component.InvokeAsync(() => component.Instance.SendMessage("Second"));

            var service = Assert.IsType<TakingChatService>(ctx.Services.GetRequiredService<IAIChatService>());
            var taken = Assert.Single(service.Taken);
            Assert.Equal("Second", taken.Text);
            Assert.Equal(component.Instance.GetMessages()[2].Id, taken.MessageId);

            Assert.Equal(3, component.Instance.GetMessages().Count);
            Assert.Empty(component.Instance.QueuedMessages);
            component.WaitForAssertion(() => Assert.Contains("rz-chat-message-queued", component.Markup));

            await component.InvokeAsync(() => component.Instance.Stop());
            await send;

            Assert.Equal(3, component.Instance.GetMessages().Count);
            component.WaitForAssertion(() => Assert.DoesNotContain("rz-chat-message-queued", component.Markup));
            Assert.True(string.IsNullOrEmpty(component.Instance.Input));
            Assert.Single(client.Requests);

            var unread = component.Instance.GetMessages().Single(message => message.Id == taken.MessageId);
            await component.InvokeAsync(() => Assert.True(component.Instance.RemoveMessage(unread)));
            Assert.Equal(2, component.Instance.GetMessages().Count);
        }

        [Fact]
        public async Task RadzenAIChat_ShowsTheSendButtonWhileStreamingOnlyWhenQueueing()
        {
            var (ctx, _) = CreateContext();
            using var __ = ctx;

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters.Add(p => p.SystemPrompt, "slow"));

            var send = component.InvokeAsync(() => component.Instance.SendMessage("First"));
            component.WaitForAssertion(() => Assert.Contains("rz-chat-stop-btn", component.Markup));
            Assert.Empty(component.FindAll(".rz-chat-send-btn"));

            await component.InvokeAsync(() => component.Instance.Stop());
            await send;
            component.WaitForAssertion(() => Assert.NotNull(component.Find(".rz-chat-send-btn")));
        }

        [Fact]
        public async Task RadzenAIChat_CollectsAdditionalPropertiesOfTheResponse()
        {
            var (ctx, _) = CreateContext();
            using var __ = ctx;

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.SystemPrompt, "files")
                .Add(p => p.MessageFooterTemplate, message => builder => builder.AddMarkupContent(0, message.AdditionalProperties?.TryGetValue("changedFiles", out string[]? files) == true ? $"<span class=\"changed\">{string.Join(", ", files!)}</span>" : string.Empty)));

            await component.InvokeAsync(() => component.Instance.SendMessage("Edit"));
            component.WaitForAssertion(() => Assert.Contains("Edited the page and the layout", component.Markup));

            var assistant = component.Instance.GetMessages()[1];
            Assert.Equal(2.5m, assistant.AdditionalProperties!["credits"]);
            Assert.Equal("Pages/Home.razor", component.Find(".changed").TextContent);

            var session = ctx.Services.GetRequiredService<IAIChatService>().GetOrCreateSession(component.Instance.GetSessionId());
            Assert.Equal(2.5m, session.Messages[1].AdditionalProperties!["credits"]);

            var json = ConversationSessionSerializer.Serialize(session);
            var restored = ConversationSessionSerializer.Deserialize(json)!;
            Assert.True(restored.Messages[1].AdditionalProperties!.ContainsKey("changedFiles"));
        }

        [Fact]
        public async Task AIChatService_ForwardsQueuedMessagesToTheChatClientQueue()
        {
            var (ctx, client) = CreateContext();
            using var __ = ctx;

            client.Queue = new QueueingClient();

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.SystemPrompt, "slow")
                .Add(p => p.QueueMessages, true));

            var send = component.InvokeAsync(() => component.Instance.SendMessage("First"));
            component.WaitForAssertion(() => Assert.Contains("word word", component.Instance.GetMessages()[1].Content));

            await component.InvokeAsync(() => component.Instance.SendMessage("Second"));

            var taken = Assert.Single(client.Queue.Taken);
            Assert.Equal("Second", taken.Text);
            Assert.Equal(component.Instance.GetMessages()[2].Id, taken.MessageId);
            Assert.Empty(component.Instance.QueuedMessages);
            component.WaitForAssertion(() => Assert.Contains("rz-chat-message-queued", component.Markup));

            component.WaitForAssertion(() => Assert.Equal(4, component.Instance.GetMessages().Count));
            component.WaitForAssertion(() => Assert.Contains("after", component.Instance.GetMessages()[3].Content));

            var messages = component.Instance.GetMessages();
            Assert.Equal(new[] { true, false, true, false }, messages.Select(message => message.IsUser));
            Assert.Equal("Second", messages[2].Content);
            Assert.False(messages[1].IsStreaming);
            Assert.DoesNotContain("after", messages[1].Content);
            Assert.True(messages[3].IsStreaming);
            Assert.DoesNotContain("rz-chat-message-queued", component.Markup);

            var first = component.Markup.IndexOf("Second", StringComparison.Ordinal);
            var answer = component.Markup.IndexOf("after", StringComparison.Ordinal);
            Assert.True(first > 0 && first < answer);

            await component.InvokeAsync(() => component.Instance.Stop());
            await send;

            Assert.Single(client.Requests);
            Assert.Equal(4, component.Instance.GetMessages().Count);

            var session = ctx.Services.GetRequiredService<IAIChatService>().GetOrCreateSession(component.Instance.GetSessionId());
            Assert.Equal(new[] { "First", null, "Second", null }, session.Messages.Select(message => message.IsUser ? message.Content : null));
            Assert.StartsWith("word ", session.Messages[1].Content);
            Assert.DoesNotContain("after", session.Messages[1].Content);
            Assert.StartsWith("after ", session.Messages[3].Content);
            Assert.Equal(new[] { ChatRole.User, ChatRole.Assistant, ChatRole.User, ChatRole.Assistant }, session.History.Select(message => message.Role));
            Assert.Equal("Second", session.History[2].Text);
            Assert.Equal(messages[2].Id, session.History[2].MessageId);
            Assert.Equal(messages[0].Id, session.Messages[0].Id);
            Assert.Equal(messages[2].Id, session.Messages[2].Id);

            var service = ctx.Services.GetRequiredService<IAIChatService>();
            client.Queue = null;
            Assert.False(await service.TryQueueMessageAsync(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, "Third"), component.Instance.GetSessionId()));
        }

        [Fact]
        public async Task RadzenAIChat_RendersStatusProgressAndErrorEntries()
        {
            var (ctx, _) = CreateContext();
            using var __ = ctx;

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.StopButtonText, "Stop the agent")
                .Add(p => p.StopButtonIcon, "stop")
                .Add(p => p.MessageFooterTemplate, message => builder => builder.AddMarkupContent(0, "<i class=\"footer\"></i>")));

            ChatMessage progress = null!;

            await component.InvokeAsync(() =>
            {
                component.Instance.AddMessage("Hi", true);
                component.Instance.AddMessage(new ChatMessage { Kind = ChatMessageKind.Status, Content = "Stopped." });
                progress = component.Instance.AddMessage(new ChatMessage { Kind = ChatMessageKind.Progress, Content = "Building..." });
                component.Instance.AddMessage(new ChatMessage { Kind = ChatMessageKind.Error, Content = "The build failed." });
            });

            component.WaitForAssertion(() => Assert.Equal(4, component.Instance.GetMessages().Count));

            Assert.Equal("Stopped.", component.Find(".rz-chat-status").TextContent.Trim());
            Assert.Equal("Building...", component.Find(".rz-chat-progress .rz-chat-progress-text").TextContent);
            Assert.NotNull(component.Find(".rz-chat-progress .rz-progressbar-circular-viewbox"));
            Assert.Contains("The build failed.", component.Find(".rz-chat-error").TextContent);
            Assert.Single(component.FindAll(".rz-chat-message"));
            Assert.Single(component.FindAll(".footer"));

            await component.InvokeAsync(() =>
            {
                progress.Content = "Still building...";
                component.Instance.Refresh();
            });
            component.WaitForAssertion(() => Assert.Equal("Still building...", component.Find(".rz-chat-progress-text").TextContent));

            await component.InvokeAsync(() => Assert.True(component.Instance.RemoveMessage(progress)));
            component.WaitForAssertion(() => Assert.Empty(component.FindAll(".rz-chat-progress")));
            Assert.False(component.Instance.RemoveMessage(progress));
            Assert.Equal(3, component.Instance.GetMessages().Count);

            var send = component.InvokeAsync(() => component.Instance.SendMessage("Go", systemPrompt: "slow"));
            component.WaitForAssertion(() => Assert.Contains("rz-chat-stop-btn", component.Markup));
            var stop = component.Find(".rz-chat-stop-btn");
            Assert.Contains("Stop the agent", stop.TextContent);
            Assert.Contains("stop", stop.QuerySelector(".rzi")!.TextContent);
            await component.InvokeAsync(() => component.Instance.Stop());
            await send;
        }

        [Fact]
        public async Task RadzenAIChat_MessageTemplateCanFallBackToTheDefaultRendering()
        {
            var (ctx, _) = CreateContext();
            using var __ = ctx;

            RadzenAIChat chat = null!;

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.MessageTemplate, message => builder =>
                {
                    if (message.AdditionalProperties?.ContainsKey("plan") == true)
                    {
                        builder.AddMarkupContent(0, $"<div class=\"plan\">{message.Content}</div>");
                    }
                    else
                    {
                        builder.AddContent(1, chat.RenderMessage(message));
                        builder.AddMarkupContent(2, "<div class=\"picker\">pick</div>");
                    }
                }));

            chat = component.Instance;

            await component.InvokeAsync(() => component.Instance.SendMessage("Hi"));
            component.WaitForAssertion(() => Assert.Contains("Hello there", component.Markup));

            await component.InvokeAsync(() => component.Instance.AddMessage(new ChatMessage { Content = "{\"app\":\"Shop\"}", AdditionalProperties = new AdditionalPropertiesDictionary { ["plan"] = true } }));

            component.WaitForAssertion(() => Assert.NotNull(component.Find(".plan")));
            Assert.Equal(2, component.FindAll(".rz-chat-message").Count);
            Assert.Equal(2, component.FindAll(".rz-chat-message-avatar").Count);
            Assert.Equal(2, component.FindAll(".picker").Count);
            Assert.Contains("Hello there", component.Find(".rz-chat-message-assistant .rz-chat-message-text").TextContent);
        }

        [Fact]
        public async Task RadzenAIChat_AttachmentAddingCanCancelOrReplaceAPickedFile()
        {
            var (ctx, _) = CreateContext();
            using var __ = ctx;

            var errors = new List<Exception>();
            var seen = new List<string>();

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.AllowAttachments, true)
                .Add(p => p.MaxTotalAttachmentSize, 6)
                .Add(p => p.Error, error => errors.Add(error))
                .Add(p => p.AttachmentAdding, args =>
                {
                    seen.Add(args.Attachment.Name);

                    if (args.Attachment.Name == "skip.txt")
                    {
                        args.Cancel = true;
                    }
                    else if (args.Attachment.Name == "big.txt")
                    {
                        args.Attachment = new ChatAttachment { Name = "small.txt", MediaType = "text/plain", Data = new byte[] { 1, 2, 3 } };
                    }
                }));

            var inputFile = component.FindComponent<InputFile>();

            await component.InvokeAsync(() => inputFile.UploadFiles(
                InputFileContent.CreateFromText("skip me", "skip.txt"),
                InputFileContent.CreateFromText("0123456789", "big.txt"),
                InputFileContent.CreateFromText("0123456789", "other.txt")));

            Assert.Equal(new[] { "skip.txt", "big.txt", "other.txt" }, seen);
            var pending = Assert.Single(component.Instance.PendingAttachments);
            Assert.Equal("small.txt", pending.Name);
            Assert.Equal(3, pending.Data.Length);
            Assert.Contains("other.txt", Assert.Single(errors).Message);

            await component.InvokeAsync(() => component.Instance.AddAttachment(new ChatAttachment { Name = "direct.txt", MediaType = "text/plain", Data = new byte[100] }));
            Assert.Equal(2, component.Instance.PendingAttachments.Count);
            Assert.Equal(3, seen.Count);
        }

        [Fact]
        public async Task RadzenAIChat_SkipsUserUpdatesThatMatchNoMessage()
        {
            var (ctx, client) = CreateContext();
            using var __ = ctx;

            client.EmitUnknownMarker = true;

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters.Add(p => p.SystemPrompt, "slow"));

            await component.InvokeAsync(() => component.Instance.SendMessage("Go"));

            var messages = component.Instance.GetMessages();
            Assert.Equal(2, messages.Count);
            Assert.DoesNotContain("leak", messages[1].Content);
            Assert.DoesNotContain("leak", component.Markup);
            Assert.Equal(50, messages[1].Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);

            var session = ctx.Services.GetRequiredService<IAIChatService>().GetOrCreateSession(component.Instance.GetSessionId());
            Assert.Equal(2, session.Messages.Count);
            Assert.DoesNotContain("leak", session.Messages[1].Content);
            Assert.Equal(new[] { ChatRole.User, ChatRole.Assistant }, session.History.Select(message => message.Role));
            Assert.DoesNotContain("leak", session.History[1].Text);
        }

        [Fact]
        public async Task AIChatService_DoesNotSaveTakenMessagesTheResponseNeverRead()
        {
            var (ctx, client) = CreateContext();
            using var __ = ctx;

            client.Queue = new QueueingClient();

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.SystemPrompt, "slow")
                .Add(p => p.QueueMessages, true));

            var send = component.InvokeAsync(() => component.Instance.SendMessage("First"));
            component.WaitForAssertion(() => Assert.Contains("word", component.Instance.GetMessages()[1].Content));

            await component.InvokeAsync(() => component.Instance.SendMessage("Second"));
            await component.InvokeAsync(() => component.Instance.Stop());
            await send;

            var session = ctx.Services.GetRequiredService<IAIChatService>().GetOrCreateSession(component.Instance.GetSessionId());
            Assert.Equal(new[] { true, false }, session.Messages.Select(message => message.IsUser));
            Assert.Equal(new[] { ChatRole.User, ChatRole.Assistant }, session.History.Select(message => message.Role));
        }
    }
}

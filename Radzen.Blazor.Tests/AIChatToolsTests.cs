using Bunit;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class AIChatToolsTests
    {
        private sealed class ScriptedChatClient : IChatClient
        {
            public List<List<Microsoft.Extensions.AI.ChatMessage>> Requests { get; } = new();

            public List<ChatOptions?> Options { get; } = new();

            public Task<ChatResponse> GetResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            {
                return GetStreamingResponseAsync(messages, options, cancellationToken).ToChatResponseAsync(cancellationToken);
            }

            public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                var list = messages.ToList();
                Requests.Add(list);
                Options.Add(options);

                await Task.Yield();

                var result = list.SelectMany(message => message.Contents.OfType<FunctionResultContent>()).LastOrDefault();

                if (result != null)
                {
                    var text = result.Result?.ToString() ?? string.Empty;
                    yield return new ChatResponseUpdate(ChatRole.Assistant, "Result: ") { ResponseId = "r2", MessageId = "r2" };
                    yield return new ChatResponseUpdate(ChatRole.Assistant, text) { ResponseId = "r2", MessageId = "r2" };
                    yield break;
                }

                var tools = options?.Tools?.OfType<AIFunctionDeclaration>().ToList();

                if (tools is { Count: > 0 })
                {
                    var calls = tools.Select((tool, index) => (AIContent)new FunctionCallContent($"call_{index + 1}", tool.Name, new Dictionary<string, object?> { ["city"] = "Sofia" })).ToList();
                    yield return new ChatResponseUpdate(ChatRole.Assistant, calls) { ResponseId = "r1", MessageId = "r1", FinishReason = ChatFinishReason.ToolCalls };
                    yield break;
                }

                if (options?.Instructions == "slow")
                {
                    for (var i = 0; i < 50; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.Delay(20, cancellationToken);
                        yield return new ChatResponseUpdate(ChatRole.Assistant, "word ") { ResponseId = "r0", MessageId = "r0" };
                    }

                    yield break;
                }

                if (options?.Instructions == "usage")
                {
                    yield return new ChatResponseUpdate(ChatRole.Assistant, "Hello there") { ResponseId = "r0", MessageId = "r0" };
                    yield return new ChatResponseUpdate(ChatRole.Assistant, [new UsageContent(new UsageDetails { InputTokenCount = 5, OutputTokenCount = 7, TotalTokenCount = 12 })]) { ResponseId = "r0", MessageId = "r0" };
                    yield break;
                }

                if (options?.Instructions == "cite")
                {
                    var cited = new TextContent("Hello there");
                    cited.Annotations = [new CitationAnnotation { Title = "Radzen docs", Url = new Uri("https://blazor.radzen.com/datagrid"), Snippet = "The grid" }];
                    yield return new ChatResponseUpdate(ChatRole.Assistant, [cited]) { ResponseId = "r0", MessageId = "r0" };
                    yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent(string.Empty) { Annotations = [new CitationAnnotation { Title = "Radzen docs", Url = new Uri("https://blazor.radzen.com/datagrid") }, new CitationAnnotation { Title = "Second source" }] }]) { ResponseId = "r0", MessageId = "r0" };
                    yield break;
                }

                if (options?.Instructions == "reason")
                {
                    yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextReasoningContent("Thinking about it")]) { ResponseId = "r0", MessageId = "r0" };
                }

                yield return new ChatResponseUpdate(ChatRole.Assistant, "Hello there") { ResponseId = "r0", MessageId = "r0" };
            }

            public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;

            public void Dispose()
            {
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

        [Description("Gets the weather")]
        private static string GetWeather([Description("The city")] string city) => $"Sunny in {city}";

        [Fact]
        public async Task RadzenAIChat_InvokesToolsAndRendersCalls()
        {
            var (ctx, client) = CreateContext();
            using var _ = ctx;

            var invoked = 0;
            var completed = new List<ChatToolCall>();
            var tool = AIFunctionFactory.Create((string city) => { invoked++; return GetWeather(city); }, "get_weather", "Gets the weather");

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.Tools, new AITool[] { tool })
                .Add(p => p.ToolCallCompleted, call => completed.Add(call)));

            await component.InvokeAsync(() => component.Instance.SendMessage("Weather in Sofia?"));

            component.WaitForAssertion(() => Assert.Contains("Result: Sunny in Sofia", component.Markup));

            Assert.Equal(1, invoked);
            Assert.Contains("rz-chat-tool-call-completed", component.Markup);
            Assert.Contains("get_weather", component.Markup);
            Assert.Contains("check_circle", component.Markup);

            var messages = component.Instance.GetMessages();
            Assert.Equal(2, messages.Count);
            var assistant = messages[1];
            var call = Assert.Single(assistant.ToolCalls);
            Assert.Equal("get_weather", call.Name);
            Assert.Equal(ChatToolCallStatus.Completed, call.Status);
            Assert.Equal("Sunny in Sofia", call.GetResult<string>());
            Assert.Equal("Result: Sunny in Sofia", assistant.Content);
            Assert.Same(call, Assert.Single(completed));

            Assert.Equal(2, client.Requests.Count);
            Assert.Contains(client.Requests[1], message => message.Role == ChatRole.Tool);
            Assert.Equal("You are a helpful AI code assistant.", client.Options[0]!.Instructions);

            var service = ctx.Services.GetRequiredService<IAIChatService>();
            var session = service.GetOrCreateSession(component.Instance.GetSessionId());
            Assert.Equal(4, session.History.Count);
            Assert.Equal(ChatRole.User, session.History[0].Role);
            Assert.Contains(session.History[1].Contents, content => content is FunctionCallContent);
            Assert.Equal(ChatRole.Tool, session.History[2].Role);
            Assert.Equal("Result: Sunny in Sofia", session.History[3].Text);
            Assert.Equal(2, session.Messages.Count);
            Assert.Single(session.Messages[1].ToolCalls);
        }

        [Fact]
        public async Task RadzenAIChat_AsksForApprovalAndInvokesToolWhenApproved()
        {
            var (ctx, client) = CreateContext();
            using var _ = ctx;

            var invoked = 0;
            var tool = new ApprovalRequiredAIFunction(AIFunctionFactory.Create((string city) => { invoked++; return GetWeather(city); }, "get_weather", "Gets the weather"));

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters.Add(p => p.Tools, new AITool[] { tool }));

            await component.InvokeAsync(() => component.Instance.SendMessage("Weather in Sofia?"));

            component.WaitForAssertion(() => Assert.Contains("rz-chat-tool-approval", component.Markup));

            Assert.Equal(0, invoked);
            Assert.Contains("rz-chat-tool-call-awaitingapproval", component.Markup);
            Assert.Contains("The assistant wants to run get_weather.", component.Markup);
            Assert.Contains("city: Sofia", component.Markup);

            var call = Assert.Single(component.Instance.GetMessages()[1].ToolCalls);
            Assert.Equal(ChatToolCallStatus.AwaitingApproval, call.Status);
            Assert.NotNull(call.ApprovalRequest);

            component.Find(".rz-chat-tool-approve").Click();

            component.WaitForAssertion(() => Assert.Contains("Result: Sunny in Sofia", component.Markup));

            Assert.Equal(1, invoked);
            Assert.Equal(ChatToolCallStatus.Completed, call.Status);
            Assert.Equal("Sunny in Sofia", call.GetResult<string>());
            Assert.DoesNotContain("rz-chat-tool-approval", component.Markup);

            var messages = component.Instance.GetMessages();
            Assert.Equal(3, messages.Count);
            Assert.Equal("Result: Sunny in Sofia", messages[2].Content);
            Assert.Empty(messages[2].ToolCalls);

            var service = ctx.Services.GetRequiredService<IAIChatService>();
            var session = service.GetOrCreateSession(component.Instance.GetSessionId());
            Assert.Contains(session.History, message => message.Contents.OfType<ToolApprovalRequestContent>().Any());
            Assert.Contains(session.History, message => message.Role == ChatRole.User && message.Contents.OfType<ToolApprovalResponseContent>().Any());
        }

        [Fact]
        public async Task RadzenAIChat_RejectsToolCall()
        {
            var (ctx, client) = CreateContext();
            using var _ = ctx;

            var invoked = 0;
            var completed = new List<ChatToolCall>();
            var tool = new ApprovalRequiredAIFunction(AIFunctionFactory.Create((string city) => { invoked++; return GetWeather(city); }, "get_weather", "Gets the weather"));

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.Tools, new AITool[] { tool })
                .Add(p => p.ToolCallCompleted, call => completed.Add(call)));

            await component.InvokeAsync(() => component.Instance.SendMessage("Weather in Sofia?"));

            component.WaitForAssertion(() => Assert.Contains("rz-chat-tool-approval", component.Markup));

            component.Find(".rz-chat-tool-reject").Click();

            component.WaitForAssertion(() => Assert.Contains("Result: ", component.Markup));

            Assert.Equal(0, invoked);

            var call = Assert.Single(component.Instance.GetMessages()[1].ToolCalls);
            Assert.Equal(ChatToolCallStatus.Rejected, call.Status);
            Assert.Contains("rz-chat-tool-call-rejected", component.Markup);
            Assert.Same(call, Assert.Single(completed));
            Assert.Contains(client.Requests.Last(), message => message.Role == ChatRole.Tool);
        }

        [Fact]
        public async Task RadzenAIChat_SendsApprovalsTogetherWhenSeveralCallsAwaitApproval()
        {
            var (ctx, client) = CreateContext();
            using var _ = ctx;

            var invoked = new List<string>();
            var tools = new AITool[]
            {
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create((string city) => { invoked.Add("first"); return "First"; }, "first_tool")),
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create((string city) => { invoked.Add("second"); return "Second"; }, "second_tool"))
            };

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.Tools, tools)
                .Add(p => p.AllowMultipleToolCalls, false));

            await component.InvokeAsync(() => component.Instance.SendMessage("Do both"));

            component.WaitForAssertion(() => Assert.Equal(2, component.FindAll(".rz-chat-tool-approval").Count));

            Assert.False(client.Options[0]!.AllowMultipleToolCalls);

            var calls = component.Instance.GetMessages()[1].ToolCalls;
            Assert.Equal(2, calls.Count);

            await component.InvokeAsync(() => component.Instance.ApproveToolCall(calls[0]));

            Assert.Single(client.Requests);
            Assert.Equal(ChatToolCallStatus.Pending, calls[0].Status);
            Assert.Equal(ChatToolCallStatus.AwaitingApproval, calls[1].Status);
            Assert.Single(component.FindAll(".rz-chat-tool-approval"));

            await component.InvokeAsync(() => component.Instance.RejectToolCall(calls[1]));

            component.WaitForAssertion(() => Assert.Contains("Result: ", component.Markup));

            Assert.Equal(2, client.Requests.Count);
            var results = client.Requests[1].Where(message => message.Role == ChatRole.Tool).SelectMany(message => message.Contents.OfType<FunctionResultContent>()).ToList();
            Assert.Equal(2, results.Count);
            Assert.Equal("First", results.Single(result => result.CallId == "call_1").Result?.ToString());
            Assert.Contains("rejected", results.Single(result => result.CallId == "call_2").Result?.ToString(), StringComparison.OrdinalIgnoreCase);

            var service = ctx.Services.GetRequiredService<IAIChatService>();
            var session = service.GetOrCreateSession(component.Instance.GetSessionId());
            var approvalMessage = session.History.Single(message => message.Role == ChatRole.User && message.Contents.OfType<ToolApprovalResponseContent>().Any());
            Assert.Equal(2, approvalMessage.Contents.OfType<ToolApprovalResponseContent>().Count());
            Assert.Equal(new[] { "first" }, invoked);
            Assert.Equal(ChatToolCallStatus.Completed, calls[0].Status);
            Assert.Equal(ChatToolCallStatus.Rejected, calls[1].Status);
            Assert.Empty(component.FindAll(".rz-chat-tool-approval"));
        }

        [Fact]
        public async Task RadzenAIChat_UsesCustomToolCallTemplate()
        {
            var (ctx, _) = CreateContext();
            using var __ = ctx;

            var tool = AIFunctionFactory.Create((string city) => GetWeather(city), "get_weather");

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.Tools, new AITool[] { tool })
                .Add(p => p.ToolCallTemplate, call => builder =>
                {
                    builder.OpenElement(0, "em");
                    builder.AddAttribute(1, "class", "custom-call");
                    builder.AddContent(2, $"{call.Name}={call.GetResult<string>()}");
                    builder.CloseElement();
                }));

            await component.InvokeAsync(() => component.Instance.SendMessage("Weather in Sofia?"));

            component.WaitForAssertion(() => Assert.Contains("Result: Sunny in Sofia", component.Markup));

            Assert.Contains("custom-call", component.Markup);
            Assert.Contains("get_weather=Sunny in Sofia", component.Markup);
            Assert.DoesNotContain("rz-chat-tool-call-completed", component.Markup);
        }

        [Fact]
        public async Task RadzenAIChat_HidesToolCallsWhenShowToolCallsIsFalse()
        {
            var (ctx, _) = CreateContext();
            using var __ = ctx;

            var tool = AIFunctionFactory.Create((string city) => GetWeather(city), "get_weather");

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.Tools, new AITool[] { tool })
                .Add(p => p.ShowToolCalls, false));

            await component.InvokeAsync(() => component.Instance.SendMessage("Weather in Sofia?"));

            component.WaitForAssertion(() => Assert.Contains("Result: Sunny in Sofia", component.Markup));

            Assert.DoesNotContain("rz-chat-tool-call", component.Markup);
            Assert.Single(component.Instance.GetMessages()[1].ToolCalls);
        }

        [Fact]
        public async Task RadzenAIChat_UsesRegisteredChatClientWithoutTools()
        {
            var (ctx, client) = CreateContext();
            using var _ = ctx;

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters.Add(p => p.SystemPrompt, "Be terse."));

            await component.InvokeAsync(() => component.Instance.SendMessage("Hi"));

            component.WaitForAssertion(() => Assert.Contains("Hello there", component.Markup));

            Assert.DoesNotContain("rz-chat-tool-call", component.Markup);
            Assert.Equal("Be terse.", client.Options.Single()!.Instructions);
            Assert.Equal(ChatRole.User, client.Requests.Single().Single().Role);
        }

        [Fact]
        public async Task RadzenAIChat_RendersReasoningBlock()
        {
            var (ctx, _) = CreateContext();
            using var __ = ctx;

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters.Add(p => p.SystemPrompt, "reason"));

            await component.InvokeAsync(() => component.Instance.SendMessage("Hi"));
            component.WaitForAssertion(() => Assert.Contains("Hello there", component.Markup));

            Assert.Contains("rz-chat-reasoning", component.Markup);
            Assert.Contains("Thinking about it", component.Markup);
            Assert.Equal("Thinking about it", component.Instance.GetMessages()[1].Reasoning);
            Assert.Equal("Hello there", component.Instance.GetMessages()[1].Content);

            var hidden = ctx.RenderComponent<RadzenAIChat>(parameters => parameters.Add(p => p.SystemPrompt, "reason").Add(p => p.ShowReasoning, false));
            await hidden.InvokeAsync(() => hidden.Instance.SendMessage("Hi"));
            hidden.WaitForAssertion(() => Assert.Contains("Hello there", hidden.Markup));
            Assert.DoesNotContain("rz-chat-reasoning", hidden.Markup);
        }

        [Fact]
        public async Task RadzenAIChat_StopKeepsPartialResponse()
        {
            var (ctx, _) = CreateContext();
            using var __ = ctx;

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters.Add(p => p.SystemPrompt, "slow"));

            var send = component.InvokeAsync(() => component.Instance.SendMessage("Go"));

            component.WaitForAssertion(() => Assert.Contains("rz-chat-stop-btn", component.Markup));
            component.WaitForAssertion(() => Assert.Contains("word word", component.Instance.GetMessages()[1].Content));

            await component.InvokeAsync(() => component.Instance.Stop());
            await send;

            var message = component.Instance.GetMessages()[1];
            Assert.False(message.IsStreaming);
            Assert.StartsWith("word ", message.Content);
            Assert.DoesNotContain("Sorry, I encountered an error", message.Content);
            Assert.DoesNotContain("rz-chat-stop-btn", component.Markup);
            Assert.Contains("rz-chat-send-btn", component.Markup);
        }

        [Fact]
        public async Task RadzenAIChat_SendsAttachmentsAndShowsUsage()
        {
            var (ctx, client) = CreateContext();
            using var __ = ctx;

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.SystemPrompt, "usage")
                .Add(p => p.AllowAttachments, true)
                .Add(p => p.ShowUsage, true));

            Assert.Contains("rz-chat-attach-btn", component.Markup);

            await component.InvokeAsync(() => component.Instance.AddAttachment(new ChatAttachment { Name = "pixel.png", MediaType = "image/png", Data = new byte[] { 1, 2, 3 } }));

            component.WaitForAssertion(() => Assert.Contains("rz-chat-attachment-thumb", component.Markup));
            Assert.Single(component.Instance.PendingAttachments);

            await component.InvokeAsync(() => component.Instance.SendMessage("What is this?"));
            component.WaitForAssertion(() => Assert.Contains("Hello there", component.Markup));

            var request = client.Requests.Single().Single();
            Assert.Equal(ChatRole.User, request.Role);
            Assert.Equal("What is this?", request.Text);
            var data = Assert.Single(request.Contents.OfType<DataContent>());
            Assert.Equal("image/png", data.MediaType);
            Assert.Equal("pixel.png", data.Name);

            Assert.Empty(component.Instance.PendingAttachments);
            var messages = component.Instance.GetMessages();
            Assert.Single(messages[0].Attachments);
            Assert.Contains("rz-chat-message-attachment-image", component.Markup);
            Assert.Equal(12, messages[1].Usage!.TotalTokenCount);
            Assert.Contains("12 tokens", component.Markup);

            var session = ctx.Services.GetRequiredService<IAIChatService>().GetOrCreateSession(component.Instance.GetSessionId());
            Assert.Single(session.Messages[0].Attachments);
        }

        [Fact]
        public async Task AIChatService_GetCompletionsAsync_YieldsTextOnly()
        {
            var (ctx, client) = CreateContext();
            using var _ = ctx;

            var service = ctx.Services.GetRequiredService<IAIChatService>();

            var chunks = new List<string>();

            await foreach (var chunk in service.GetCompletionsAsync("Hi", "session-1", model: "m1", temperature: 0.1, maxTokens: 5))
            {
                chunks.Add(chunk);
            }

            Assert.Equal("Hello there", string.Concat(chunks));
            Assert.Equal("m1", client.Options.Single()!.ModelId);
            Assert.Equal(0.1f, client.Options.Single()!.Temperature);
            Assert.Equal(5, client.Options.Single()!.MaxOutputTokens);

            var session = service.GetOrCreateSession("session-1");
            Assert.Equal(2, session.History.Count);
            Assert.Equal(2, session.Messages.Count);
            Assert.True(session.Messages[0].IsUser);
            Assert.Equal("Hello there", session.Messages[1].Content);

            service.ClearSession("session-1");
            Assert.Empty(session.History);
            Assert.Empty(session.Messages);
        }

        [Fact]
        public async Task AIChatService_FallsBackToOpenAIClientWhenEndpointOverridden()
        {
            var (ctx, client) = CreateContext();
            using var _ = ctx;

            ctx.Services.AddSingleton(new System.Net.Http.HttpClient(new ThrowingHandler()));

            var service = ctx.Services.GetRequiredService<IAIChatService>();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await foreach (var _ in service.GetCompletionsAsync("Hi", endpoint: "https://example.com/v1/chat/completions"))
                {
                }
            });

            Assert.Equal("override endpoint used", exception.Message);
            Assert.Empty(client.Requests);
        }

        private sealed class ThrowingHandler : System.Net.Http.HttpMessageHandler
        {
            protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, CancellationToken cancellationToken)
            {
                throw new InvalidOperationException("override endpoint used");
            }
        }

        [Fact]
        public void ConversationSession_AddHistory_KeepsToolCallsWithTheirResults()
        {
            var session = new ConversationSession { MaxMessages = 3 };

            session.AddHistory(
            [
                new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, "first"),
                new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, [new FunctionCallContent("c1", "tool")]),
                new Microsoft.Extensions.AI.ChatMessage(ChatRole.Tool, [new FunctionResultContent("c1", "ok")]),
                new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, "done"),
                new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, "second"),
                new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, "reply")
            ]);

            Assert.Equal(2, session.History.Count);
            Assert.Equal("second", session.History[0].Text);
            Assert.Equal("reply", session.History[1].Text);
        }

        [Fact]
        public void ChatToolCall_FormatsArgumentsAndDeserializesResults()
        {
            var call = new ChatToolCall
            {
                Arguments = new Dictionary<string, object?> { ["city"] = "Sofia", ["days"] = 3 },
                Result = System.Text.Json.JsonSerializer.SerializeToElement(new[] { 1, 2, 3 })
            };

            Assert.Equal("city: Sofia, days: 3", call.FormattedArguments);
            Assert.Equal(new[] { 1, 2, 3 }, call.GetResult<int[]>());
            Assert.Null(new ChatToolCall().GetResult<int[]>());
        }

        [Description("Searches the docs")]
        private static List<ChatCitation> SearchDocs([Description("The city")] string city) =>
        [
            new ChatCitation { Title = $"Guide for {city}", Url = "https://example.com/guide", Snippet = "A guide" },
            new ChatCitation { Title = "Duplicate", Url = "HTTPS://EXAMPLE.COM/GUIDE" },
            new ChatCitation { Title = "Internal note", Snippet = "No url" }
        ];

        [Fact]
        public async Task RadzenAIChat_RendersCitationsReturnedByTools()
        {
            var (ctx, _) = CreateContext();
            using var __ = ctx;

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters.Add(p => p.Tools, new AITool[] { AIFunctionFactory.Create(SearchDocs) }));

            await component.InvokeAsync(() => component.Instance.SendMessage("Find it"));
            component.WaitForAssertion(() => Assert.Contains("Result:", component.Markup));

            var message = component.Instance.GetMessages().Last();
            Assert.Equal(2, message.Citations.Count);
            Assert.Equal("Guide for Sofia", message.Citations[0].Title);
            Assert.Equal("Internal note", message.Citations[1].Title);

            Assert.Contains("rz-chat-citations", component.Markup);
            Assert.Contains("Sources", component.Markup);
            var link = component.Find("a.rz-chat-citation");
            Assert.Equal("https://example.com/guide", link.GetAttribute("href"));
            Assert.Equal("_blank", link.GetAttribute("target"));
            Assert.Equal("A guide", link.GetAttribute("title"));
            Assert.Contains("Guide for Sofia", link.TextContent);
            Assert.Equal(2, component.FindAll(".rz-chat-citation").Count);
            Assert.Equal("2", component.FindAll(".rz-chat-citation-index")[1].TextContent);
        }

        [Fact]
        public async Task RadzenAIChat_RendersCitationAnnotationsAndCanHideThem()
        {
            var (ctx, _) = CreateContext();
            using var __ = ctx;

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters.Add(p => p.SystemPrompt, "cite"));

            await component.InvokeAsync(() => component.Instance.SendMessage("Hi"));
            component.WaitForAssertion(() => Assert.Contains("Hello there", component.Markup));

            var message = component.Instance.GetMessages()[1];
            Assert.Equal("Hello there", message.Content);
            Assert.Equal(2, message.Citations.Count);
            Assert.Equal("https://blazor.radzen.com/datagrid", message.Citations[0].Url);
            Assert.Equal("The grid", message.Citations[0].Snippet);
            Assert.Equal("Second source", message.Citations[1].Title);
            Assert.Contains("rz-chat-citations", component.Markup);

            var templated = ctx.RenderComponent<RadzenAIChat>(parameters => parameters.Add(p => p.SystemPrompt, "cite").Add(p => p.CitationTemplate, citation => builder => builder.AddMarkupContent(0, $"<em class=\"custom-citation\">{citation.Title}</em>")));
            await templated.InvokeAsync(() => templated.Instance.SendMessage("Hi"));
            templated.WaitForAssertion(() => Assert.Contains("Hello there", templated.Markup));
            Assert.Equal(2, templated.FindAll(".custom-citation").Count);
            Assert.Empty(templated.FindAll("a.rz-chat-citation"));

            var hidden = ctx.RenderComponent<RadzenAIChat>(parameters => parameters.Add(p => p.SystemPrompt, "cite").Add(p => p.ShowCitations, false));
            await hidden.InvokeAsync(() => hidden.Instance.SendMessage("Hi"));
            hidden.WaitForAssertion(() => Assert.Contains("Hello there", hidden.Markup));
            Assert.DoesNotContain("rz-chat-citations", hidden.Markup);
        }

        [Fact]
        public void ChatCitation_ConvertsToAndFromAnnotations()
        {
            var citation = new ChatCitation { Title = "T", Url = "https://example.com/a", Snippet = "S", FileId = "f1", ToolName = "search" };
            var annotation = citation.ToAnnotation();

            Assert.Equal("T", annotation.Title);
            Assert.Equal(new Uri("https://example.com/a"), annotation.Url);
            Assert.Equal("S", annotation.Snippet);
            Assert.Equal("f1", annotation.FileId);
            Assert.Equal("search", annotation.ToolName);

            var copy = ChatCitation.FromAnnotation(annotation);
            Assert.Equal("https://example.com/a", copy.Url);
            Assert.True(copy.IsSameSource(citation));
            Assert.False(copy.IsSameSource(new ChatCitation { Url = "https://example.com/b" }));
            Assert.True(new ChatCitation { FileId = "f" }.IsSameSource(new ChatCitation { FileId = "f" }));
            Assert.True(new ChatCitation { Title = "Same" }.IsSameSource(new ChatCitation { Title = "same" }));
            Assert.Null(new ChatCitation { Url = "not a url" }.ToAnnotation().Url);

            var message = new ChatMessage();
            AIChatService.ApplyCitations(message, new FunctionResultContent("c", new object[] { new ChatCitation { Url = "https://example.com/a" }, "ignored", new ChatCitation { Url = "https://example.com/a" } }));
            AIChatService.ApplyCitations(message, new FunctionResultContent("c", "plain text"));
            AIChatService.ApplyCitations(message, new FunctionResultContent("c", new TextContent("x") { Annotations = [new CitationAnnotation { Url = new Uri("https://example.com/c") }] }));
            Assert.Equal(2, message.Citations.Count);

            var json = new ChatMessage();
            AIChatService.ApplyCitations(json, new FunctionResultContent("c", System.Text.Json.JsonSerializer.SerializeToElement(new object[] { new { title = "Doc", url = "https://example.com/d", snippet = "S" }, new { title = "Product", url = "https://example.com/p", price = 10 }, new { Title = "Note" } })));
            AIChatService.ApplyCitations(json, new FunctionResultContent("c", System.Text.Json.JsonSerializer.SerializeToElement(new { title = "Single", url = "https://example.com/s" })));
            AIChatService.ApplyCitations(json, new FunctionResultContent("c", System.Text.Json.JsonSerializer.SerializeToElement(new { snippet = "no title or url" })));
            Assert.Equal(new[] { "Doc", "Note", "Single" }, json.Citations.Select(citation => citation.Title));

            Assert.Equal("See [1] and [2].", ChatCitation.NormalizeMarkers("See 【1†L1-L4】 and 【2】."));
            Assert.Equal("plain [3]", ChatCitation.NormalizeMarkers("plain [3]"));
            Assert.Equal("", ChatCitation.NormalizeMarkers(""));
        }
    }
}

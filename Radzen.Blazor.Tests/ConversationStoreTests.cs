using Bunit;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ConversationStoreTests
    {
        private sealed class EchoChatClient : IChatClient
        {
            public Task<ChatResponse> GetResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
                => GetStreamingResponseAsync(messages, options, cancellationToken).ToChatResponseAsync(cancellationToken);

            public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                yield return new ChatResponseUpdate(ChatRole.Assistant, "Echo: " + messages.Last().Text) { ResponseId = "r", MessageId = "r" };
            }

            public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;

            public void Dispose()
            {
            }
        }

        private sealed class RecordingStore : IConversationStore
        {
            public Dictionary<string, string> Json { get; } = new();

            public int Saves { get; private set; }

            public Task<ConversationSession?> LoadAsync(string sessionId, CancellationToken cancellationToken = default)
                => Task.FromResult(Json.TryGetValue(sessionId, out var json) ? ConversationSessionSerializer.Deserialize(json) : null);

            public Task SaveAsync(ConversationSession session, CancellationToken cancellationToken = default)
            {
                Saves++;
                Json[session.Id] = ConversationSessionSerializer.Serialize(session);
                return Task.CompletedTask;
            }

            public Task<IReadOnlyList<ConversationSession>> ListAsync(string? userId = null, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<ConversationSession>>(Json.Values.Select(json => ConversationSessionSerializer.Deserialize(json)!).Where(s => userId == null || s.UserId == userId).ToList());

            public Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
            {
                Json.Remove(sessionId);
                return Task.CompletedTask;
            }
        }

        private static ConversationSession CreateRichSession()
        {
            var session = new ConversationSession { Id = "s1", UserId = "alice" };
            session.AddMessage(new ChatMessage
            {
                IsUser = true,
                Role = "user",
                Content = "What is the weather in Sofia and how does it look?",
                Attachments = { new ChatAttachment { Name = "sky.png", MediaType = "image/png", Data = new byte[] { 1, 2, 3 } } }
            });
            session.AddMessage(new ChatMessage
            {
                IsUser = false,
                Role = "assistant",
                Content = "Sunny",
                Reasoning = "Checked the tool",
                Usage = new UsageDetails { InputTokenCount = 3, OutputTokenCount = 4, TotalTokenCount = 7 },
                ToolCalls =
                {
                    new ChatToolCall
                    {
                        CallId = "c1",
                        Name = "get_weather",
                        Status = ChatToolCallStatus.Completed,
                        Arguments = new Dictionary<string, object?> { ["city"] = JsonSerializer.SerializeToElement("Sofia") },
                        Result = JsonSerializer.SerializeToElement(new { temperature = 21 }),
                        Exception = new InvalidOperationException("not serialized")
                    }
                }
            });
            session.AddHistory(
            [
                new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, [new TextContent("What is the weather in Sofia and how does it look?"), new DataContent(new byte[] { 1, 2, 3 }, "image/png")]),
                new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, [new FunctionCallContent("c1", "get_weather", new Dictionary<string, object?> { ["city"] = "Sofia" })]),
                new Microsoft.Extensions.AI.ChatMessage(ChatRole.Tool, [new FunctionResultContent("c1", JsonSerializer.SerializeToElement(new { temperature = 21 }))]),
                new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, "Sunny")
            ]);
            return session;
        }

        [Fact]
        public void Serializer_RoundTripsSessionsWithToolCallsAttachmentsAndHistory()
        {
            var session = CreateRichSession();

            var json = ConversationSessionSerializer.Serialize(session);
            var copy = ConversationSessionSerializer.Deserialize(json)!;

            Assert.Equal("s1", copy.Id);
            Assert.Equal("alice", copy.UserId);
            Assert.Equal("What is the weather in Sofia and how does it look?", copy.Title);
            Assert.Equal(2, copy.Messages.Count);
            Assert.Equal("sky.png", copy.Messages[0].Attachments.Single().Name);
            Assert.Equal(new byte[] { 1, 2, 3 }, copy.Messages[0].Attachments.Single().Data.ToArray());
            Assert.True(copy.Messages[0].Attachments.Single().IsImage);

            var call = copy.Messages[1].ToolCalls.Single();
            Assert.Equal("get_weather", call.Name);
            Assert.Equal(ChatToolCallStatus.Completed, call.Status);
            Assert.Equal("city: Sofia", call.FormattedArguments);
            Assert.Equal(21, call.GetResult<JsonElement>().GetProperty("temperature").GetInt32());
            Assert.Null(call.Exception);
            Assert.Equal("Checked the tool", copy.Messages[1].Reasoning);
            Assert.Equal(7, copy.Messages[1].Usage!.TotalTokenCount);

            Assert.Equal(4, copy.History.Count);
            Assert.Equal(ChatRole.User, copy.History[0].Role);
            Assert.Single(copy.History[0].Contents.OfType<DataContent>());
            Assert.Equal("get_weather", copy.History[1].Contents.OfType<FunctionCallContent>().Single().Name);
            Assert.Equal(ChatRole.Tool, copy.History[2].Role);
            Assert.Equal("Sunny", copy.History[3].Text);
            Assert.DoesNotContain("not serialized", json);
            Assert.DoesNotContain("formattedArguments", json);
        }

        [Fact]
        public async Task InMemoryStore_SavesListsAndDeletes()
        {
            var store = new InMemoryConversationStore { MaxAge = TimeSpan.FromHours(1) };
            var old = new ConversationSession { Id = "old", UserId = "bob", LastUpdated = DateTime.Now.AddHours(-2) };
            var fresh = new ConversationSession { Id = "fresh", UserId = "alice" };

            await store.SaveAsync(old);
            await store.SaveAsync(fresh);

            Assert.Null(await store.LoadAsync("old"));
            Assert.Same(fresh, await store.LoadAsync("fresh"));
            Assert.Single(await store.ListAsync());
            Assert.Empty(await store.ListAsync("bob"));

            await store.DeleteAsync("fresh");
            Assert.Empty(await store.ListAsync());
        }

        [Fact]
        public async Task AIChatService_PersistsSessionsThroughTheStore()
        {
            var store = new RecordingStore();

            using var ctx = new TestContext();
            ctx.Services.AddSingleton<IChatClient>(new EchoChatClient());
            ctx.Services.AddAIChatService();
            ctx.Services.AddConversationStore<RecordingStore>(ServiceLifetime.Singleton);
            ctx.Services.AddSingleton<IConversationStore>(store);

            var service = ctx.Services.GetRequiredService<IAIChatService>();
            Assert.Same(store, service.Store);

            await foreach (var _ in service.GetStreamingResponseAsync(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, "Hello"), "session-1", userId: "alice"))
            {
            }

            Assert.Equal(1, store.Saves);
            Assert.Contains("Echo: Hello", store.Json["session-1"]);

            using var ctx2 = new TestContext();
            ctx2.Services.AddSingleton<IChatClient>(new EchoChatClient());
            ctx2.Services.AddAIChatService();
            ctx2.Services.AddSingleton<IConversationStore>(store);

            var second = ctx2.Services.GetRequiredService<IAIChatService>();
            var session = await second.GetOrCreateSessionAsync("session-1");
            Assert.Equal("alice", session.UserId);
            Assert.Equal("Hello", session.Title);
            Assert.Equal(2, session.Messages.Count);
            Assert.Equal(2, session.History.Count);

            var listed = await second.GetSessionsAsync("alice");
            Assert.Single(listed);

            await second.ClearSessionAsync("session-1");
            Assert.Empty((await second.GetOrCreateSessionAsync("session-1")).Messages);
            Assert.Equal(2, store.Saves);

            await second.DeleteSessionAsync("session-1");
            Assert.Empty(store.Json);
        }

        [Fact]
        public void AddConversationStore_ReplacesTheDefaultStore()
        {
            var services = new ServiceCollection();
            services.AddAIChatService();
            services.AddConversationStore<RecordingStore>(ServiceLifetime.Singleton);

            using var provider = services.BuildServiceProvider();
            Assert.IsType<RecordingStore>(provider.GetRequiredService<IConversationStore>());

            var defaults = new ServiceCollection();
            defaults.AddAIChatService();
            using var defaultProvider = defaults.BuildServiceProvider();
            Assert.IsType<InMemoryConversationStore>(defaultProvider.GetRequiredService<IConversationStore>());
        }

        [Fact]
        public async Task RadzenAIChat_RestoresConversationFromTheStore()
        {
            var store = new RecordingStore();
            await store.SaveAsync(CreateRichSession());

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddSingleton<IChatClient>(new EchoChatClient());
            ctx.Services.AddAIChatService();
            ctx.Services.AddSingleton<IConversationStore>(store);

            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters.Add(p => p.SessionId, "s1").Add(p => p.UserId, "alice").Add(p => p.ShowUsage, true));

            component.WaitForAssertion(() => Assert.Equal(2, component.Instance.GetMessages().Count));
            Assert.Contains("Sunny", component.Markup);
            Assert.Contains("get_weather", component.Markup);
            Assert.Contains("rz-chat-message-attachment-image", component.Markup);
            Assert.Contains("Checked the tool", component.Markup);
            Assert.Contains("7 tokens", component.Markup);

            await component.InvokeAsync(() => component.Instance.SendMessage("Again"));
            component.WaitForAssertion(() => Assert.Contains("Echo: Again", component.Markup));

            var saved = ConversationSessionSerializer.Deserialize(store.Json["s1"])!;
            Assert.Equal(4, saved.Messages.Count);
            Assert.Equal(6, saved.History.Count);
        }
    }
}

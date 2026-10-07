using Microsoft.Extensions.AI;
using Radzen;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

    public class OpenAICompatibleChatClientTests
    {
        private sealed class FakeHandler(Func<HttpRequestMessage, string, HttpResponseMessage> respond) : HttpMessageHandler
        {
            public List<(HttpRequestMessage Request, JsonDocument Body)> Requests { get; } = new();

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var body = await request.Content!.ReadAsStringAsync(cancellationToken);
                Requests.Add((request, JsonDocument.Parse(body)));
                return respond(request, body);
            }
        }

        private static HttpResponseMessage Sse(params string[] events)
        {
            var builder = new StringBuilder();

            foreach (var evt in events)
            {
                builder.Append("data: ").Append(evt).Append("\n\n");
            }

            builder.Append("data: [DONE]\n\n");

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(builder.ToString(), Encoding.UTF8, "text/event-stream")
            };
        }

        private static (OpenAICompatibleChatClient Client, FakeHandler Handler) CreateClient(Func<HttpRequestMessage, string, HttpResponseMessage> respond, string? apiKey = "secret", string? apiKeyHeader = "Authorization")
        {
            var handler = new FakeHandler(respond);
            var httpClient = new HttpClient(handler);
            return (new OpenAICompatibleChatClient(httpClient, "https://example.com/v1/chat/completions", apiKey, apiKeyHeader, "test-model"), handler);
        }

        [Description("Gets the weather")]
        private static string GetWeather([Description("The city")] string city) => $"Sunny in {city}";

        [Fact]
        public async Task StreamsTextAndAccumulatesToolCallDeltas()
        {
            var (client, _) = CreateClient((_, _) => Sse(
                """{"id":"resp-1","model":"test-model","created":1700000000,"choices":[{"index":0,"delta":{"role":"assistant","content":"Let me "}}]}""",
                """{"id":"resp-1","choices":[{"index":0,"delta":{"content":"check."}}]}""",
                """{"id":"resp-1","choices":[{"index":0,"delta":{"tool_calls":[{"index":0,"id":"call_1","type":"function","function":{"name":"get_weather","arguments":"{\"city\": \""}}]}}]}""",
                """{"id":"resp-1","choices":[{"index":0,"delta":{"tool_calls":[{"index":0,"function":{"arguments":"Sofia\"}"}}]}}]}""",
                """{"id":"resp-1","choices":[{"index":0,"delta":{},"finish_reason":"tool_calls"}]}"""));

            var updates = new List<ChatResponseUpdate>();

            await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "Weather in Sofia?")]))
            {
                updates.Add(update);
            }

            var text = string.Concat(updates.SelectMany(update => update.Contents.OfType<TextContent>()).Select(content => content.Text));
            Assert.Equal("Let me check.", text);

            var call = Assert.Single(updates.SelectMany(update => update.Contents.OfType<FunctionCallContent>()));
            Assert.Equal("call_1", call.CallId);
            Assert.Equal("get_weather", call.Name);
            Assert.NotNull(call.Arguments);
            Assert.Equal("Sofia", ((JsonElement)call.Arguments!["city"]!).GetString());
            Assert.All(updates, update => Assert.Equal("resp-1", update.ResponseId));
            Assert.Equal(ChatFinishReason.ToolCalls, updates.Last().FinishReason);

            var response = updates.ToChatResponse();
            var message = Assert.Single(response.Messages);
            Assert.Equal(ChatRole.Assistant, message.Role);
            Assert.Contains(message.Contents, content => content is FunctionCallContent);
        }

        [Fact]
        public async Task FlushesToolCallsWithoutFinishReason()
        {
            var (client, _) = CreateClient((_, _) => Sse(
                """{"id":"resp-2","choices":[{"index":0,"delta":{"tool_calls":[{"index":0,"id":"call_9","function":{"name":"get_weather","arguments":"{\"city\":\"Varna\"}"}}]}}]}"""));

            var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "Weather in Varna?")]);

            var call = Assert.Single(response.Messages.SelectMany(message => message.Contents.OfType<FunctionCallContent>()));
            Assert.Equal("call_9", call.CallId);
            Assert.Equal("Varna", ((JsonElement)call.Arguments!["city"]!).GetString());
        }

        [Fact]
        public async Task SendsToolsHistoryAndAuthorization()
        {
            var (client, handler) = CreateClient((_, _) => Sse("""{"id":"resp-3","choices":[{"index":0,"delta":{"content":"Sunny."},"finish_reason":"stop"}]}"""));

            var tool = AIFunctionFactory.Create(GetWeather);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, "Weather in Sofia?"),
                new(ChatRole.Assistant, [new FunctionCallContent("call_1", "get_weather", new Dictionary<string, object?> { ["city"] = "Sofia" })]),
                new(ChatRole.Tool, [new FunctionResultContent("call_1", "Sunny in Sofia")])
            };

            var options = new ChatOptions
            {
                Instructions = "Be brief.",
                Temperature = 0.2f,
                MaxOutputTokens = 100,
                Tools = [tool],
                ModelId = "override-model"
            };

            var response = await client.GetResponseAsync(messages, options);
            Assert.Equal("Sunny.", response.Text);

            var (request, body) = Assert.Single(handler.Requests);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
            Assert.Equal("secret", request.Headers.Authorization.Parameter);

            var root = body.RootElement;
            Assert.Equal("override-model", root.GetProperty("model").GetString());
            Assert.True(root.GetProperty("stream").GetBoolean());
            Assert.Equal(0.2, root.GetProperty("temperature").GetDouble(), 3);
            Assert.Equal(100, root.GetProperty("max_tokens").GetInt32());

            var sent = root.GetProperty("messages").EnumerateArray().ToList();
            Assert.Equal(4, sent.Count);
            Assert.Equal("system", sent[0].GetProperty("role").GetString());
            Assert.Equal("Be brief.", sent[0].GetProperty("content").GetString());
            Assert.Equal("user", sent[1].GetProperty("role").GetString());
            Assert.Equal("assistant", sent[2].GetProperty("role").GetString());
            var toolCall = Assert.Single(sent[2].GetProperty("tool_calls").EnumerateArray());
            Assert.Equal("call_1", toolCall.GetProperty("id").GetString());
            Assert.Equal("get_weather", toolCall.GetProperty("function").GetProperty("name").GetString());
            Assert.Equal("""{"city":"Sofia"}""", toolCall.GetProperty("function").GetProperty("arguments").GetString());
            Assert.Equal("tool", sent[3].GetProperty("role").GetString());
            Assert.Equal("call_1", sent[3].GetProperty("tool_call_id").GetString());
            Assert.Equal("Sunny in Sofia", sent[3].GetProperty("content").GetString());

            var sentTool = Assert.Single(root.GetProperty("tools").EnumerateArray());
            Assert.Equal("function", sentTool.GetProperty("type").GetString());
            Assert.Equal("GetWeather", sentTool.GetProperty("function").GetProperty("name").GetString());
            Assert.Equal("Gets the weather", sentTool.GetProperty("function").GetProperty("description").GetString());
            Assert.Equal("object", sentTool.GetProperty("function").GetProperty("parameters").GetProperty("type").GetString());
            Assert.True(sentTool.GetProperty("function").GetProperty("parameters").GetProperty("properties").TryGetProperty("city", out _));
        }

        [Fact]
        public async Task SendsCustomApiKeyHeaderAndNoToolsWhenNoneConfigured()
        {
            var (client, handler) = CreateClient((_, _) => Sse("""{"id":"resp-4","choices":[{"index":0,"delta":{"content":"Hi"}}]}"""), "key-123", "X-Api-Key");

            await client.GetResponseAsync([new ChatMessage(ChatRole.User, "Hi")]);

            var (request, body) = Assert.Single(handler.Requests);
            Assert.Null(request.Headers.Authorization);
            Assert.Equal("key-123", request.Headers.GetValues("X-Api-Key").Single());
            Assert.False(body.RootElement.TryGetProperty("tools", out _));
            Assert.Equal("test-model", body.RootElement.GetProperty("model").GetString());
        }

        [Fact]
        public async Task ThrowsOnErrorStatus()
        {
            var (client, _) = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("""{"errors":[{"code":10000,"message":"Authentication error"}]}""")
            });

            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetResponseAsync([new ChatMessage(ChatRole.User, "Hi")]));
            Assert.Contains("401", exception.Message);
            Assert.Contains("Authentication error", exception.Message);
        }

        [Fact]
        public async Task InvokesToolsWhenWrappedWithFunctionInvocation()
        {
            var round = 0;

            var (client, handler) = CreateClient((_, body) =>
            {
                round++;

                return body.Contains("\"role\":\"tool\"")
                    ? Sse("""{"id":"resp-6","choices":[{"index":0,"delta":{"content":"It is sunny in Sofia."},"finish_reason":"stop"}]}""")
                    : Sse("""{"id":"resp-5","choices":[{"index":0,"delta":{"tool_calls":[{"index":0,"id":"call_1","function":{"name":"GetWeather","arguments":"{\"city\":\"Sofia\"}"}}]},"finish_reason":"tool_calls"}]}""");
            });

            IChatClient pipeline = new FunctionInvokingChatClient(client);

            var response = await pipeline.GetResponseAsync([new ChatMessage(ChatRole.User, "Weather in Sofia?")], new ChatOptions { Tools = [AIFunctionFactory.Create(GetWeather)] });

            Assert.Equal(2, round);
            Assert.Equal("It is sunny in Sofia.", response.Text);
            Assert.Contains(response.Messages, message => message.Role == ChatRole.Tool && message.Contents.OfType<FunctionResultContent>().Any(result => result.CallId == "call_1"));

            var secondBody = handler.Requests[1].Body.RootElement;
            var toolMessage = secondBody.GetProperty("messages").EnumerateArray().Single(message => message.GetProperty("role").GetString() == "tool");
            Assert.Equal("Sunny in Sofia", toolMessage.GetProperty("content").GetString());
        }
    }
}

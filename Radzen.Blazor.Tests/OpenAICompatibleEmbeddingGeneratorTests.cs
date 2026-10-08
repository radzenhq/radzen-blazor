using Bunit;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Radzen;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class OpenAICompatibleEmbeddingGeneratorTests
    {
        private sealed class FakeHandler(Func<HttpRequestMessage, string, HttpResponseMessage> respond) : HttpMessageHandler
        {
            public HttpRequestMessage? Request { get; private set; }

            public JsonDocument? Body { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Request = request;
                var body = await request.Content!.ReadAsStringAsync(cancellationToken);
                Body = JsonDocument.Parse(body);
                return respond(request, body);
            }
        }

        private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };

        [Fact]
        public async Task GeneratesEmbeddingsInInputOrder()
        {
            var handler = new FakeHandler((_, _) => Json("""{"object":"list","data":[{"index":1,"embedding":[0.5,0.5]},{"index":0,"embedding":[1,0]}],"model":"bge","usage":{"prompt_tokens":4,"total_tokens":4}}"""));
            var generator = new OpenAICompatibleEmbeddingGenerator(new HttpClient(handler), "https://example.com/v1/embeddings", "secret", "Authorization", "default-model");

            var result = await generator.GenerateAsync(["first", "second"], new EmbeddingGenerationOptions { Dimensions = 2 });

            Assert.Equal(2, result.Count);
            Assert.Equal(new[] { 1f, 0f }, result[0].Vector.ToArray());
            Assert.Equal(new[] { 0.5f, 0.5f }, result[1].Vector.ToArray());
            Assert.Equal("bge", result[0].ModelId);
            Assert.Equal(4, result.Usage!.TotalTokenCount);

            Assert.Equal("Bearer", handler.Request!.Headers.Authorization!.Scheme);
            var root = handler.Body!.RootElement;
            Assert.Equal("default-model", root.GetProperty("model").GetString());
            Assert.Equal(new[] { "first", "second" }, root.GetProperty("input").EnumerateArray().Select(e => e.GetString()).ToArray());
            Assert.Equal(2, root.GetProperty("dimensions").GetInt32());
            Assert.Equal("openai-compatible", generator.Metadata.ProviderName);
            Assert.Same(generator, generator.GetService<OpenAICompatibleEmbeddingGenerator>());
        }

        [Fact]
        public async Task ThrowsOnErrorStatusAndEmptyInputReturnsEmpty()
        {
            var handler = new FakeHandler((_, _) => new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("""{"errors":[{"message":"Authentication error"}]}""") });
            var generator = new OpenAICompatibleEmbeddingGenerator(new HttpClient(handler), "https://example.com/v1/embeddings");

            Assert.Empty(await generator.GenerateAsync([]));

            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => generator.GenerateAsync(["x"]));
            Assert.Contains("401", exception.Message);
            Assert.Contains("Authentication error", exception.Message);
        }

        [Fact]
        public void AIChatServiceOptions_DerivesEmbeddingsEndpointAndProxy()
        {
            var options = new AIChatServiceOptions { Endpoint = "https://host/v1/chat/completions", Proxy = "api/chat/completions" };

            Assert.Equal("https://host/v1/embeddings", options.GetEmbeddingsEndpoint());
            Assert.Equal("api/embeddings", options.GetEmbeddingsProxy());

            options.EmbeddingsEndpoint = "https://other/embed";
            options.EmbeddingsProxy = "api/embed";
            Assert.Equal("https://other/embed", options.GetEmbeddingsEndpoint());
            Assert.Equal("api/embed", options.GetEmbeddingsProxy());

            Assert.Null(new AIChatServiceOptions { Endpoint = "https://host/v1/chat/completions" }.GetEmbeddingsProxy());
        }

        [Fact]
        public async Task AIChatService_GetEmbeddingGenerator_UsesRegisteredOrBuiltIn()
        {
            using var ctx = new TestContext();
            var handler = new FakeHandler((_, _) => Json("""{"data":[{"index":0,"embedding":[1,2,3]}]}"""));
            ctx.Services.AddSingleton(new HttpClient(handler));
            ctx.Services.AddAIChatService(options =>
            {
                options.Endpoint = "https://host/v1/chat/completions";
                options.ApiKey = "key";
                options.EmbeddingsModel = "embed-model";
            });

            var service = ctx.Services.GetRequiredService<IAIChatService>();
            var generator = service.GetEmbeddingGenerator();

            Assert.Same(generator, service.GetEmbeddingGenerator());
            Assert.NotNull(generator.GetService<OpenAICompatibleEmbeddingGenerator>());

            var result = await generator.GenerateAsync(["hello"]);
            Assert.Equal(new[] { 1f, 2f, 3f }, result[0].Vector.ToArray());
            Assert.Equal("https://host/v1/embeddings", handler.Request!.RequestUri!.ToString());
            Assert.Equal("embed-model", handler.Body!.RootElement.GetProperty("model").GetString());

            using var ctx2 = new TestContext();
            var registered = new OpenAICompatibleEmbeddingGenerator(new HttpClient(handler), "https://registered/embeddings");
            ctx2.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(registered);
            ctx2.Services.AddAIChatService();
            Assert.Same(registered, ctx2.Services.GetRequiredService<IAIChatService>().GetEmbeddingGenerator());
        }
    }
}

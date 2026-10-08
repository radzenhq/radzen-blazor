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
    public class AIComponentsTests
    {
        private sealed class FuncChatClient(Func<IList<Microsoft.Extensions.AI.ChatMessage>, ChatOptions?, string> respond) : IChatClient
        {
            public List<(IList<Microsoft.Extensions.AI.ChatMessage> Messages, ChatOptions? Options)> Requests { get; } = new();

            public Task<ChatResponse> GetResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            {
                var list = messages.ToList();
                Requests.Add((list, options));
                return Task.FromResult(new ChatResponse(new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, respond(list, options))));
            }

            public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                var list = messages.ToList();
                Requests.Add((list, options));
                await Task.Yield();

                foreach (var word in respond(list, options).Split(' '))
                {
                    yield return new ChatResponseUpdate(ChatRole.Assistant, word + " ") { ResponseId = "r", MessageId = "r" };
                }
            }

            public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;

            public void Dispose()
            {
            }
        }

        public enum CustomerKind
        {
            Retail,
            Wholesale
        }

        public class Customer
        {
            [Description("The company name")]
            public string? Company { get; set; }

            public string? Contact { get; set; }

            public decimal CreditLimit { get; set; }

            public int? Employees { get; set; }

            public DateTime Since { get; set; }

            public bool Active { get; set; }

            public CustomerKind Kind { get; set; }

            public List<string> Tags { get; set; } = new();
        }

        private static (TestContext Context, FuncChatClient Client) CreateContext(Func<IList<Microsoft.Extensions.AI.ChatMessage>, ChatOptions?, string> respond)
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var client = new FuncChatClient(respond);
            ctx.Services.AddSingleton<IChatClient>(client);
            ctx.Services.AddAIChatService();
            return (ctx, client);
        }

        [Fact]
        public async Task SmartPasteButton_FillsModelFromText()
        {
            var (ctx, client) = CreateContext((_, _) => """{"company":"Alfreds Futterkiste","contact":"Maria Anders","creditLimit":"12,500.50","employees":42,"since":"2023-05-01","active":"yes","kind":"wholesale","tags":["a"]}""");
            using var _ = ctx;

            var customer = new Customer();
            Customer? pasted = null;

            var component = ctx.RenderComponent<RadzenSmartPasteButton<Customer>>(parameters => parameters
                .Add(p => p.Data, customer)
                .Add(p => p.Pasted, c => pasted = c));

            Assert.Contains("Smart paste", component.Markup);
            Assert.Contains("content_paste_go", component.Markup);

            await component.InvokeAsync(() => component.Instance.Paste("Alfreds Futterkiste, contact Maria Anders, credit 12,500.50, 42 employees, customer since May 2023, active, wholesale"));

            Assert.Same(customer, pasted);
            Assert.Equal("Alfreds Futterkiste", customer.Company);
            Assert.Equal("Maria Anders", customer.Contact);
            Assert.Equal(12500.50m, customer.CreditLimit);
            Assert.Equal(42, customer.Employees);
            Assert.Equal(new DateTime(2023, 5, 1), customer.Since);
            Assert.True(customer.Active);
            Assert.Equal(CustomerKind.Wholesale, customer.Kind);
            Assert.Empty(customer.Tags);

            var (messages, options) = Assert.Single(client.Requests);
            Assert.Equal(ChatRole.User, messages.Single().Role);
            Assert.IsType<ChatResponseFormatJson>(options!.ResponseFormat);
            Assert.Contains("- Company (text): The company name", options.Instructions);
            Assert.Contains("- Kind (one of: Retail, Wholesale)", options.Instructions);
            Assert.DoesNotContain("Tags", options.Instructions);
            Assert.False(component.Instance.IsBusy);
        }

        [Fact]
        public async Task SmartPasteButton_RespectsFieldsAndSkipsNulls()
        {
            var (ctx, client) = CreateContext((_, _) => """{"Company":"Berglunds","Contact":null}""");
            using var _ = ctx;

            var customer = new Customer { Contact = "Keep me", CreditLimit = 7 };

            var component = ctx.RenderComponent<RadzenSmartPasteButton<Customer>>(parameters => parameters
                .Add(p => p.Data, customer)
                .Add(p => p.Fields, new[] { "Company", "Contact" }));

            await component.InvokeAsync(() => component.Instance.Paste("Berglunds snabbköp"));

            Assert.Equal("Berglunds", customer.Company);
            Assert.Equal("Keep me", customer.Contact);
            Assert.Equal(7, customer.CreditLimit);
            Assert.DoesNotContain("CreditLimit", client.Requests.Single().Options!.Instructions);
        }

        [Fact]
        public async Task SmartPasteButton_ReadsClipboardOnClick()
        {
            var (ctx, client) = CreateContext((messages, _) => messages.Single().Text.Contains("clip") ? """{"Company":"From clipboard"}""" : "{}");
            using var _ = ctx;

            ctx.JSInterop.Setup<string?>("Radzen.readClipboardText").SetResult("clip text");

            var customer = new Customer();
            var component = ctx.RenderComponent<RadzenSmartPasteButton<Customer>>(parameters => parameters.Add(p => p.Data, customer));

            await component.Find("button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            component.WaitForAssertion(() => Assert.Equal("From clipboard", customer.Company));
        }

        [Fact]
        public async Task SmartPasteButton_RaisesEmptyClipboard()
        {
            var (ctx, client) = CreateContext((_, _) => "{}");
            using var _ = ctx;

            ctx.JSInterop.Setup<string?>("Radzen.readClipboardText").SetResult(null);

            var empty = false;
            var component = ctx.RenderComponent<RadzenSmartPasteButton<Customer>>(parameters => parameters
                .Add(p => p.Data, new Customer())
                .Add(p => p.EmptyClipboard, () => empty = true));

            await component.Find("button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

            component.WaitForAssertion(() => Assert.True(empty));
            Assert.Empty(client.Requests);
        }

        [Fact]
        public async Task AIPrompt_GeneratesFromSuggestionAndStreamsOutput()
        {
            var (ctx, client) = CreateContext((messages, _) => "**Answer** to " + messages.Single().Text);
            using var _ = ctx;

            string? value = null;
            string? output = null;
            AIPromptGeneratedEventArgs? generated = null;

            var component = ctx.RenderComponent<RadzenAIPrompt>(parameters => parameters
                .Add(p => p.Suggestions, new[] { "Summarize", "Translate" })
                .Add(p => p.SystemPrompt, "Be brief.")
                .Add(p => p.ValueChanged, v => value = v)
                .Add(p => p.OutputChanged, o => output = o)
                .Add(p => p.Generated, args => generated = args));

            Assert.Equal(2, component.FindAll(".rz-aiprompt-suggestion").Count);
            Assert.Contains("Ask AI...", component.Markup);

            await component.InvokeAsync(() => component.FindAll(".rz-aiprompt-suggestion")[0].Click());

            component.WaitForAssertion(() => Assert.False(component.Instance.IsGenerating));

            Assert.Equal("Summarize", value);
            Assert.Equal("**Answer** to Summarize ", output);
            Assert.NotNull(generated);
            Assert.Equal("Summarize", generated!.Prompt);
            Assert.Null(generated.Command);
            Assert.Contains("<strong>Answer</strong>", component.Markup);
            Assert.Equal("Be brief.", client.Requests.Single().Options!.Instructions);
            Assert.Contains("rz-aiprompt-copy", component.Markup);
        }

        [Fact]
        public async Task AIPrompt_SendsContextAndAppliesCommands()
        {
            var (ctx, client) = CreateContext((messages, _) => messages.Single().Text.Contains("Instruction: Shorter") ? "short" : "long answer here");
            using var _ = ctx;

            var component = ctx.RenderComponent<RadzenAIPrompt>(parameters => parameters
                .Add(p => p.Context, "Some long text")
                .Add(p => p.Commands, new[] { new AIPromptCommand { Text = "Shorter", Prompt = "Shorter" } }));

            await component.InvokeAsync(() => component.Instance.Generate("Summarize"));
            component.WaitForAssertion(() => Assert.False(component.Instance.IsGenerating));

            var first = client.Requests[0].Messages.Single().Text;
            Assert.Contains("Text:\nSome long text", first);
            Assert.Contains("Instruction: Summarize", first);
            Assert.Equal("long answer here ", component.Instance.Output);

            await component.InvokeAsync(() => component.Find(".rz-aiprompt-command").Click());
            component.WaitForAssertion(() => Assert.Equal("short ", component.Instance.Output));

            var second = client.Requests[1].Messages.Single().Text;
            Assert.Contains("Text:\nlong answer here", second);
            Assert.Contains("Instruction: Shorter", second);
        }

        [Fact]
        public async Task AIPrompt_ShowsErrorAsOutputWhenNoErrorHandler()
        {
            var (ctx, _) = CreateContext((_, _) => throw new InvalidOperationException("boom"));
            using var __ = ctx;

            var component = ctx.RenderComponent<RadzenAIPrompt>();

            await component.InvokeAsync(() => component.Instance.Generate("Hi"));
            component.WaitForAssertion(() => Assert.False(component.Instance.IsGenerating));

            Assert.Equal("boom", component.Instance.Output);
            Assert.Contains("boom", component.Markup);
        }

        [Fact]
        public async Task AIChatService_GetChatClient_WrapsRegisteredClientWithFunctionInvocation()
        {
            var (ctx, client) = CreateContext((_, _) => "ok");
            using var _ = ctx;

            var service = ctx.Services.GetRequiredService<IAIChatService>();
            var chatClient = service.GetChatClient();

            Assert.NotNull(chatClient.GetService<FunctionInvokingChatClient>());
            Assert.Same(chatClient, service.GetChatClient());
            Assert.Equal("ok", (await chatClient.GetResponseAsync("hi")).Text);
            Assert.Same(client, chatClient.GetService<FuncChatClient>());
        }
    }
}

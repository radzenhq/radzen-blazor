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

        [Fact]
        public async Task InlineAIPrompt_ReplacesAppendsAndDiscardsOutput()
        {
            var (ctx, client) = CreateContext((messages, _) => "Improved text");
            using var _ = ctx;

            string? value = "original text";
            var applied = new List<string?>();

            var component = ctx.RenderComponent<RadzenInlineAIPrompt>(parameters => parameters
                .Add(p => p.Value, value)
                .Add(p => p.ValueChanged, v => value = v)
                .Add(p => p.Applied, v => applied.Add(v))
                .Add(p => p.Suggestions, new[] { "Fix grammar" }));

            Assert.Contains("rz-inline-aiprompt-button", component.Markup);
            Assert.DoesNotContain("rz-aiprompt-textarea", component.Markup);

            await component.InvokeAsync(() => component.Find("button.rz-inline-aiprompt-button").Click());
            component.WaitForAssertion(() => Assert.Contains("rz-aiprompt-textarea", component.Markup));
            Assert.True(component.Instance.IsOpen);

            var prompt = component.FindComponent<RadzenAIPrompt>();
            Assert.Equal("original text", prompt.Instance.Context);
            Assert.Contains("return only the resulting text", prompt.Instance.SystemPrompt);

            await component.InvokeAsync(() => prompt.Instance.Generate("Fix grammar"));
            component.WaitForAssertion(() => Assert.Contains("rz-inline-aiprompt-actions", component.Markup));
            Assert.Equal("Improved text", component.Instance.Output?.Trim());
            Assert.Contains("original text", client.Requests.Last().Messages.Last().Text);

            await component.InvokeAsync(() => component.Find("button.rz-inline-aiprompt-replace").Click());
            Assert.Equal("Improved text", value?.Trim());
            Assert.Equal("Improved text", applied.Single()?.Trim());

            value = "first line\nsecond line";
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Value, value));
            await component.InvokeAsync(() => component.Instance.OpenAsync());
            component.WaitForAssertion(() => Assert.Contains("rz-aiprompt-textarea", component.Markup));
            await component.InvokeAsync(() => component.FindComponent<RadzenAIPrompt>().Instance.Generate("More"));
            component.WaitForAssertion(() => Assert.Contains("rz-inline-aiprompt-append", component.Markup));
            await component.InvokeAsync(() => component.Find("button.rz-inline-aiprompt-append").Click());
            Assert.Equal("first line\nsecond line\n\nImproved text", value?.Trim());

            await component.InvokeAsync(() => component.Instance.OpenAsync());
            component.WaitForAssertion(() => Assert.Contains("rz-aiprompt-textarea", component.Markup));
            await component.InvokeAsync(() => component.FindComponent<RadzenAIPrompt>().Instance.Generate("More"));
            component.WaitForAssertion(() => Assert.Contains("rz-inline-aiprompt-discard", component.Markup));
            await component.InvokeAsync(() => component.Find("button.rz-inline-aiprompt-discard").Click());
            component.WaitForAssertion(() => Assert.DoesNotContain("rz-inline-aiprompt-actions", component.Markup));
            Assert.Null(component.Instance.Output);
            Assert.Equal(2, applied.Count);
        }

        [Fact]
        public async Task InlineAIPrompt_ReplacesOnlyTheSelectionOfTheTarget()
        {
            var (ctx, client) = CreateContext((messages, _) => "QUICK");
            using var _ = ctx;
            ctx.JSInterop.Setup<int[]?>("Radzen.getSelectionRange", "description").SetResult(new[] { 4, 9 });

            string? value = "the quick fox";

            var component = ctx.RenderComponent<RadzenInlineAIPrompt>(parameters => parameters
                .Add(p => p.Value, value)
                .Add(p => p.ValueChanged, v => value = v)
                .Add(p => p.TargetId, "description")
                .Add(p => p.ReplaceSelectionText, "Only selection"));

            await component.InvokeAsync(() => component.Instance.OpenAsync());
            component.WaitForAssertion(() => Assert.Contains("rz-aiprompt-textarea", component.Markup));

            var prompt = component.FindComponent<RadzenAIPrompt>();
            Assert.Equal("quick", prompt.Instance.Context);

            await component.InvokeAsync(() => prompt.Instance.Generate("Uppercase"));
            component.WaitForAssertion(() => Assert.Contains("Only selection", component.Markup));
            Assert.Contains("quick", client.Requests.Single().Messages.Last().Text);

            await component.InvokeAsync(() => component.Find("button.rz-inline-aiprompt-replace").Click());
            Assert.Equal("the QUICK  fox", value);
        }

        [Fact]
        public void InlineAIPrompt_CanHideTheButtonAndRendersAccessibleNames()
        {
            var (ctx, _) = CreateContext((_, _) => "x");
            using var __ = ctx;

            var component = ctx.RenderComponent<RadzenInlineAIPrompt>(parameters => parameters.Add(p => p.Title, "Improve").Add(p => p.Text, "Ask AI"));
            var button = component.Find("button.rz-inline-aiprompt-button");
            Assert.Equal("Improve", button.GetAttribute("title"));
            Assert.Equal("dialog", button.GetAttribute("aria-haspopup"));
            Assert.Equal("false", button.GetAttribute("aria-expanded"));
            Assert.Contains("Ask AI", button.TextContent);
            Assert.Equal("Improve", component.Find(".rz-inline-aiprompt-popup").GetAttribute("aria-label"));

            var hidden = ctx.RenderComponent<RadzenInlineAIPrompt>(parameters => parameters.Add(p => p.ShowButton, false));
            Assert.Empty(hidden.FindAll("button.rz-inline-aiprompt-button"));
            Assert.Single(hidden.FindAll(".rz-inline-aiprompt-popup"));
        }
    }
}

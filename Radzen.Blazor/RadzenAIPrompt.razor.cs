using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A prompt box for one-shot AI tasks: the user types or picks a prompt, the model's answer streams into an output panel and
    /// commands such as "Shorter" or "Translate" refine it. Set <see cref="Context"/> to run the prompts against a piece of text,
    /// for example the content of an editor. Uses the <see cref="IChatClient"/> configured for <see cref="IAIChatService"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenAIPrompt Suggestions="@(new[] { "Summarize the text", "Translate to German" })" Context="@text" @bind-Output="@result" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenAIPrompt : RadzenComponent
    {
        [Inject]
        IServiceProvider ServiceProvider { get; set; } = default!;

        private bool preventDefault;
        private string? lastPrompt;
        private AIPromptCommand? lastCommand;
        private CancellationTokenSource? cts;

        /// <summary>
        /// Gets or sets the prompt text.
        /// </summary>
        [Parameter]
        public string? Value { get; set; }

        /// <summary>
        /// Event callback that is invoked when the prompt text changes.
        /// </summary>
        [Parameter]
        public EventCallback<string?> ValueChanged { get; set; }

        /// <summary>
        /// Gets or sets the generated output.
        /// </summary>
        [Parameter]
        public string? Output { get; set; }

        /// <summary>
        /// Event callback that is invoked when the output changes, including while it streams.
        /// </summary>
        [Parameter]
        public EventCallback<string?> OutputChanged { get; set; }

        /// <summary>
        /// Gets or sets the text the prompts apply to, for example the content of an editor or the selected text. When set, the model receives it together with the prompt.
        /// </summary>
        [Parameter]
        public string? Context { get; set; }

        /// <summary>
        /// Gets or sets the system prompt.
        /// </summary>
        [Parameter]
        public string? SystemPrompt { get; set; }

        /// <summary>
        /// Gets or sets the prompts offered as suggestions under the input.
        /// </summary>
        [Parameter]
        public IEnumerable<string>? Suggestions { get; set; }

        /// <summary>
        /// Gets or sets the commands shown under the output. A command sends the current output together with its <see cref="AIPromptCommand.Prompt"/> and replaces the output with the result.
        /// </summary>
        [Parameter]
        public IEnumerable<AIPromptCommand>? Commands { get; set; }

        /// <summary>
        /// Gets or sets whether the output panel is rendered. Set to <c>false</c> to show the output elsewhere via <see cref="OutputChanged"/>.
        /// </summary>
        [Parameter]
        public bool ShowOutput { get; set; } = true;

        /// <summary>
        /// Gets or sets a template that renders the output instead of the default Markdown rendering.
        /// </summary>
        [Parameter]
        public RenderFragment<string?>? OutputTemplate { get; set; }

        /// <summary>
        /// Specifies additional custom attributes that will be rendered by the input.
        /// </summary>
        [Parameter]
        public IReadOnlyDictionary<string, object>? InputAttributes { get; set; }

        private string? placeholder;

        /// <summary>
        /// Gets or sets the placeholder of the input.
        /// </summary>
        [Parameter]
        public string Placeholder { get => placeholder ?? Localize(nameof(RadzenStrings.AIPrompt_Placeholder)); set => placeholder = value; }

        private string? generateText;

        /// <summary>
        /// Gets or sets the text of the generate button.
        /// </summary>
        [Parameter]
        public string GenerateText { get => generateText ?? Localize(nameof(RadzenStrings.AIPrompt_GenerateText)); set => generateText = value; }

        private string? stopText;

        /// <summary>
        /// Gets or sets the text of the stop button shown while generating.
        /// </summary>
        [Parameter]
        public string StopText { get => stopText ?? Localize(nameof(RadzenStrings.AIPrompt_StopText)); set => stopText = value; }

        private string? copyTitle;

        /// <summary>
        /// Gets or sets the title of the copy button.
        /// </summary>
        [Parameter]
        public string CopyTitle { get => copyTitle ?? Localize(nameof(RadzenStrings.AIPrompt_CopyTitle)); set => copyTitle = value; }

        private string? retryTitle;

        /// <summary>
        /// Gets or sets the title of the regenerate button.
        /// </summary>
        [Parameter]
        public string RetryTitle { get => retryTitle ?? Localize(nameof(RadzenStrings.AIPrompt_RetryTitle)); set => retryTitle = value; }

        /// <summary>
        /// Gets or sets whether the component is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets the model name. Falls back to the configured <see cref="AIChatServiceOptions.Model"/>.
        /// </summary>
        [Parameter]
        public string? ModelId { get; set; }

        /// <summary>
        /// Gets or sets the temperature.
        /// </summary>
        [Parameter]
        public double? Temperature { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of output tokens.
        /// </summary>
        [Parameter]
        public int? MaxTokens { get; set; }

        /// <summary>
        /// Event callback that is invoked when the model finishes generating.
        /// </summary>
        [Parameter]
        public EventCallback<AIPromptGeneratedEventArgs> Generated { get; set; }

        /// <summary>
        /// Event callback that is invoked when the request fails. When not set the error message is shown as the output.
        /// </summary>
        [Parameter]
        public EventCallback<Exception> Error { get; set; }

        /// <summary>
        /// Gets whether a response is being generated.
        /// </summary>
        public bool IsGenerating { get; private set; }

        /// <inheritdoc />
        protected override string GetComponentCssClass() => ClassList.Create("rz-aiprompt").Add("rz-state-disabled", Disabled).ToString();

        /// <summary>
        /// Sends a prompt to the model and streams the answer into <see cref="Output"/>.
        /// </summary>
        /// <param name="prompt">The prompt. Uses <see cref="Value"/> when not specified.</param>
        public async Task Generate(string? prompt = null)
        {
            prompt ??= Value;

            if (string.IsNullOrWhiteSpace(prompt) || Disabled)
            {
                return;
            }

            if (prompt != Value)
            {
                Value = prompt;
                await ValueChanged.InvokeAsync(prompt);
            }

            await Run(prompt, null, Context);
        }

        /// <summary>
        /// Applies a command to the current output and replaces it with the result.
        /// </summary>
        /// <param name="command">The command.</param>
        public async Task ExecuteCommand(AIPromptCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);

            if (string.IsNullOrEmpty(Output) || Disabled)
            {
                return;
            }

            await Run(command.Prompt, command, Output);
        }

        /// <summary>
        /// Stops the current generation and keeps the output received so far.
        /// </summary>
        public void Stop()
        {
            cts?.Cancel();
        }

        /// <summary>
        /// Clears the output.
        /// </summary>
        public async Task Clear()
        {
            Stop();
            Output = null;
            lastPrompt = null;
            lastCommand = null;
            await OutputChanged.InvokeAsync(null);
            await InvokeAsync(StateHasChanged);
        }

        private async Task Retry()
        {
            if (lastPrompt == null)
            {
                return;
            }

            if (lastCommand != null)
            {
                await Run(lastPrompt, lastCommand, lastCommandInput);
            }
            else
            {
                await Run(lastPrompt, null, Context);
            }
        }

        private string? lastCommandInput;

        private async Task Run(string prompt, AIPromptCommand? command, string? context)
        {
            Stop();
            cts?.Dispose();
            cts = new CancellationTokenSource();
            var token = cts.Token;

            lastPrompt = prompt;
            lastCommand = command;
            lastCommandInput = command != null ? context : null;

            IsGenerating = true;
            Output = string.Empty;
            await OutputChanged.InvokeAsync(Output);
            await InvokeAsync(StateHasChanged);

            var text = new StringBuilder();

            try
            {
                var client = ServiceProvider.GetService<IAIChatService>()?.GetChatClient() ?? ServiceProvider.GetService<IChatClient>() ?? throw new InvalidOperationException("Register an IChatClient or call AddAIChatService to use RadzenAIPrompt.");

                var options = new ChatOptions
                {
                    ModelId = ModelId,
                    Instructions = SystemPrompt,
                    Temperature = Temperature.HasValue ? (float)Temperature.Value : null,
                    MaxOutputTokens = MaxTokens
                };

                var content = string.IsNullOrWhiteSpace(context) ? prompt : $"Text:\n{context}\n\nInstruction: {prompt}\n\nRespond with the result only, without preamble.";

                await foreach (var update in client.GetStreamingResponseAsync([new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, content)], options, token))
                {
                    foreach (var part in update.Contents.OfType<TextContent>())
                    {
                        text.Append(part.Text);
                    }

                    Output = text.ToString();
                    await OutputChanged.InvokeAsync(Output);
                    await InvokeAsync(StateHasChanged);
                }

                await Generated.InvokeAsync(new AIPromptGeneratedEventArgs { Prompt = prompt, Command = command, Output = Output ?? string.Empty });
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                if (Error.HasDelegate)
                {
                    await Error.InvokeAsync(exception);
                }
                else
                {
                    Output = exception.Message;
                    await OutputChanged.InvokeAsync(Output);
                }
            }
            finally
            {
                if (cts == null || cts.Token == token)
                {
                    IsGenerating = false;
                }

                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task OnInput(ChangeEventArgs args)
        {
            Value = args.Value?.ToString();
            await ValueChanged.InvokeAsync(Value);
        }

        private async Task OnKeyDown(KeyboardEventArgs args)
        {
            if (args.Key == "Enter" && !args.ShiftKey && !IsGenerating)
            {
                preventDefault = true;
                await Generate();
            }
            else
            {
                preventDefault = false;
            }
        }

        private async Task Copy()
        {
            if (JSRuntime != null && !string.IsNullOrEmpty(Output))
            {
                await JSRuntime.InvokeVoidAsync("Radzen.copyToClipboard", Output);
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
            GC.SuppressFinalize(this);
        }
    }
}

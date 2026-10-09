using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// An AI assistant for a text field: a button that opens a <see cref="RadzenAIPrompt"/> in a popup and applies the result to the bound value.
    /// Bind <see cref="Value"/> to the same value as the field; set <see cref="TargetId"/> to the id of the input or textarea so that a selection inside it becomes the context and only the selection is replaced.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenTextArea id="description" @bind-Value="description" /&gt;
    /// &lt;RadzenInlineAIPrompt @bind-Value="description" TargetId="description" Suggestions="@(new[] { "Fix grammar", "Make it shorter" })" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenInlineAIPrompt : RadzenComponent
    {
        private const string DefaultSystemPrompt = "You are a writing assistant for a form field. Apply the instruction to the text and return only the resulting text, without explanations, quotes or markdown formatting.";

        private RadzenButton? button;
        private RadzenPopup? popup;
        private RadzenAIPrompt? prompt;
        private string? output;
        private bool ready;
        private string? context;
        private bool hasSelection;
        private int selectionStart;
        private int selectionEnd;
        private string? title;
        private string? replaceText;
        private string? replaceSelectionText;
        private string? appendText;
        private string? discardText;

        /// <summary>
        /// Gets or sets the text of the field the assistant works on.
        /// </summary>
        [Parameter]
        public string? Value { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the assistant changes the value.
        /// </summary>
        [Parameter]
        public EventCallback<string?> ValueChanged { get; set; }

        /// <summary>
        /// Gets or sets the id of the input or textarea the assistant works on. When set, the text selected in it when the popup opens is used as context and replaced by the result;
        /// without a selection, or without a target, the whole <see cref="Value"/> is used.
        /// </summary>
        [Parameter]
        public string? TargetId { get; set; }

        /// <summary>
        /// Gets or sets the system prompt. The default asks the model to return only the resulting text without explanations or markdown.
        /// </summary>
        [Parameter]
        public string SystemPrompt { get; set; } = DefaultSystemPrompt;

        /// <summary>
        /// Gets or sets the prompts offered as buttons, for example "Fix grammar" or "Make it shorter".
        /// </summary>
        [Parameter]
        public IEnumerable<string>? Suggestions { get; set; }

        /// <summary>
        /// Gets or sets the commands that refine the output, for example shorter, formal or translate.
        /// </summary>
        [Parameter]
        public IEnumerable<AIPromptCommand>? Commands { get; set; }

        /// <summary>
        /// Gets or sets the placeholder of the prompt input.
        /// </summary>
        [Parameter]
        public string? Placeholder { get; set; }

        /// <summary>
        /// Gets or sets the template that renders the output instead of markdown.
        /// </summary>
        [Parameter]
        public RenderFragment<string?>? OutputTemplate { get; set; }

        /// <summary>
        /// Gets or sets the model. Uses the model of <see cref="AIChatServiceOptions"/> when not set.
        /// </summary>
        [Parameter]
        public string? ModelId { get; set; }

        /// <summary>
        /// Gets or sets the sampling temperature.
        /// </summary>
        [Parameter]
        public double? Temperature { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of output tokens.
        /// </summary>
        [Parameter]
        public int? MaxTokens { get; set; }

        /// <summary>
        /// Gets or sets whether the button that opens the popup is rendered. Set to <c>false</c> and call <see cref="OpenAsync(ElementReference)"/> to open the popup from your own trigger. Default is <c>true</c>.
        /// </summary>
        [Parameter]
        public bool ShowButton { get; set; } = true;

        /// <summary>
        /// Gets or sets the icon of the button. Default is <c>auto_awesome</c>.
        /// </summary>
        [Parameter]
        public string Icon { get; set; } = "auto_awesome";

        /// <summary>
        /// Gets or sets the color of the icon.
        /// </summary>
        [Parameter]
        public string? IconColor { get; set; }

        /// <summary>
        /// Gets or sets the text of the button. The button shows only the icon when not set.
        /// </summary>
        [Parameter]
        public string? Text { get; set; }

        /// <summary>
        /// Gets or sets the title of the button and the accessible name of the popup. Default is "AI assistant".
        /// </summary>
        [Parameter]
        public string Title { get => title ?? Localize(nameof(RadzenStrings.InlineAIPrompt_Title)); set => title = value; }

        /// <summary>
        /// Gets or sets the text of the button that replaces the value with the output. Default is "Replace".
        /// </summary>
        [Parameter]
        public string ReplaceText { get => replaceText ?? Localize(nameof(RadzenStrings.InlineAIPrompt_ReplaceText)); set => replaceText = value; }

        /// <summary>
        /// Gets or sets the text of the replace button when a selection is replaced. Default is "Replace selection".
        /// </summary>
        [Parameter]
        public string ReplaceSelectionText { get => replaceSelectionText ?? Localize(nameof(RadzenStrings.InlineAIPrompt_ReplaceSelectionText)); set => replaceSelectionText = value; }

        /// <summary>
        /// Gets or sets the text of the button that appends the output to the value. Default is "Append".
        /// </summary>
        [Parameter]
        public string AppendText { get => appendText ?? Localize(nameof(RadzenStrings.InlineAIPrompt_AppendText)); set => appendText = value; }

        /// <summary>
        /// Gets or sets the text of the button that discards the output. Default is "Discard".
        /// </summary>
        [Parameter]
        public string DiscardText { get => discardText ?? Localize(nameof(RadzenStrings.InlineAIPrompt_DiscardText)); set => discardText = value; }

        /// <summary>
        /// Gets or sets the style of the button. Default is <see cref="ButtonStyle.Base"/>.
        /// </summary>
        [Parameter]
        public ButtonStyle ButtonStyle { get; set; } = ButtonStyle.Base;

        /// <summary>
        /// Gets or sets the variant of the button. Default is <see cref="Variant.Text"/>.
        /// </summary>
        [Parameter]
        public Variant Variant { get; set; } = Variant.Text;

        /// <summary>
        /// Gets or sets the shade of the button.
        /// </summary>
        [Parameter]
        public Shade Shade { get; set; } = Shade.Default;

        /// <summary>
        /// Gets or sets the size of the button. Default is <see cref="ButtonSize.Medium"/>.
        /// </summary>
        [Parameter]
        public ButtonSize Size { get; set; } = ButtonSize.Medium;

        /// <summary>
        /// Gets or sets whether the button is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the model produces an output.
        /// </summary>
        [Parameter]
        public EventCallback<AIPromptGeneratedEventArgs> Generated { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked after the output is applied to the value, with the new value.
        /// </summary>
        [Parameter]
        public EventCallback<string?> Applied { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when a request fails.
        /// </summary>
        [Parameter]
        public EventCallback<Exception> Error { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the popup opens.
        /// </summary>
        [Parameter]
        public EventCallback Open { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the popup closes.
        /// </summary>
        [Parameter]
        public EventCallback Close { get; set; }

        /// <summary>
        /// Gets whether the popup is open.
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// Gets the output of the last generation, until it is applied or discarded.
        /// </summary>
        public string? Output => output;

        private string PopupId => $"{GetId()}-popup";

        /// <inheritdoc />
        protected override string GetComponentCssClass() => ClassList.Create("rz-inline-aiprompt").Add("rz-state-disabled", Disabled).ToString();

        /// <summary>
        /// Opens the popup anchored to the button, or to the component itself when <see cref="ShowButton"/> is <c>false</c>.
        /// </summary>
        public Task OpenAsync() => OpenAsync(button?.Element ?? Element);

        /// <summary>
        /// Opens the popup anchored to the specified element.
        /// </summary>
        /// <param name="anchor">The element the popup is positioned against.</param>
        public async Task OpenAsync(ElementReference anchor)
        {
            if (Disabled || popup == null || IsOpen)
            {
                return;
            }

            await ReadSelection();

            await popup.ToggleAsync(anchor);
        }

        /// <summary>
        /// Closes the popup.
        /// </summary>
        public async Task CloseAsync()
        {
            if (popup != null && IsOpen)
            {
                await popup.CloseAsync();
            }
        }

        private async Task ReadSelection()
        {
            hasSelection = false;
            context = Value;

            if (string.IsNullOrEmpty(TargetId) || string.IsNullOrEmpty(Value) || JSRuntime == null)
            {
                return;
            }

            try
            {
                var range = await JSRuntime.InvokeAsync<int[]?>("Radzen.getSelectionRange", TargetId);

                if (range is { Length: 2 } && range[0] >= 0 && range[1] > range[0] && range[1] <= Value.Length)
                {
                    selectionStart = range[0];
                    selectionEnd = range[1];
                    hasSelection = true;
                    context = Value.Substring(selectionStart, selectionEnd - selectionStart);
                }
            }
            catch (JSException)
            {
            }
        }

        private async Task OnPopupOpen()
        {
            IsOpen = true;
            await Open.InvokeAsync();
        }

        private async Task OnPopupClose()
        {
            IsOpen = false;
            output = null;
            await Close.InvokeAsync();
        }

        private void OnOutputChanged(string? value)
        {
            output = value;

            if (string.IsNullOrEmpty(value))
            {
                ready = false;
            }
        }

        private async Task OnGenerated(AIPromptGeneratedEventArgs args)
        {
            output = args.Output;
            ready = true;
            await Generated.InvokeAsync(args);
        }

        private Task Replace()
        {
            var text = output ?? string.Empty;

            if (hasSelection && Value != null && selectionEnd <= Value.Length)
            {
                return Apply(Value.Substring(0, selectionStart) + text + Value.Substring(selectionEnd));
            }

            return Apply(text);
        }

        private Task Append()
        {
            var text = output ?? string.Empty;

            if (string.IsNullOrEmpty(Value))
            {
                return Apply(text);
            }

            var separator = Value.Contains('\n', StringComparison.Ordinal) ? "\n\n" : " ";

            return Apply(Value.TrimEnd() + separator + text);
        }

        private async Task Apply(string value)
        {
            Value = value;
            await ValueChanged.InvokeAsync(value);
            await Applied.InvokeAsync(value);
            await Discard();
            await CloseAsync();
        }

        private async Task Discard()
        {
            if (prompt != null)
            {
                await prompt.Clear();
            }

            output = null;
            ready = false;
        }
    }
}

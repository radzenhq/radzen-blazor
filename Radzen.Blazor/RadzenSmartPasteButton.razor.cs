using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A button that fills a form model from the text in the clipboard with the help of an AI model. Reads the clipboard, asks the
    /// <see cref="IChatClient"/> configured for <see cref="IAIChatService"/> to extract the public properties of <typeparamref name="TModel"/>
    /// from it and assigns the values. Put it inside a <see cref="RadzenTemplateForm{TItem}"/> bound to the same model.
    /// </summary>
    /// <typeparam name="TModel">The type of the form model.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenTemplateForm Data="@customer"&gt;
    ///     &lt;RadzenSmartPasteButton Data="@customer" Pasted="@(c =&gt; StateHasChanged())" /&gt;
    ///     ...
    /// &lt;/RadzenTemplateForm&gt;
    /// </code>
    /// </example>
    public partial class RadzenSmartPasteButton<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TModel> : RadzenComponent where TModel : class
    {
        [Inject]
        IServiceProvider ServiceProvider { get; set; } = default!;

        [CascadingParameter]
        EditContext? EditContext { get; set; }

        /// <summary>
        /// Gets or sets the model to fill. Required.
        /// </summary>
        [Parameter]
        public TModel Data { get; set; } = default!;

        /// <summary>
        /// Gets or sets the names of the properties to fill. All public writable properties of simple types are filled when not set.
        /// </summary>
        [Parameter]
        public IEnumerable<string>? Fields { get; set; }

        /// <summary>
        /// Gets or sets additional instructions for the model, for example the expected language or format of a field.
        /// </summary>
        [Parameter]
        public string? Instructions { get; set; }

        /// <summary>
        /// Gets or sets the model name. Falls back to the configured <see cref="AIChatServiceOptions.Model"/>.
        /// </summary>
        [Parameter]
        public string? ModelId { get; set; }

        private string? text;

        /// <summary>
        /// Gets or sets the button text. Default is <c>Smart paste</c>.
        /// </summary>
        [Parameter]
        public string Text { get => text ?? Localize(nameof(RadzenStrings.SmartPasteButton_Text)); set => text = value; }

        private string? title;

        /// <summary>
        /// Gets or sets the button title (tooltip).
        /// </summary>
        [Parameter]
        public string Title { get => title ?? Localize(nameof(RadzenStrings.SmartPasteButton_Title)); set => title = value; }

        /// <summary>
        /// Gets or sets the text shown while the model is working.
        /// </summary>
        [Parameter]
        public string BusyText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the icon. Default is <c>content_paste_go</c>.
        /// </summary>
        [Parameter]
        public string Icon { get; set; } = "content_paste_go";

        /// <summary>
        /// Gets or sets the icon color.
        /// </summary>
        [Parameter]
        public string? IconColor { get; set; }

        /// <summary>
        /// Gets or sets the button style.
        /// </summary>
        [Parameter]
        public ButtonStyle ButtonStyle { get; set; } = ButtonStyle.Primary;

        /// <summary>
        /// Gets or sets the button variant.
        /// </summary>
        [Parameter]
        public Variant Variant { get; set; } = Variant.Filled;

        /// <summary>
        /// Gets or sets the button shade.
        /// </summary>
        [Parameter]
        public Shade Shade { get; set; } = Shade.Default;

        /// <summary>
        /// Gets or sets the button size.
        /// </summary>
        [Parameter]
        public ButtonSize Size { get; set; } = ButtonSize.Medium;

        /// <summary>
        /// Gets or sets whether the button is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Event callback that is invoked after the model is filled. Receives <see cref="Data"/>.
        /// </summary>
        [Parameter]
        public EventCallback<TModel> Pasted { get; set; }

        /// <summary>
        /// Event callback that is invoked when the clipboard is empty or cannot be read.
        /// </summary>
        [Parameter]
        public EventCallback EmptyClipboard { get; set; }

        /// <summary>
        /// Event callback that is invoked when the model request fails.
        /// </summary>
        [Parameter]
        public EventCallback<Exception> Error { get; set; }

        /// <summary>
        /// Gets whether a paste is in progress.
        /// </summary>
        public bool IsBusy { get; private set; }

        /// <inheritdoc />
        protected override string GetComponentCssClass() => "rz-smart-paste-button";

        private async Task OnClick(MouseEventArgs args)
        {
            if (IsBusy || Disabled)
            {
                return;
            }

            string? clipboard = null;

            if (JSRuntime != null)
            {
                try
                {
                    clipboard = await JSRuntime.InvokeAsync<string?>("Radzen.readClipboardText");
                }
                catch (JSException)
                {
                }
            }

            if (string.IsNullOrWhiteSpace(clipboard))
            {
                await EmptyClipboard.InvokeAsync();
                return;
            }

            await Paste(clipboard);
        }

        /// <summary>
        /// Fills <see cref="Data"/> from the specified text instead of the clipboard.
        /// </summary>
        /// <param name="text">The text to extract the field values from.</param>
        public async Task Paste(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(Data);

            var properties = GetProperties();

            if (properties.Count == 0)
            {
                return;
            }

            IsBusy = true;
            await InvokeAsync(StateHasChanged);

            try
            {
                var client = ServiceProvider.GetService<IAIChatService>()?.GetChatClient() ?? ServiceProvider.GetService<IChatClient>() ?? throw new InvalidOperationException("Register an IChatClient or call AddAIChatService to use RadzenSmartPasteButton.");

                var options = new ChatOptions
                {
                    ModelId = ModelId,
                    Temperature = 0,
                    ResponseFormat = ChatResponseFormat.Json,
                    Instructions = BuildInstructions(properties)
                };

                var response = await client.GetResponseAsync([new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, text)], options);

                Apply(response.Text, properties);

                await Pasted.InvokeAsync(Data);
            }
            catch (Exception exception) when (Error.HasDelegate)
            {
                await Error.InvokeAsync(exception);
            }
            finally
            {
                IsBusy = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private List<PropertyInfo> GetProperties()
        {
            var fields = Fields?.ToHashSet(StringComparer.OrdinalIgnoreCase);

            return typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanWrite && property.GetIndexParameters().Length == 0 && IsSupported(property.PropertyType) && (fields == null || fields.Contains(property.Name)))
                .ToList();
        }

        private string BuildInstructions(List<PropertyInfo> properties)
        {
            var builder = new StringBuilder();

            builder.AppendLine("You extract structured data from text the user pasted. Respond with a JSON object only.");
            builder.AppendLine("Use exactly these keys. Set a key to null when the text has no value for it. Write dates as ISO 8601, numbers without units or thousands separators, and for choice fields use one of the listed values.");

            if (!string.IsNullOrWhiteSpace(Instructions))
            {
                builder.AppendLine(Instructions);
            }

            builder.AppendLine("Keys:");

            foreach (var property in properties)
            {
                var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                var kind = type.IsEnum ? $"one of: {string.Join(", ", Enum.GetNames(type))}" : type == typeof(string) ? "text" : type == typeof(bool) ? "true or false" : type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly) ? "date" : type == typeof(TimeOnly) ? "time" : "number";
                var description = property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? property.GetCustomAttribute<DisplayAttribute>()?.GetName();

                builder.Append("- ").Append(property.Name).Append(" (").Append(kind).Append(')');

                if (!string.IsNullOrEmpty(description))
                {
                    builder.Append(": ").Append(description);
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private void Apply(string json, List<PropertyInfo> properties)
        {
            var start = json.IndexOf('{', StringComparison.Ordinal);
            var end = json.LastIndexOf('}');

            if (start < 0 || end <= start)
            {
                throw new InvalidOperationException("The model did not return a JSON object.");
            }

            using var document = JsonDocument.Parse(json.Substring(start, end - start + 1));

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("The model did not return a JSON object.");
            }

            var values = document.RootElement.EnumerateObject().ToDictionary(property => property.Name, property => property.Value, StringComparer.OrdinalIgnoreCase);

            foreach (var property in properties)
            {
                if (!values.TryGetValue(property.Name, out var element) || element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                {
                    continue;
                }

                if (TryConvert(element, property.PropertyType, out var value))
                {
                    property.SetValue(Data, value);
                    EditContext?.NotifyFieldChanged(new FieldIdentifier(Data, property.Name));
                }
            }
        }

        private static bool IsSupported(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            return type == typeof(string) || type == typeof(bool) || type.IsEnum || type == typeof(Guid)
                || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly)
                || type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte)
                || type == typeof(double) || type == typeof(float) || type == typeof(decimal);
        }

        private static bool TryConvert(JsonElement element, Type type, out object? value)
        {
            value = null;
            type = Nullable.GetUnderlyingType(type) ?? type;

            var text = element.ValueKind == JsonValueKind.String ? element.GetString() : element.ValueKind is JsonValueKind.Object or JsonValueKind.Array ? element.GetRawText() : element.ToString();

            if (type == typeof(string))
            {
                value = text;
                return true;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            text = text.Trim();

            if (type == typeof(bool))
            {
                if (bool.TryParse(text, out var boolean))
                {
                    value = boolean;
                    return true;
                }

                value = text.Equals("yes", StringComparison.OrdinalIgnoreCase) || text == "1";
                return true;
            }

            if (type.IsEnum)
            {
                if (Enum.TryParse(type, text.Replace(" ", string.Empty, StringComparison.Ordinal), true, out var member))
                {
                    value = member;
                    return true;
                }

                return false;
            }

            if (type == typeof(Guid))
            {
                if (Guid.TryParse(text, out var guid))
                {
                    value = guid;
                    return true;
                }

                return false;
            }

            if (type == typeof(DateTime))
            {
                if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateTime))
                {
                    value = dateTime;
                    return true;
                }

                return false;
            }

            if (type == typeof(DateTimeOffset))
            {
                if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateTimeOffset))
                {
                    value = dateTimeOffset;
                    return true;
                }

                return false;
            }

            if (type == typeof(DateOnly))
            {
                if (DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
                {
                    value = dateOnly;
                    return true;
                }

                if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateTime))
                {
                    value = DateOnly.FromDateTime(dateTime);
                    return true;
                }

                return false;
            }

            if (type == typeof(TimeOnly))
            {
                if (TimeOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeOnly))
                {
                    value = timeOnly;
                    return true;
                }

                return false;
            }

            var number = text.Replace(",", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal);

            if (!decimal.TryParse(number, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                return false;
            }

            try
            {
                value = Convert.ChangeType(parsed, type, CultureInfo.InvariantCulture);
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }
    }
}

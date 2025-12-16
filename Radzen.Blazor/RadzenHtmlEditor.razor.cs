using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A rich text HTML editor component with WYSIWYG editing, formatting toolbar, image upload, and custom tool support.
    /// RadzenHtmlEditor provides a full-featured editor for creating and editing formatted content with a Microsoft Word-like interface.
    /// Allows users to create rich formatted content without knowing HTML.
    /// Features WYSIWYG (what-you-see-is-what-you-get) visual editing interface, formatting tools (bold, italic, underline, font selection, colors, alignment, lists, links, images),
    /// built-in image upload with configurable upload URL, custom toolbar buttons via RadzenHtmlEditorCustomTool, toggle between visual editing and HTML source code view,
    /// paste filtering to remove unwanted HTML when pasting from other sources, and programmatic execution of formatting commands via ExecuteCommandAsync().
    /// The Value property contains HTML markup. Use UploadUrl to configure where images are uploaded. Add custom tools for domain-specific functionality like inserting templates or special content.
    /// </summary>
    /// <example>
    /// Basic HTML editor:
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@htmlContent Style="height: 400px;" /&gt;
    /// @code {
    ///     string htmlContent = "&lt;p&gt;Enter content here...&lt;/p&gt;";
    /// }
    /// </code>
    /// Editor with image upload:
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@content UploadUrl="api/upload/image" UploadHeaders=@uploadHeaders&gt;
    ///     &lt;RadzenHtmlEditorBold /&gt;
    ///     &lt;RadzenHtmlEditorItalic /&gt;
    ///     &lt;RadzenHtmlEditorUnderline /&gt;
    ///     &lt;RadzenHtmlEditorSeparator /&gt;
    ///     &lt;RadzenHtmlEditorImage /&gt;
    /// &lt;/RadzenHtmlEditor&gt;
    /// </code>
    /// Editor with custom tool:
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@html Execute=@OnExecute&gt;
    ///     &lt;RadzenHtmlEditorCustomTool CommandName="InsertDate" Icon="calendar_today" Title="Insert Current Date" /&gt;
    /// &lt;/RadzenHtmlEditor&gt;
    /// @code {
    ///     async Task OnExecute(HtmlEditorExecuteEventArgs args)
    ///     {
    ///         if (args.CommandName == "InsertDate")
    ///         {
    ///             await args.Editor.ExecuteCommandAsync(HtmlEditorCommands.InsertHtml, DateTime.Today.ToLongDateString());
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditor : FormComponent<string>
    {
        /// <summary>
        /// Gets or sets whether to display the formatting toolbar above the editor.
        /// When false, hides the toolbar but editing is still possible. Useful for read-only or simplified views.
        /// </summary>
        /// <value><c>true</c> to show the toolbar; <c>false</c> to hide it. Default is <c>true</c>.</value>
        [Parameter]
        public bool ShowToolbar { get; set; } = true;

        /// <summary>
        /// Gets or sets the editor mode determining whether users see the visual editor or HTML source code.
        /// Design mode shows WYSIWYG editing, Source mode shows raw HTML for advanced users.
        /// </summary>
        /// <value>The editor mode. Default is <see cref="HtmlEditorMode.Design"/>.</value>
        [Parameter]
        public HtmlEditorMode Mode { get; set; } = HtmlEditorMode.Design;

        private HtmlEditorMode mode;

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Specifies custom headers that will be submit during uploads.
        /// </summary>
        [Parameter]
        public IDictionary<string, string> UploadHeaders { get; set; }

        /// <summary>
        /// Gets or sets the input.
        /// </summary>
        /// <value>The input.</value>
        [Parameter]
        public EventCallback<string> Input { get; set; }

        /// <summary>
        /// A callback that will be invoked when the user pastes content in the editor. Commonly used to filter unwanted HTML.
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;RadzenHtmlEditor @bind-Value=@html Paste=@OnPaste /&gt;
        /// @code {
        ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!";
        ///   void OnPaste(HtmlEditorPasteEventArgs args)
        ///   {
        ///     // Set args.Html to filter unwanted tags.
        ///     args.Html = args.Html.Replace("&lt;br&gt;", "");
        ///   }
        /// </code>
        /// </example>
        [Parameter]
        public EventCallback<HtmlEditorPasteEventArgs> Paste { get; set; }

        /// <summary>
        /// A callback that will be invoked when there is an error during upload.
        /// </summary>
        [Parameter]
        public EventCallback<UploadErrorEventArgs> UploadError { get; set; }

        /// <summary>
        /// Called on upload error.
        /// </summary>
        /// <param name="error">The error.</param>
        [JSInvokable("OnError")]
        public async Task OnError(string error)
        {
            await UploadError.InvokeAsync(new UploadErrorEventArgs { Message = error });
        }

        /// <summary>
        /// A callback that will be invoked when the user executes a command of the editor (e.g. by clicking one of the tools).
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;RadzenHtmlEditor Execute=@OnExecute&gt;
        ///   &lt;RadzenHtmlEditorCustomTool CommandName="InsertToday" Icon="today" Title="Insert today" /&gt;
        /// &lt;/RadzenHtmlEditor&gt;
        /// @code {
        ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!";
        ///   async Task OnExecute(HtmlEditorExecuteEventArgs args)
        ///   {
        ///     if (args.CommandName == "InsertToday")
        ///     {
        ///       await args.Editor.ExecuteCommandAsync(HtmlEditorCommands.InsertHtml, DateTime.Today.ToLongDateString());
        ///     }
        ///  }
        /// </code>
        /// </example>
        [Parameter]
        public EventCallback<HtmlEditorExecuteEventArgs> Execute { get; set; }

        /// <summary>
        /// Specifies the URL to which RadzenHtmlEditor will submit files.
        /// </summary>
        [Parameter]
        public string UploadUrl { get; set; }

        ElementReference ContentEditable { get; set; }
        RadzenTextArea TextArea { get; set; }

        /// <summary>
        /// Focuses the editor.
        /// </summary>
        public override ValueTask FocusAsync()
        {

            if (mode == HtmlEditorMode.Design)
            {
                return ContentEditable.FocusAsync();
            }
            else
            {
                return TextArea.Element.FocusAsync();
            }
        }

        /// <summary>
        /// Represents the current state of the toolbar commands and other functionalities within the RadzenHtmlEditor component.
        /// Updated dynamically based on user actions or programmatically invoked commands.
        /// </summary>
        public RadzenHtmlEditorCommandState State { get; set; } = new();

        async Task OnFocus()
        {
            await UpdateCommandState();
        }

        private readonly IDictionary<string, Func<Task>> shortcuts = new Dictionary<string, Func<Task>>();

        /// <summary>
        /// Registers a shortcut for the specified action.
        /// </summary>
        /// <param name="key">The shortcut. Can be combination of keys such as <c>CTRL+B</c>.</param>
        /// <param name="action">The action to execute.</param>
        public void RegisterShortcut(string key, Func<Task> action)
        {
            shortcuts[key] = action;
        }

        /// <summary>
        /// Unregisters the specified shortcut.
        /// </summary>
        /// <param name="key"></param>
        public void UnregisterShortcut(string key)
        {
            shortcuts.Remove(key);
        }

        /// <summary>
        /// Invoked by interop when the RadzenHtmlEditor selection changes.
        /// </summary>
        [JSInvokable]
        public async Task OnSelectionChange()
        {
            await UpdateCommandState();
        }

        /// <summary>
        /// Invoked by interop during uploads. Provides the custom headers.
        /// </summary>
        [JSInvokable("GetHeaders")]
        public IDictionary<string, string> GetHeaders()
        {
            return UploadHeaders ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Executes the requested command with the provided value. Check <see cref="HtmlEditorCommands" /> for the list of supported commands.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public async Task ExecuteCommandAsync(string name, string value = null)
        {
            State = await JSRuntime.InvokeAsync<RadzenHtmlEditorCommandState>("Radzen.execCommand", ContentEditable, name, value);

            await OnExecuteAsync(name);

            if (Html != State.Html)
            {
                Html = State.Html;

                htmlChanged = true;

                await OnChange();
            }
        }

        /// <summary>
        /// Executes the action associated with the specified shortcut. Used internally by RadzenHtmlEditor.
        /// </summary>
        /// <param name="shortcut"></param>
        /// <returns></returns>
        [JSInvokable("ExecuteShortcutAsync")]
        public async Task ExecuteShortcutAsync(string shortcut)
        {
            if (shortcuts.TryGetValue(shortcut, out var action))
            {
                await action();
            }
        }

        /// <summary>
        /// Handles changes to the editor's source content.
        /// </summary>
        /// <param name="html">The updated HTML content.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task SourceChanged(string html)
        {
            if (Html != html)
            {
                Html = html;
                htmlChanged = true;
                sourceChanged = true;
            }
            await OnChange();
            StateHasChanged();
        }

        async Task OnChange()
        {
            if (htmlChanged)
            {
                htmlChanged = false;

                _value = Html;

                await ValueChanged.InvokeAsync(Html);

                if (FieldIdentifier.FieldName != null)
                {
                    EditContext?.NotifyFieldChanged(FieldIdentifier);
                }

                await Change.InvokeAsync(Html);
            }
        }

        internal async Task OnExecuteAsync(string name)
        {
            await Execute.InvokeAsync(new HtmlEditorExecuteEventArgs(this) { CommandName = name });

            StateHasChanged();
        }

        /// <summary>
        /// Saves the current selection. RadzenHtmlEditor will lose its selection when it loses focus. Use this method to persist the current selection.
        /// </summary>
        public async Task SaveSelectionAsync()
        {
            await JSRuntime.InvokeVoidAsync("Radzen.saveSelection", ContentEditable);
        }

        /// <summary>
        /// Restores the last saved selection.
        /// </summary>
        public async Task RestoreSelectionAsync()
        {
            await JSRuntime.InvokeVoidAsync("Radzen.restoreSelection", ContentEditable);
        }

        async Task UpdateCommandState()
        {
            State = await JSRuntime.InvokeAsync<RadzenHtmlEditorCommandState>("Radzen.queryCommands", ContentEditable);

            StateHasChanged();
        }

        async Task OnBlur()
        {
            await OnChange();
        }

        bool htmlChanged = false;
        bool sourceChanged = false;

        bool visibleChanged = false;
        bool firstRender = true;

        /// <summary>
        /// Retrieves the specified attributes of a selection within the content editable area.
        /// </summary>
        /// <typeparam name="T">The type of attributes to retrieve.</typeparam>
        /// <param name="selector">The CSS selector used to target the element.</param>
        /// <param name="attributes">An array of attribute names to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation, returning the attributes as an object of type T.</returns>
        public ValueTask<T> GetSelectionAttributes<T>(string selector, string[] attributes)
        {
            return JSRuntime.InvokeAsync<T>("Radzen.selectionAttributes", selector, attributes, ContentEditable);
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            this.firstRender = firstRender;

            if (firstRender || visibleChanged)
            {
                if (Visible)
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.createEditor", ContentEditable, UploadUrl, Paste.HasDelegate, Reference, shortcuts.Keys);
                }
            }

            var requiresUpdate = false;

            if (valueChanged || visibleChanged)
            {
                valueChanged = false;
                visibleChanged = false;

                Html = Value;

                if (Visible)
                {
                    requiresUpdate = true;
                }
            }
            else if (sourceChanged)
            {
                sourceChanged = false;

                if (Visible)
                {
                    requiresUpdate = true;
                }
            }

            if (requiresUpdate)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.innerHTML", ContentEditable, Html);
            }
        }

        internal void SetMode(HtmlEditorMode value)
        {
            mode = value;

            StateHasChanged();
        }

        /// <summary>
        /// Returns the current mode of the editor.
        /// </summary>
        public HtmlEditorMode GetMode()
        {
            return mode;
        }

        string Html { get; set; }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            Html = Value;
            mode = Mode;

            base.OnInitialized();
        }

        /// <summary>
        /// Invoked via interop when the value of RadzenHtmlEditor changes.
        /// </summary>
        /// <param name="html">The HTML.</param>
        [JSInvokable]
        public void OnChange(string html)
        {
            if (Html != html)
            {
                Html = html;
                htmlChanged = true;
            }
            Input.InvokeAsync(html);
        }

        /// <summary>
        /// Invoked via interop when the user pastes content in RadzenHtmlEditor. Invokes <see cref="Paste" />.
        /// </summary>
        /// <param name="html">The HTML.</param>
        [JSInvokable]
        public async Task<string> OnPaste(string html)
        {
            var args = new HtmlEditorPasteEventArgs { Html = html };

            await Paste.InvokeAsync(args);

            return args.Html;
        }

        bool valueChanged = false;

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(Value), Value))
            {
                valueChanged = Html != parameters.GetValueOrDefault<string>(nameof(Value));
            }

            if (parameters.DidParameterChange(nameof(Mode), Mode))
            {
                mode = Mode;
            }

            visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);

            await base.SetParametersAsync(parameters);

            if (visibleChanged && !firstRender && !Visible)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.destroyEditor", ContentEditable);
            }
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-html-editor").ToString();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            if (Visible && IsJSRuntimeAvailable)
            {
                JSRuntime.InvokeVoid("Radzen.destroyEditor", ContentEditable);
            }
        }

        /// <summary>
        /// Gets or sets the callback which when a file is uploaded.
        /// </summary>
        /// <value>The complete callback.</value>
        [Parameter]
        public EventCallback<UploadCompleteEventArgs> UploadComplete { get; set; }


        internal async Task RaiseUploadComplete(UploadCompleteEventArgs args)
        {
            await UploadComplete.InvokeAsync(args);
        }

        /// <summary>
        /// Invoked by interop when the upload is complete.
        /// </summary>
        [JSInvokable("OnUploadComplete")]
        public async Task OnUploadComplete(string response)
        {
            System.Text.Json.JsonDocument doc = null;

            if (!string.IsNullOrEmpty(response))
            {
                try
                {
                    doc = System.Text.Json.JsonDocument.Parse(response);
                }
                catch (System.Text.Json.JsonException)
                {
                    //
                }
            }

            await UploadComplete.InvokeAsync(new UploadCompleteEventArgs() { RawResponse = response, JsonResponse = doc });
        }
    }
}

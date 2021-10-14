using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenHtmlEditor.
    /// Implements the <see cref="Radzen.FormComponent{System.String}" />
    /// </summary>
    /// <seealso cref="Radzen.FormComponent{System.String}" />
    public partial class RadzenHtmlEditor : FormComponent<string>
    {
        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the upload headers.
        /// </summary>
        /// <value>The upload headers.</value>
        [Parameter]
        public IDictionary<string, string> UploadHeaders { get; set; }

        /// <summary>
        /// Gets or sets the paste.
        /// </summary>
        /// <value>The paste.</value>
        [Parameter]
        public EventCallback<HtmlEditorPasteEventArgs> Paste { get; set; }

        /// <summary>
        /// Gets or sets the execute.
        /// </summary>
        /// <value>The execute.</value>
        [Parameter]
        public EventCallback<HtmlEditorExecuteEventArgs> Execute { get; set; }

        /// <summary>
        /// Gets or sets the upload URL.
        /// </summary>
        /// <value>The upload URL.</value>
        [Parameter]
        public string UploadUrl { get; set; }

        /// <summary>
        /// Gets or sets the content editable.
        /// </summary>
        /// <value>The content editable.</value>
        ElementReference ContentEditable { get; set; }

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>The state.</value>
        internal RadzenHtmlEditorCommandState State { get; set; } = new RadzenHtmlEditorCommandState();

        /// <summary>
        /// Called when [focus].
        /// </summary>
        async Task OnFocus()
        {
            await UpdateCommandState();
        }

        /// <summary>
        /// Called when [selection change].
        /// </summary>
        [JSInvokable]
        public async Task OnSelectionChange()
        {
            await UpdateCommandState();
        }

        /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <returns>IDictionary&lt;System.String, System.String&gt;.</returns>
        [JSInvokable("GetHeaders")]
        public IDictionary<string, string> GetHeaders()
        {
            return UploadHeaders ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Execute command as an asynchronous operation.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task ExecuteCommandAsync(string name, string value = null)
        {
            State = await JSRuntime.InvokeAsync<RadzenHtmlEditorCommandState>("Radzen.execCommand", ContentEditable, name, value);
            await OnExecuteAsync(name);
            Html = State.Html;
            await OnChange();
        }

        /// <summary>
        /// Called when [change].
        /// </summary>
        async Task OnChange()
        {
            await Change.InvokeAsync(Html);
            await ValueChanged.InvokeAsync(Html);
        }

        /// <summary>
        /// On execute as an asynchronous operation.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        internal async Task OnExecuteAsync(string name)
        {
            await Execute.InvokeAsync(new HtmlEditorExecuteEventArgs(this) { CommandName = name });

            StateHasChanged();
        }

        /// <summary>
        /// Save selection as an asynchronous operation.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task SaveSelectionAsync()
        {
            await JSRuntime.InvokeVoidAsync("Radzen.saveSelection", ContentEditable);
        }

        /// <summary>
        /// Restore selection as an asynchronous operation.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task RestoreSelectionAsync()
        {
            await JSRuntime.InvokeVoidAsync("Radzen.restoreSelection", ContentEditable);
        }

        /// <summary>
        /// Updates the state of the command.
        /// </summary>
        async Task UpdateCommandState()
        {
            State = await JSRuntime.InvokeAsync<RadzenHtmlEditorCommandState>("Radzen.queryCommands", ContentEditable);

            StateHasChanged();
        }

        /// <summary>
        /// Called when [blur].
        /// </summary>
        async Task OnBlur()
        {
            await OnChange();
        }

        /// <summary>
        /// The visible changed
        /// </summary>
        bool visibleChanged = false;
        /// <summary>
        /// The first render
        /// </summary>
        bool firstRender = true;

        /// <summary>
        /// On after render as an asynchronous operation.
        /// </summary>
        /// <param name="firstRender">if set to <c>true</c> [first render].</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            this.firstRender = firstRender;

            if (firstRender || visibleChanged)
            {
                visibleChanged = false;

                if (Visible)
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.createEditor", ContentEditable, UploadUrl, Paste.HasDelegate, Reference);
                }
            }

            if (valueChanged)
            {
                valueChanged = false;

                Html = Value;

                if (Visible)
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.innerHTML", ContentEditable, Value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the HTML.
        /// </summary>
        /// <value>The HTML.</value>
        string Html { get; set; }

        /// <summary>
        /// Called when [initialized].
        /// </summary>
        protected override void OnInitialized()
        {
            Html = Value;
        }

        /// <summary>
        /// Called when [change].
        /// </summary>
        /// <param name="html">The HTML.</param>
        [JSInvokable]
        public void OnChange(string html)
        {
            Html = html;
        }

        /// <summary>
        /// Called when [paste].
        /// </summary>
        /// <param name="html">The HTML.</param>
        /// <returns>System.String.</returns>
        [JSInvokable]
        public async Task<string> OnPaste(string html)
        {
            var args = new HtmlEditorPasteEventArgs { Html = html };

            await Paste.InvokeAsync(args);

            return args.Html;
        }

        /// <summary>
        /// The value changed
        /// </summary>
        bool valueChanged = false;

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(Value), Value))
            {
                valueChanged = Html != parameters.GetValueOrDefault<string>(nameof(Value));
            }

            visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);

            await base.SetParametersAsync(parameters);

            if (visibleChanged && !firstRender && !Visible)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.destroyEditor", ContentEditable);
            }
        }

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return "rz-html-editor";
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            if (Visible && IsJSRuntimeAvailable)
            {
                JSRuntime.InvokeVoidAsync("Radzen.destroyEditor", ContentEditable);
            }
        }
    }
}
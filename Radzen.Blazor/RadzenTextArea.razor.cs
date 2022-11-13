using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenTextArea component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenTextArea Cols="30" Rows="3" @bind-Value=@value Change=@(args => Console.WriteLine($"Value: {args}")) /&gt;
    /// </code>
    /// </example>
    public partial class RadzenTextArea : FormComponent<string>
    {
        /// <summary>
        /// Gets or sets the maximum length.
        /// </summary>
        /// <value>The maximum length.</value>
        [Parameter]
        public long? MaxLength { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is read only.
        /// </summary>
        /// <value><c>true</c> if is read only; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets the number of rows.
        /// </summary>
        /// <value>The number of rows.</value>
        [Parameter]
        public int Rows { get; set; } = 2;

        /// <summary>
        /// Gets or sets the number of cols.
        /// </summary>
        /// <value>The number of cols.</value>
        [Parameter]
        public int Cols { get; set; } = 20;

        /// <summary>
        /// Enables speech to text functionality.
        /// <para>This is only supported on select browsers. See https://caniuse.com/?search=SpeechRecognition</para>
        /// </summary>
        [Parameter]
        public bool EnableSpeechToText { get; set; }

        /// <summary>
        /// Handles the <see cref="E:Change" /> event.
        /// </summary>
        /// <param name="args">The <see cref="ChangeEventArgs"/> instance containing the event data.</param>
        protected async System.Threading.Tasks.Task OnChange(ChangeEventArgs args)
        {
            Value = $"{args.Value}";

            await ValueChanged.InvokeAsync(Value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-textarea").ToString();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            _componentRef?.Dispose();

            base.Dispose();
        }

        #region Speech to Text

        private bool _isRecording;

        private string MicIconCss => _isRecording ? "rz-mic rz-mic-on" : "rz-mic";

        private DotNetObjectReference<RadzenTextArea> _componentRef;

        private async System.Threading.Tasks.Task OnSpeechToTextClicked()
        {
            if (_componentRef == null)
            {
                _componentRef = DotNetObjectReference.Create(this);
            }

            await JSRuntime.InvokeVoidAsync("Radzen.toggleDictation", @Element, _componentRef);

            _isRecording = !_isRecording;
        }

        /// <summary>
        /// Provides interface for javascript to stop speech to text recording on this component if another component starts recording.
        /// </summary>
        [JSInvokable]
        public void StopRecording()
        {
            _isRecording = false;

            StateHasChanged();
        }

        #endregion
    }
}

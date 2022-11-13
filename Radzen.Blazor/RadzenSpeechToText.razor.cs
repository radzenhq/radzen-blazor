using Microsoft.JSInterop;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenSpeechToText component. Enables speech to text functionality.
    /// <para>This is only supported on select browsers. See https://caniuse.com/?search=SpeechRecognition</para>
    /// <example>
    /// <code>
    /// &lt;RadzenSpeechToText OnResult=@(args => Console.WriteLine($"Value: {args}")) /&gt;
    /// </code>
    /// </example>
    /// </summary>
    public partial class RadzenSpeechToText: RadzenComponent
    {
        /// <summary>
        /// Callback which provides results from the speech recognition API.
        /// </summary>
        [Parameter]
        public EventCallback<string> OnResult { get; set; }

        private bool _isRecording;

        private DotNetObjectReference<RadzenSpeechToText> _componentRef;

        private RadzenButton _micButton;


        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return _isRecording ? "rz-mic rz-mic-on" : "rz-mic";
        }

        private async Task OnSpeechToTextClicked()
        {
            if (_componentRef == null)
            {
                _componentRef = DotNetObjectReference.Create(this);
            }

            await JSRuntime.InvokeVoidAsync("Radzen.toggleDictation", _micButton.Element, _componentRef);

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

        /// <summary>
        /// Provides interface for javascript to pass speech results back to this component.
        /// </summary>
        /// <param name="result"></param>
        [JSInvokable]
        public void OnResultFromJs(string result)
        {
            OnResult.InvokeAsync(result);
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            _componentRef?.Dispose();

            base.Dispose();
        }
    }
}

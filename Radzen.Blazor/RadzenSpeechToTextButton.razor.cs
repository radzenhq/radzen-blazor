using Microsoft.JSInterop;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenSpeechToTextButton component. Enables speech to text functionality.
    /// <para>This is only supported on select browsers. See https://caniuse.com/?search=SpeechRecognition</para>
    /// <example>
    /// <code>
    /// &lt;RadzenSpeechToTextButton Change=@(args => Console.WriteLine($"Value: {args}")) /&gt;
    /// </code>
    /// </example>
    /// </summary>
    public partial class RadzenSpeechToTextButton : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the button style.
        /// </summary>
        /// <value>The button style.</value>
        [Parameter]
        public ButtonStyle ButtonStyle { get; set; } = ButtonStyle.Light;

        /// <summary>
        /// Gets or sets the icon.
        /// </summary>
        /// <value>The icon.</value>
        [Parameter]
        public string Icon { get; set; } = "mic";

        /// <summary>
        /// Callback which provides results from the speech recognition API.
        /// </summary>
        [Parameter]
        public EventCallback<string> Change { get; set; }

        private bool recording;

        /// <inheritdoc />
        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return recording ? "rz-speech-to-text-button rz-speech-to-text-button-recording" : "rz-speech-to-text-button";
        }

        private async Task OnSpeechToTextClicked()
        {
            recording = !recording;

            await JSRuntime.InvokeVoidAsync("Radzen.toggleDictation", Reference);
        }

        /// <summary>
        /// Provides interface for javascript to stop speech to text recording on this component if another component starts recording.
        /// </summary>
        [JSInvokable]
        public void StopRecording()
        {
            recording = false;

            StateHasChanged();
        }

        /// <summary>
        /// Provides interface for javascript to pass speech results back to this component.
        /// </summary>
        /// <param name="result"></param>
        [JSInvokable]
        public void OnResult(string result)
        {
            Change.InvokeAsync(result);
        }
    }
}

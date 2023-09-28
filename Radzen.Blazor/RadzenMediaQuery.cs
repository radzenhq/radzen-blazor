using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenMediaQuery fires its <see cref="Change" /> event when the media query specified via <see cref="Query" /> matches or not.
    /// </summary>
    /// <example>
    /// &lt; RadzenMediaQuery Query="(max-width: 768px)" Change=@OnChange /&gt;
    /// @code {
    ///  void OnChange(bool matches)
    ///  {
    ///     // matches is true if the media query applies; otherwise false.
    ///  }
    ///}
    /// </example>
    public class RadzenMediaQuery : ComponentBase, IDisposable
    {

        [Inject]
        IJSRuntime JSRuntime { get; set; }
        

        /// <summary>
        /// The CSS media query this component will listen for.
        /// </summary>
        [Parameter]
        public string Query { get; set; }

        /// <summary>
        /// A callback that will be invoked when the status of the media query changes - to either match or not.
        /// </summary>
        [Parameter]
        public EventCallback<bool> Change { get; set; }

        /// <summary>
        /// Invoked by interop when media query changes.
        /// </summary>
        [JSInvokable]
        public async Task OnChange(bool matches)
        {
            await Change.InvokeAsync(matches);
        }

        bool initialized;
        private DotNetObjectReference<RadzenMediaQuery> reference;

        private DotNetObjectReference<RadzenMediaQuery> Reference
        {
            get
            {
                if (reference == null)
                {
                    reference = DotNetObjectReference.Create(this);
                }

                return reference;
            }
        }

        /// <summary>
        /// Called by the Blazor runtime. Initializes the media query on the client-side.
        /// </summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                initialized = true;
                try
                {
                    var matches = await JSRuntime.InvokeAsync<bool>("Radzen.mediaQuery", Query, Reference);

                    await Change.InvokeAsync(matches);
                }
                catch
                { 
                    //
                }
            }
        }

        /// <summary>
        /// Detaches client-side event listeners.
        /// </summary>
        public void Dispose()
        {
            if (initialized)
            {
                try
                {
                    JSRuntime.InvokeVoidAsync("Radzen.mediaQuery", Reference);
                }
                catch
                {
                    //
                }
            }

            reference?.Dispose();
            reference = null;
        }
    }
}
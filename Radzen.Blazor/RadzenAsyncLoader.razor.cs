using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A generic component for asynchronously loading data and rendering templates based on the loading state.
    /// </summary>
    public partial class RadzenAsyncLoader<TData> : ComponentBase
    {
        /// <summary>
        /// The template to render when data is loaded.
        /// </summary>
        [Parameter]
        public RenderFragment<TData> Template { get; set; }

        /// <summary>
        /// The template to render while data is loading.
        /// </summary>
        [Parameter]
        public RenderFragment LoadingTemplate { get; set; }

        /// <summary>
        /// The asynchronous task that fetches the data.
        /// </summary>
        [Parameter]
        public Task<TData> DataTask { get; set; }

        /// <summary>
        /// A callback action invoked when an error occurs during data loading.
        /// </summary>
        [Parameter]
        public Action<Exception> OnError { get; set; }

        /// <summary>
        /// Indicates whether the component is in a loading state.
        /// </summary>
        private bool IsLoading { get; set; } = true;

        /// <summary>
        /// The loaded data result.
        /// </summary>
        private TData Result { get; set; }

        /// <summary>
        /// Initializes the component and asynchronously loads data.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected override async Task OnInitializedAsync()
        {
            try
            {
                Result = await DataTask;
                IsLoading = false;
            }
            catch (Exception ex)
            {
                IsLoading = false;
                if (OnError != null)
                {
                    OnError(ex);
                }
            }
            finally
            {
                // Ensure UI updates after loading completes or encounters an error.
                StateHasChanged();
            }
        }
    }
}

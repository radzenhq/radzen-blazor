using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Component for asynchronously loading data with optional loading and data templates.
    /// </summary>
    /// <typeparam name="TData">The type of the data to be loaded.</typeparam>
    public partial class RadzenAsyncLoader<TData> : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the template for rendering the loaded data.
        /// </summary>
        [Parameter]
        public RenderFragment<TData> Template { get; set; }

        /// <summary>
        /// Gets or sets the template for rendering while data is loading.
        /// </summary>
        [Parameter]
        public RenderFragment LoadingTemplate { get; set; }

        /// <summary>
        /// Gets or sets the task that loads the data.
        /// </summary>
        [Parameter]
        public Task<TData> DataTask { get; set; }

        /// <summary>
        /// Gets or sets the action to handle errors during data loading.
        /// </summary>
        [Parameter]
        public Action<Exception> OnError { get; set; }

        private bool IsLoading { get; set; } = true;

        public TData Data { get; set; }

        /// <inheritdoc/>
        protected override async Task OnInitializedAsync()
        {
            try
            {
                Data = await DataTask;
            }
            catch (Exception ex)
            {
                if (OnError != null)
                {
                    OnError(ex);
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        /// <inheritdoc/>
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            base.BuildRenderTree(builder);

            if (IsLoading && LoadingTemplate != null)
            {
                builder.AddContent(0, LoadingTemplate);
            }
            else if (Data != null)
            {
                builder.AddContent(1, Template(Data));
            }
        }
    }
}

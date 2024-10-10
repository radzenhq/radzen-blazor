using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenCarouselItem component.
    /// </summary>
    public partial class RadzenCarouselItem : IDisposable
    {        
        /// <summary>
        /// Gets the class list.
        /// </summary>
        /// <value>The class list.</value>
        ClassList ClassList => ClassList.Create()
                                        .Add("rz-carousel-item")
                                        .Add(Attributes);
        
        /// <summary>
        /// Gets or sets the arbitrary attributes.
        /// </summary>
        /// <value>The arbitrary attributes.</value>
        [Parameter(CaptureUnmatchedValues = true)]
        public IDictionary<string, object> Attributes { get; set; }

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the tabs.
        /// </summary>
        /// <value>The tabs.</value>
        [CascadingParameter]
        public RadzenCarousel Carousel { get; set; }

        /// <inheritdoc />
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            Carousel.AddItem(this);

            itemIndex = Carousel.items.IndexOf(this);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Carousel?.RemoveItem(this);
        }

        int itemIndex;
        internal ElementReference element;
    }
}

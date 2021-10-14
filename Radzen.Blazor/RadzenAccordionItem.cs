using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenAccordionItem.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponent" />
    public partial class RadzenAccordionItem : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the icon.
        /// </summary>
        /// <value>The icon.</value>
        [Parameter]
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenAccordionItem"/> is selected.
        /// </summary>
        /// <value><c>true</c> if selected; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Selected { get; set; }

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }


        bool _visible = true;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenAccordionItem"/> is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        [Parameter]
        public override bool Visible
        {
            get
            {
                return _visible;
            }
            set
            {
                if (_visible != value)
                {
                    _visible = value;
                    Accordion.Refresh();
                }
            }
        }

        RadzenAccordion _accordion;

        /// <summary>
        /// Gets or sets the accordion.
        /// </summary>
        /// <value>The accordion.</value>
        [CascadingParameter]
        public RadzenAccordion Accordion
        {
            get
            {
                return _accordion;
            }
            set
            {
                if (_accordion != value)
                {
                    _accordion = value;
                    _accordion.AddItem(this);
                }
            }
        }

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(Selected), Selected))
            {
                Accordion?.SelectItem(this);
            }

            await base.SetParametersAsync(parameters);
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            Accordion?.RemoveItem(this);
        }
    }
}
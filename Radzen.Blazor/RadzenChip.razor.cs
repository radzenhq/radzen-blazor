using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A compact component for displaying short text labels, tags, or statuses with optional icon and remove action.
    /// </summary>
    public partial class RadzenChip : RadzenComponent
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass() => ClassList.Create("rz-chip")
                                                                     .Add($"rz-chip-{ChipStyle.ToString().ToLowerInvariant()}")
                                                                     .AddVariant(Variant)
                                                                     .AddShade(Shade)
                                                                     .Add("rz-chip-sm", Size == ChipSize.Small)
                                                                     .Add("rz-chip-xs", Size == ChipSize.ExtraSmall)
                                                                     .Add("rz-chip-selected", IsSelected)
                                                                     .AddDisabled(Disabled)
                                                                     .ToString();

        /// <summary>
        /// Gets or sets custom child content rendered inside the chip.
        /// </summary>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the text content of the chip.
        /// </summary>
        [Parameter]
        public string? Text { get; set; }

        /// <summary>
        /// Gets or sets the material icon displayed before the text.
        /// </summary>
        [Parameter]
        public string? Icon { get; set; }

        /// <summary>
        /// Gets or sets the chip semantic style.
        /// </summary>
        [Parameter]
        public BadgeStyle ChipStyle { get; set; } = BadgeStyle.Base;

        /// <summary>
        /// Gets or sets the chip design variant.
        /// </summary>
        [Parameter]
        public Variant Variant { get; set; } = Variant.Filled;

        /// <summary>
        /// Gets or sets the chip color shade.
        /// </summary>
        [Parameter]
        public Shade Shade { get; set; } = Shade.Default;

        /// <summary>
        /// Gets or sets the chip size.
        /// </summary>
        [Parameter]
        public ChipSize Size { get; set; } = ChipSize.Medium;

        /// <summary>
        /// Gets or sets a value indicating whether the chip is selected.
        /// </summary>
        [Parameter]
        public bool IsSelected { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the chip is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets the tab index of the chip.
        /// </summary>
        [Parameter]
        public int TabIndex { get; set; }

        /// <summary>
        /// Gets or sets the title used by the close button for accessibility.
        /// </summary>
        [Parameter]
        public string RemoveChipTitle { get; set; } = "Remove";

        /// <summary>
        /// Gets or sets the callback invoked when the chip is clicked.
        /// </summary>
        [Parameter]
        public EventCallback<MouseEventArgs> Click { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the remove button is clicked.
        /// </summary>
        [Parameter]
        public EventCallback<MouseEventArgs> CloseClick { get; set; }

        internal async Task OnClick(MouseEventArgs args)
        {
            if (Disabled)
            {
                return;
            }

            await Click.InvokeAsync(args);
        }

        internal async Task OnCloseClick(MouseEventArgs args)
        {
            if (Disabled || !CloseClick.HasDelegate)
            {
                return;
            }

            await CloseClick.InvokeAsync(args);
        }

        internal async Task OnKeyDown(KeyboardEventArgs args)
        {
            if (Disabled)
            {
                return;
            }

            var key = args.Code ?? args.Key;
            if (key == "Enter" || key == "Space")
            {
                await Click.InvokeAsync();
            }
            else if ((key == "Delete" || key == "Backspace") && CloseClick.HasDelegate)
            {
                await CloseClick.InvokeAsync();
            }
        }

        internal int TabIndexValue => Disabled ? -1 : TabIndex;
    }
}

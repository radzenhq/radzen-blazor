using Microsoft.AspNetCore.Components;
using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// Represents a single item inside <see cref="RadzenChipList{TValue}" />.
    /// </summary>
    public class RadzenChipItem : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the chip text.
        /// </summary>
        [Parameter]
        public string? Text { get; set; }

        /// <summary>
        /// Gets or sets the chip icon.
        /// </summary>
        [Parameter]
        public string? Icon { get; set; }

        /// <summary>
        /// Gets or sets the chip value.
        /// </summary>
        [Parameter]
        public object? Value { get; set; }

        /// <summary>
        /// Gets or sets whether the item is disabled.
        /// </summary>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets the optional style override.
        /// </summary>
        [Parameter]
        public BadgeStyle? ChipStyle { get; set; }

        /// <summary>
        /// Gets or sets the optional variant override.
        /// </summary>
        [Parameter]
        public Variant? Variant { get; set; }

        /// <summary>
        /// Gets or sets the template used for rendering item content.
        /// </summary>
        [Parameter]
        public RenderFragment<RadzenChipItem>? Template { get; set; }

        IRadzenChipList? chipList;

        /// <summary>
        /// Gets or sets the cascading parent chip list.
        /// </summary>
        [CascadingParameter]
        public IRadzenChipList? ChipList
        {
            get
            {
                return chipList;
            }
            set
            {
                if (chipList != value)
                {
                    chipList = value;
                    chipList?.AddItem(this);
                }
            }
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            ChipList?.RemoveItem(this);
            GC.SuppressFinalize(this);
        }

        internal void SetText(string? value)
        {
            Text = value;
        }

        internal void SetValue(object? value)
        {
            Value = value;
        }

        internal void SetDisabled(bool value)
        {
            Disabled = value;
        }

        /// <inheritdoc />
        public override async System.Threading.Tasks.Task SetParametersAsync(ParameterView parameters)
        {
            var shouldRefresh = parameters.DidParameterChange(nameof(Disabled), Disabled) ||
                parameters.DidParameterChange(nameof(Text), Text) ||
                parameters.DidParameterChange(nameof(Value), Value) ||
                parameters.DidParameterChange(nameof(Icon), Icon) ||
                parameters.DidParameterChange(nameof(ChipStyle), ChipStyle) ||
                parameters.DidParameterChange(nameof(Variant), Variant);

            await base.SetParametersAsync(parameters);

            if (shouldRefresh && ChipList != null)
            {
                ChipList.Refresh();
            }
        }
    }
}

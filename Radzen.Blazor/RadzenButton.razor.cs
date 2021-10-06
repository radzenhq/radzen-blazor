using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    public partial class RadzenButton : RadzenComponent
    {
        private string getButtonSize()
        {
            return Size == ButtonSize.Medium ? "md" : "sm";
        }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public string Text { get; set; } = "";

        [Parameter]
        public string Icon { get; set; }

        [Parameter]
        public string Image { get; set; }

        [Parameter]
        public ButtonStyle ButtonStyle { get; set; } = ButtonStyle.Primary;

        [Parameter]
        public ButtonType ButtonType { get; set; } = ButtonType.Button;

        [Parameter]
        public ButtonSize Size { get; set; } = ButtonSize.Medium;

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public EventCallback<MouseEventArgs> Click { get; set; }

        [Parameter]
        public bool IsBusy { get; set; }

        [Parameter]
        public string BusyText { get; set; } = "";

        public bool IsDisabled { get => Disabled || IsBusy; }

        bool clicking;
        public async Task OnClick(MouseEventArgs args)
        {
            if (clicking)
            {
                return;
            }

            try
            {
                clicking = true;

                await Click.InvokeAsync(args);
            }
            finally
            {
                clicking = false;
            }
        }

        protected override string GetComponentCssClass()
        {
            return $"rz-button rz-button-{getButtonSize()} btn-{Enum.GetName(typeof(ButtonStyle), ButtonStyle).ToLower()}{(IsDisabled ? " rz-state-disabled" : "")}{(string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(Icon) ? " rz-button-icon-only" : "")}";
        }
    }
}
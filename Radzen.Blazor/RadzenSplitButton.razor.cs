using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Radzen.Blazor
{
    public partial class RadzenSplitButton : RadzenComponentWithChildren
    {
        [Parameter]
        public string Text { get; set; } = "";

        [Parameter]
        public string Icon { get; set; }

        [Parameter]
        public string Image { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public EventCallback<RadzenSplitButtonItem> Click { get; set; }

        public async System.Threading.Tasks.Task OnClick(MouseEventArgs args)
        {
            if (!Disabled)
            {
                await Click.InvokeAsync(null);
            }
        }

        public void Close()
        {
            JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID);
        }

        private string PopupID
        {
            get
            {
                return $"popup{UniqueID}";
            }
        }

        private string getButtonCss()
        {
            return $"rz-button  rz-button-text-icon-left{(Disabled ? " rz-state-disabled" : "")}";
        }

        private string getPopupButtonCss()
        {
            return $"rz-splitbutton-menubutton rz-button rz-button-icon-only{(Disabled ? " rz-state-disabled" : "")}";
        }

        private string OpenPopupScript()
        {
            if (Disabled)
            {
                return string.Empty;
            }

            return $"Radzen.togglePopup(this.parentNode, '{PopupID}')";
        }

        protected override string GetComponentCssClass()
        {
            return Disabled ? "rz-splitbutton rz-buttonset rz-state-disabled" : "rz-splitbutton rz-buttonset";
        }
    }
}
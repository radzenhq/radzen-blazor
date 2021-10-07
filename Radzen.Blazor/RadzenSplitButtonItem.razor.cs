using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Radzen.Blazor
{
    public partial class RadzenSplitButtonItem : RadzenComponent
    {
        [Parameter]
        public string Text { get; set; } = "";

        [Parameter]
        public string Icon { get; set; }

        [Parameter]
        public string Value { get; set; }

        [CascadingParameter]
        public RadzenSplitButton SplitButton { get; set; }


        public async System.Threading.Tasks.Task OnClick(MouseEventArgs args)
        {
            if (SplitButton != null)
            {
                SplitButton.Close();
                await SplitButton.Click.InvokeAsync(this);
            }
        }
    }
}
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public abstract class RadzenHtmlEditorColorBase : RadzenHtmlEditorButtonBase
    {
        [Parameter]
        public bool ShowHSV { get; set; } = true;

        [Parameter]
        public bool ShowRGBA { get; set; } = true;

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public bool ShowColors { get; set; } = true;

        [Parameter]
        public bool ShowButton { get; set; } = true;

        [Parameter]
        public string HexText { get; set; } = "Hex";

        [Parameter]
        public string RedText { get; set; } = "R";

        [Parameter]
        public string GreenText { get; set; } = "G";

        [Parameter]
        public string BlueText { get; set; } = "B";

        [Parameter]
        public string AlphaText { get; set; } = "A";

        [Parameter]
        public string ButtonText { get; set; } = "OK";


        protected async Task OnChange(string value)
        {
            await Editor.ExecuteCommandAsync(CommandName, value);
        }
    }
}

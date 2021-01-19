using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public abstract class RadzenHtmlEditorButtonBase : ComponentBase
    {
        [CascadingParameter]
        public RadzenHtmlEditor Editor { get; set; }

        protected virtual string CommandName { get; }

        protected virtual async Task OnClick()
        {
            await Editor.ExecuteCommandAsync(CommandName);
        }
    }
}

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Base class that RadzenHtmlEditor color picker tools inherit from.
    /// </summary>
    public abstract class RadzenHtmlEditorButtonBase : ComponentBase
    {
        /// <summary>
        /// The RadzenHtmlEditor component which this tool is part of.
        /// </summary>
        [CascadingParameter]
        public RadzenHtmlEditor Editor { get; set; }

        /// <summary>
        /// Specifies the name of the command. It is available as <see cref="HtmlEditorExecuteEventArgs.CommandName" /> when
        /// <see cref="RadzenHtmlEditor.Execute" /> is raised.
        /// </summary>
        protected virtual string CommandName { get; }

        /// <summary>
        /// Handles the click event of the button. Executes the command.
        /// </summary>
        protected virtual async Task OnClick()
        {
            await Editor.ExecuteCommandAsync(CommandName);
        }
    }
}

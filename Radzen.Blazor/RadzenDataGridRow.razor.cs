using Microsoft.AspNetCore.Components.Web;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenDataGridRow.
    /// </summary>
    public partial class RadzenDataGridRow<
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)] TItem> where TItem : notnull
    {
        bool stopKeydownPropagation = true;

        void OnGuardKeyDown(KeyboardEventArgs args)
        {
            var key = args.Code ?? args.Key;
            stopKeydownPropagation = key != "Escape";
        }
    }
}
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Web;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenDataGridHeaderCell.
    /// </summary>
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2026, Justification = TrimMessages.DataTypePreserved)]
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2072, Justification = TrimMessages.DataTypePreserved)]
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2087, Justification = TrimMessages.DataTypePreserved)]
    public partial class RadzenDataGridHeaderCell<TItem> where TItem : notnull
    {
        bool stopKeydownPropagation = true;

        void OnGuardKeyDown(KeyboardEventArgs args)
        {
            var key = args.Code ?? args.Key;
            stopKeydownPropagation = key != "Escape";
        }
    }
}


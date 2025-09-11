using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenFab component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenFab Icon="add" Click=@(args => Console.WriteLine("FAB clicked")) /&gt;
    /// &lt;RadzenFab Icon="add" IsBusy="@isLoading" BusyText="Loading..." Click=@OnFabClick /&gt;
    /// </code>
    /// </example>
    public partial class RadzenFab : RadzenButton
    {
        /// <summary>
        /// Gets or sets the size. Defaults to Large for FAB components.
        /// </summary>
        /// <value>The size.</value>
        public new ButtonSize Size { get; set; } = ButtonSize.Large;

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        protected override string GetComponentCssClass()
        {
            return ClassList.Create("rz-button rz-fab")
                           .AddButtonSize(Size)
                           .AddVariant(Variant)
                           .AddButtonStyle(ButtonStyle)
                           .AddDisabled(IsDisabled)
                           .AddShade(Shade)
                           .Add($"rz-button-icon-only", string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(Icon))
                           .ToString();
        }
    }
} 
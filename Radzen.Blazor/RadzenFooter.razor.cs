using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenFooter component.
    /// </summary>
    public partial class RadzenFooter : RadzenComponentWithChildren
    {
        /// <summary>
        /// The <see cref="RadzenLayout" /> this component is nested in.
        /// </summary>
        [CascadingParameter]
        public RadzenLayout Layout { get; set; }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return ClassList.Create("rz-footer")
                            .Add("fixed", Layout == null)
                            .ToString();
        }
    }
}

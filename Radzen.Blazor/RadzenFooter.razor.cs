namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenFooter component.
    /// </summary>
    public partial class RadzenFooter : RadzenComponentWithChildren
    {
        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return "footer fixed";
        }
    }
}
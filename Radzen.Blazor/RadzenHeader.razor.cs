namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenHeader component.
    /// </summary>
    public partial class RadzenHeader : RadzenComponentWithChildren
    {
        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return "header fixed";
        }
    }
}
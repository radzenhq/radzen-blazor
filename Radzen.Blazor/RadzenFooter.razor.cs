namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenFooter component.
    /// </summary>
    public partial class RadzenFooter : RadzenComponentWithChildren
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "footer fixed";
        }
    }
}

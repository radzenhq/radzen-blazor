namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenHeader component.
    /// </summary>
    public partial class RadzenHeader : RadzenComponentWithChildren
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "header fixed";
        }
    }
}

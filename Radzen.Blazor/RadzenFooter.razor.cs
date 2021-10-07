namespace Radzen.Blazor
{
    public partial class RadzenFooter : RadzenComponentWithChildren
    {
        protected override string GetComponentCssClass()
        {
            return "footer fixed";
        }
    }
}
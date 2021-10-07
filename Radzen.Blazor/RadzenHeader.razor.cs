namespace Radzen.Blazor
{
    public partial class RadzenHeader : RadzenComponentWithChildren
    {
        protected override string GetComponentCssClass()
        {
            return "header fixed";
        }
    }
}
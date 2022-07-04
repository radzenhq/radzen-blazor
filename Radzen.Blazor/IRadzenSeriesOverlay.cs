using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    internal interface IRadzenSeriesOverlay
    {
        RenderFragment Render(ScaleBase categoryScale, ScaleBase valueScale);
    }
}

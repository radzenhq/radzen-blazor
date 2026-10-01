using Bunit;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class GoogleMapTests
    {
        [Fact]
        public async Task GoogleMap_Raises_BoundsChanged_WithVisibleArea()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            GoogleMapBoundsChangedEventArgs? raised = null;

            var component = ctx.RenderComponent<RadzenGoogleMap>(parameters =>
                parameters.Add(p => p.BoundsChanged, (GoogleMapBoundsChangedEventArgs args) => raised = args));

            var args = JsonSerializer.Deserialize<GoogleMapBoundsChangedEventArgs>(
                "{\"NorthEast\":{\"Lat\":52.5,\"Lng\":13.5},\"SouthWest\":{\"Lat\":48.1,\"Lng\":2.3},\"Center\":{\"Lat\":50.3,\"Lng\":7.9},\"Zoom\":5.5}");

            await component.InvokeAsync(() => component.Instance.OnBoundsChanged(args!));

            Assert.NotNull(raised);
            Assert.Equal(52.5, raised!.NorthEast!.Lat);
            Assert.Equal(13.5, raised.NorthEast.Lng);
            Assert.Equal(48.1, raised.SouthWest!.Lat);
            Assert.Equal(2.3, raised.SouthWest.Lng);
            Assert.Equal(50.3, raised.Center!.Lat);
            Assert.Equal(7.9, raised.Center.Lng);
            Assert.Equal(5.5, raised.Zoom);
        }
    }
}

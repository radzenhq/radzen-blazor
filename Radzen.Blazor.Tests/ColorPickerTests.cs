using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ColorPickerTests
    {
        [Fact]
        public void ColorPicker_ShouldAcceptInvalidValues()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenColorPicker>(ComponentParameter.CreateParameter("Value", "invalid"));
        }
    }
}

using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class AutoCompleteDisabledTests
    {
        [Fact]
        public async System.Threading.Tasks.Task AutoComplete_Ignores_Option_Click_After_Disabling()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var value = "Apple";
            var component = ctx.RenderComponent<RadzenAutoComplete>(parameters => parameters
                .Add(p => p.Data, new[] { "Apple", "Orange" })
                .Add(p => p.OpenOnFocus, true)
                .Add(p => p.Value, value)
                .Add(p => p.ValueChanged, (string selected) => { value = selected; }));

            await component.InvokeAsync(() => component.Instance.OnPopupOpen());
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Disabled, true));
            component.FindAll("[role=option]")[1].Click();

            Assert.Equal("Apple", value);
        }
    }
}

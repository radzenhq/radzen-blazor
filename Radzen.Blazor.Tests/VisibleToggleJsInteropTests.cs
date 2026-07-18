using Bunit;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class VisibleToggleJsInteropTests
    {
        static TestContext CreateContext()
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            return ctx;
        }

        static int Count(TestContext ctx, string identifier)
        {
            return ctx.JSInterop.Invocations.Count(i => i.Identifier == identifier);
        }

        static void AssertRecreatedOnVisibleToggle<TComponent>(string identifier)
            where TComponent : RadzenComponent
        {
            using var ctx = CreateContext();

            var component = ctx.RenderComponent<TComponent>();

            Assert.Equal(1, Count(ctx, identifier));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, false));
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, true));

            Assert.Equal(2, Count(ctx, identifier));
        }

        [Fact]
        public void ProfileMenu_RecreatesJsHandler_WhenVisibleToggled()
        {
            AssertRecreatedOnVisibleToggle<RadzenProfileMenu>("Radzen.createProfileMenu");
        }

        [Fact]
        public void Menu_RecreatesJsHandler_WhenVisibleToggled()
        {
            AssertRecreatedOnVisibleToggle<RadzenMenu>("Radzen.createMenu");
        }

        [Fact]
        public void Accordion_RecreatesJsHandler_WhenVisibleToggled()
        {
            AssertRecreatedOnVisibleToggle<RadzenAccordion>("Radzen.createAccordion");
        }

        [Fact]
        public void AutoComplete_RecreatesJsHandler_WhenVisibleToggled()
        {
            AssertRecreatedOnVisibleToggle<RadzenAutoComplete>("Radzen.createAutoComplete");
        }

        [Fact]
        public void Mask_RecreatesJsHandler_WhenVisibleToggled()
        {
            AssertRecreatedOnVisibleToggle<RadzenMask>("Radzen.createMask");
        }

        [Fact]
        public void Numeric_RecreatesJsHandler_WhenVisibleToggled()
        {
            AssertRecreatedOnVisibleToggle<RadzenNumeric<int>>("Radzen.createNumeric");
        }

        [Fact]
        public void Upload_RecreatesJsHandler_WhenVisibleToggled()
        {
            AssertRecreatedOnVisibleToggle<RadzenUpload>("Radzen.createUpload");
        }

        [Fact]
        public void Carousel_RecreatesJsHandler_WhenVisibleToggled()
        {
            AssertRecreatedOnVisibleToggle<RadzenCarousel>("Radzen.createCarousel");
        }

        [Fact]
        public void FormField_RecreatesJsHandler_WhenVisibleToggled()
        {
            AssertRecreatedOnVisibleToggle<RadzenFormField>("Radzen.createFormField");
        }

        [Fact]
        public void SignaturePad_RecreatesJsHandler_WhenVisibleToggled()
        {
            AssertRecreatedOnVisibleToggle<RadzenSignaturePad>("Radzen.createSignaturePad");
        }

        [Fact]
        public void GoogleMap_RecreatesJsHandler_WhenVisibleToggled()
        {
            AssertRecreatedOnVisibleToggle<RadzenGoogleMap>("Radzen.createMap");
        }

        [Fact]
        public void ProfileMenu_CreatesJsHandler_WhenInitiallyHiddenBecomesVisible()
        {
            using var ctx = CreateContext();

            var component = ctx.RenderComponent<RadzenProfileMenu>(parameters => parameters
                .Add(p => p.Visible, false));

            Assert.Equal(0, Count(ctx, "Radzen.createProfileMenu"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, true));

            Assert.Equal(1, Count(ctx, "Radzen.createProfileMenu"));
        }

        [Fact]
        public void Accordion_CreatesJsHandler_WhenInitiallyHiddenBecomesVisible()
        {
            using var ctx = CreateContext();

            var component = ctx.RenderComponent<RadzenAccordion>(parameters => parameters
                .Add(p => p.Visible, false));

            Assert.Equal(0, Count(ctx, "Radzen.createAccordion"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, true));

            Assert.Equal(1, Count(ctx, "Radzen.createAccordion"));
        }

        [Fact]
        public void Menu_DoesNotRecreateJsHandler_OnUnrelatedRender()
        {
            using var ctx = CreateContext();

            var component = ctx.RenderComponent<RadzenMenu>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Style, "width: 100px"));

            Assert.Equal(1, Count(ctx, "Radzen.createMenu"));
        }
    }
}

using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class CarouselTests
    {
        [Fact]
        public void Carousel_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenCarousel>();

            Assert.Contains(@"rz-carousel", component.Markup);
        }

        [Fact]
        public void Carousel_Renders_AllowPaging_True()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenCarousel>(parameters =>
            {
                parameters.Add(p => p.AllowPaging, true);
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenCarouselItem>(0);
                    builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Slide 1")));
                    builder.CloseComponent();
                    
                    builder.OpenComponent<RadzenCarouselItem>(2);
                    builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Slide 2")));
                    builder.CloseComponent();
                });
            });

            Assert.Contains("rz-carousel-pager-button", component.Markup);
        }

        [Fact]
        public void Carousel_Renders_AllowPaging_False()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenCarousel>(parameters =>
            {
                parameters.Add(p => p.AllowPaging, false);
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenCarouselItem>(0);
                    builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Slide 1")));
                    builder.CloseComponent();
                });
            });

            Assert.DoesNotContain("rz-carousel-pager-button", component.Markup);
        }

        [Fact]
        public void Carousel_Renders_AllowNavigation_True()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenCarousel>(parameters =>
            {
                parameters.Add(p => p.AllowNavigation, true);
            });

            Assert.Contains("rz-carousel-prev", component.Markup);
            Assert.Contains("rz-carousel-next", component.Markup);
        }

        [Fact]
        public void Carousel_Renders_AllowNavigation_False()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenCarousel>(parameters =>
            {
                parameters.Add(p => p.AllowNavigation, false);
            });

            Assert.DoesNotContain("rz-carousel-prev", component.Markup);
            Assert.DoesNotContain("rz-carousel-next", component.Markup);
            Assert.Contains("rz-carousel-no-navigation", component.Markup);
        }

        [Fact]
        public void Carousel_Renders_PagerPosition_Top()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenCarousel>(parameters =>
            {
                parameters.Add(p => p.PagerPosition, PagerPosition.Top);
                parameters.Add(p => p.AllowPaging, true);
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenCarouselItem>(0);
                    builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Slide")));
                    builder.CloseComponent();
                });
            });

            Assert.Contains("rz-carousel-pager-top", component.Markup);
        }

        [Fact]
        public void Carousel_Renders_PagerPosition_Bottom()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenCarousel>(parameters =>
            {
                parameters.Add(p => p.PagerPosition, PagerPosition.Bottom);
                parameters.Add(p => p.AllowPaging, true);
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenCarouselItem>(0);
                    builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Slide")));
                    builder.CloseComponent();
                });
            });

            Assert.Contains("rz-carousel-pager-bottom", component.Markup);
        }

        [Fact]
        public void Carousel_Renders_PagerPosition_TopAndBottom()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenCarousel>(parameters =>
            {
                parameters.Add(p => p.PagerPosition, PagerPosition.TopAndBottom);
                parameters.Add(p => p.AllowPaging, true);
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenCarouselItem>(0);
                    builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Slide")));
                    builder.CloseComponent();
                });
            });

            Assert.Contains("rz-carousel-pager-top", component.Markup);
            Assert.Contains("rz-carousel-pager-bottom", component.Markup);
        }

        [Fact]
        public void Carousel_Renders_PagerOverlay_True()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenCarousel>(parameters =>
            {
                parameters.Add(p => p.PagerOverlay, true);
            });

            Assert.Contains("rz-carousel-pager-overlay", component.Markup);
        }

        [Fact]
        public void Carousel_Renders_PagerOverlay_False()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenCarousel>(parameters =>
            {
                parameters.Add(p => p.PagerOverlay, false);
            });

            Assert.DoesNotContain("rz-carousel-pager-overlay", component.Markup);
        }
    }
}


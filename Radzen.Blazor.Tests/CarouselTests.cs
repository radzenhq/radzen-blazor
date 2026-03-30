using Bunit;
using Microsoft.AspNetCore.Components;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class CarouselTests
    {
        [Fact]
        public void Carousel_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
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
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
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
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
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
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
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
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenCarousel>(parameters =>
            {
                parameters.Add(p => p.PagerOverlay, false);
            });

            Assert.DoesNotContain("rz-carousel-pager-overlay", component.Markup);
        }

        [Fact]
        public void Carousel_ItemsPerPage_DefaultIsOne()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenCarousel>();

            Assert.Equal(1, component.Instance.ItemsPerPage);
        }

        [Fact]
        public void Carousel_ItemsPerPage_SetsFlexBasisOnItems()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenCarousel>(parameters =>
            {
                parameters.Add(p => p.ItemsPerPage, 3);
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenCarouselItem>(0);
                    builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Slide 1")));
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenCarouselItem>(2);
                    builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Slide 2")));
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenCarouselItem>(4);
                    builder.AddAttribute(5, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Slide 3")));
                    builder.CloseComponent();
                });
            });

            Assert.Contains("calc(100% / 3)", component.Markup);
        }

        [Fact]
        public void Carousel_ItemsPerPage_PagerShowsPageCount()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenCarousel>(parameters =>
            {
                parameters.Add(p => p.ItemsPerPage, 2);
                parameters.Add(p => p.AllowPaging, true);
                parameters.Add(p => p.Items, builder =>
                {
                    for (int i = 0; i < 5; i++)
                    {
                        var seq = i * 2;
                        builder.OpenComponent<RadzenCarouselItem>(seq);
                        builder.AddAttribute(seq + 1, "ChildContent", (RenderFragment)(b => b.AddContent(0, $"Slide {i + 1}")));
                        builder.CloseComponent();
                    }
                });
            });

            // 5 items / 2 per page = 3 pages (ceil), so 3 pager buttons
            var pagerButtons = component.FindAll(".rz-carousel-pager-button");
            Assert.Equal(3, pagerButtons.Count);
        }

        [Fact]
        public void Carousel_ItemsPerPage_One_NoInlineStyle()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenCarousel>(parameters =>
            {
                parameters.Add(p => p.ItemsPerPage, 1);
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenCarouselItem>(0);
                    builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Slide 1")));
                    builder.CloseComponent();
                });
            });

            // With ItemsPerPage=1 (default), no flex override style should be applied
            Assert.DoesNotContain("calc(100%", component.Markup);
        }

        [Fact]
        public void Carousel_ItemsPerPage_PagerShowsCorrectActiveButton()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenCarousel>(parameters =>
            {
                parameters.Add(p => p.ItemsPerPage, 3);
                parameters.Add(p => p.AllowPaging, true);
                parameters.Add(p => p.Items, builder =>
                {
                    for (int i = 0; i < 9; i++)
                    {
                        var seq = i * 2;
                        builder.OpenComponent<RadzenCarouselItem>(seq);
                        builder.AddAttribute(seq + 1, "ChildContent", (RenderFragment)(b => b.AddContent(0, $"Slide {i + 1}")));
                        builder.CloseComponent();
                    }
                });
            });

            // 9 items / 3 per page = 3 pages
            var pagerButtons = component.FindAll(".rz-carousel-pager-button");
            Assert.Equal(3, pagerButtons.Count);

            // First page button should be active by default (selectedIndex=0)
            Assert.Contains("rz-state-active", pagerButtons[0].GetAttribute("class"));
            Assert.DoesNotContain("rz-state-active", pagerButtons[1].GetAttribute("class"));
            Assert.DoesNotContain("rz-state-active", pagerButtons[2].GetAttribute("class"));
        }

        [Fact]
        public void Carousel_ItemsPerPage_PagerWithUnevenItems()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenCarousel>(parameters =>
            {
                parameters.Add(p => p.ItemsPerPage, 3);
                parameters.Add(p => p.AllowPaging, true);
                parameters.Add(p => p.Items, builder =>
                {
                    for (int i = 0; i < 7; i++)
                    {
                        var seq = i * 2;
                        builder.OpenComponent<RadzenCarouselItem>(seq);
                        builder.AddAttribute(seq + 1, "ChildContent", (RenderFragment)(b => b.AddContent(0, $"Slide {i + 1}")));
                        builder.CloseComponent();
                    }
                });
            });

            // 7 items / 3 per page = 3 pages (ceil)
            var pagerButtons = component.FindAll(".rz-carousel-pager-button");
            Assert.Equal(3, pagerButtons.Count);
        }

        [Fact]
        public void Carousel_ItemsPerPage_AllItemsSetFlexBasis()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenCarousel>(parameters =>
            {
                parameters.Add(p => p.ItemsPerPage, 4);
                parameters.Add(p => p.Items, builder =>
                {
                    for (int i = 0; i < 6; i++)
                    {
                        var seq = i * 2;
                        builder.OpenComponent<RadzenCarouselItem>(seq);
                        builder.AddAttribute(seq + 1, "ChildContent", (RenderFragment)(b => b.AddContent(0, $"Slide {i + 1}")));
                        builder.CloseComponent();
                    }
                });
            });

            // All 6 items should have the flex style
            var items = component.FindAll(".rz-carousel-item");
            Assert.Equal(6, items.Count);
            foreach (var item in items)
            {
                Assert.Contains("calc(100% / 4)", item.GetAttribute("style"));
            }
        }

        [Fact]
        public void Carousel_ItemsPerPage_SingleItem_OnePage()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenCarousel>(parameters =>
            {
                parameters.Add(p => p.ItemsPerPage, 3);
                parameters.Add(p => p.AllowPaging, true);
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenCarouselItem>(0);
                    builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Only slide")));
                    builder.CloseComponent();
                });
            });

            // 1 item / 3 per page = 1 page
            var pagerButtons = component.FindAll(".rz-carousel-pager-button");
            Assert.Single(pagerButtons);
        }

        [Fact]
        public void Carousel_ItemsPerPage_PagerButtonCount_WithTopAndBottom()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenCarousel>(parameters =>
            {
                parameters.Add(p => p.ItemsPerPage, 2);
                parameters.Add(p => p.AllowPaging, true);
                parameters.Add(p => p.PagerPosition, PagerPosition.TopAndBottom);
                parameters.Add(p => p.Items, builder =>
                {
                    for (int i = 0; i < 6; i++)
                    {
                        var seq = i * 2;
                        builder.OpenComponent<RadzenCarouselItem>(seq);
                        builder.AddAttribute(seq + 1, "ChildContent", (RenderFragment)(b => b.AddContent(0, $"Slide {i + 1}")));
                        builder.CloseComponent();
                    }
                });
            });

            // 6 items / 2 per page = 3 pages, rendered in both top and bottom = 6 buttons total
            var pagerButtons = component.FindAll(".rz-carousel-pager-button");
            Assert.Equal(6, pagerButtons.Count);
        }
    }
}


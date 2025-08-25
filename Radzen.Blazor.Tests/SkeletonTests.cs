using Bunit;
using Radzen.Blazor.Rendering;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SkeletonTests
    {
        [Fact]
        public void Skeleton_Renders_CssClass()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSkeleton>();

            Assert.Contains("rz-skeleton", component.Markup);
            Assert.Contains("rz-skeleton-text", component.Markup);
        }

        [Fact]
        public void Skeleton_Renders_TypeParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSkeleton>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Variant, SkeletonVariant.Circular));

            Assert.Contains("rz-skeleton", component.Markup);
            Assert.Contains("rz-skeleton-circular", component.Markup);
        }

        [Theory]
        [InlineData(SkeletonVariant.Text, "rz-skeleton-text")]
        [InlineData(SkeletonVariant.Circular, "rz-skeleton-circular")]
        [InlineData(SkeletonVariant.Rectangular, "rz-skeleton-rectangular")]
        public void Skeleton_Renders_AllTypes(SkeletonVariant type, string expectedClass)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSkeleton>(parameters => parameters.Add(p => p.Variant, type));

            Assert.Contains("rz-skeleton", component.Markup);
            Assert.Contains(expectedClass, component.Markup);
        }

        [Fact]
        public void Skeleton_Renders_AnimationParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSkeleton>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Animation, SkeletonAnimation.Wave));

            Assert.Contains("rz-skeleton", component.Markup);
            Assert.Contains("rz-skeleton-wave", component.Markup);
        }

        [Theory]
        [InlineData(SkeletonAnimation.Wave, "rz-skeleton-wave")]
        [InlineData(SkeletonAnimation.Pulse, "rz-skeleton-pulse")]
        public void Skeleton_Renders_AllAnimations(SkeletonAnimation animation, string expectedClass)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSkeleton>(parameters => parameters.Add(p => p.Animation, animation));

            Assert.Contains("rz-skeleton", component.Markup);
            Assert.Contains(expectedClass, component.Markup);
        }

        [Fact]
        public void Skeleton_Renders_StyleParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSkeleton>();

            var style = "width: 200px; height: 20px;";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Style, style));

            Assert.Contains($"style=\"{style}\"", component.Markup);
        }

        [Fact]
        public void Skeleton_Renders_VisibleParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSkeleton>();

            // Should be visible by default
            Assert.Contains("rz-skeleton", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, false));

            // Should not render when not visible
            Assert.DoesNotContain("rz-skeleton", component.Markup);
        }

        [Fact]
        public void Skeleton_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSkeleton>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("data-testid", "skeleton-test"));

            Assert.Contains("data-testid=\"skeleton-test\"", component.Markup);
        }

        [Fact]
        public void Skeleton_DefaultType_IsText()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSkeleton>();

            // Should render with text type by default
            Assert.Contains("rz-skeleton-text", component.Markup);
        }

        [Fact]
        public void Skeleton_DefaultAnimation_IsNone()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSkeleton>();

            // Should not render animation classes by default
            Assert.DoesNotContain("rz-skeleton-wave", component.Markup);
            Assert.DoesNotContain("rz-skeleton-pulse", component.Markup);
        }
    }
} 
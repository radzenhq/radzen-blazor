using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class GravatarTests
    {
        [Fact]
        public void Gravatar_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenGravatar>();

            Assert.Contains("rz-gravatar", component.Markup);
        }

        [Fact]
        public void Gravatar_Renders_ImgTag()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenGravatar>();

            Assert.Contains("<img", component.Markup);
        }

        [Fact]
        public void Gravatar_Renders_GravatarUrl()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenGravatar>(parameters =>
            {
                parameters.Add(p => p.Email, "test@example.com");
            });

            Assert.Contains("secure.gravatar.com/avatar/", component.Markup);
        }

        [Fact]
        public void Gravatar_Renders_DefaultAlternateText()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenGravatar>();

            Assert.Contains(@"alt=""gravatar""", component.Markup);
        }

        [Fact]
        public void Gravatar_Renders_CustomAlternateText()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenGravatar>(parameters =>
            {
                parameters.Add(p => p.AlternateText, "User Avatar");
            });

            Assert.Contains("User Avatar", component.Markup);
        }

        [Fact]
        public void Gravatar_Renders_DefaultSize()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenGravatar>();

            Assert.Contains("s=36", component.Markup);
        }

        [Fact]
        public void Gravatar_Renders_CustomSize()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenGravatar>(parameters =>
            {
                parameters.Add(p => p.Size, 80);
            });

            Assert.Contains("s=80", component.Markup);
        }

        [Fact]
        public void Gravatar_Renders_RetroStyle()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenGravatar>();

            Assert.Contains("d=retro", component.Markup);
        }

        [Fact]
        public void Gravatar_DifferentEmails_ProduceDifferentUrls()
        {
            using var ctx = new TestContext();

            var component1 = ctx.RenderComponent<RadzenGravatar>(parameters =>
            {
                parameters.Add(p => p.Email, "user1@example.com");
            });

            var component2 = ctx.RenderComponent<RadzenGravatar>(parameters =>
            {
                parameters.Add(p => p.Email, "user2@example.com");
            });

            // Different emails should produce different MD5 hashes in the URL
            Assert.NotEqual(component1.Markup, component2.Markup);
        }

        [Fact]
        public void Gravatar_NullEmail_StillRenders()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenGravatar>();

            // Should render without throwing even with null email
            Assert.Contains("secure.gravatar.com/avatar/", component.Markup);
        }

        [Fact]
        public void Gravatar_Renders_StyleParameter()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenGravatar>(parameters =>
            {
                parameters.Add(p => p.Style, "border-radius:50%");
            });

            Assert.Contains("border-radius:50%", component.Markup);
        }

        [Fact]
        public void Gravatar_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenGravatar>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("<img", component.Markup);
        }
    }
}

using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class FileInputTests
    {
        [Fact]
        public void FileInput_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFileInput<string>>();

            Assert.Contains(@"rz-fileupload", component.Markup);
        }

        [Fact]
        public void FileInput_Renders_ChooseButton()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFileInput<string>>();

            Assert.Contains("rz-fileupload-choose", component.Markup);
            Assert.Contains("rz-button", component.Markup);
        }

        [Fact]
        public void FileInput_Renders_ChooseText()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFileInput<string>>(parameters =>
            {
                parameters.Add(p => p.ChooseText, "Select File");
            });

            Assert.Contains("Select File", component.Markup);
        }

        [Fact]
        public void FileInput_Renders_DefaultChooseText()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFileInput<string>>();

            Assert.Contains("Choose", component.Markup);
        }

        [Fact]
        public void FileInput_Renders_Disabled()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFileInput<string>>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
            });

            Assert.Contains("rz-state-disabled", component.Markup);
            Assert.Contains("disabled", component.Markup);
        }

        [Fact]
        public void FileInput_Renders_Accept()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFileInput<string>>(parameters =>
            {
                parameters.Add(p => p.Accept, "application/pdf");
            });

            Assert.Contains("accept=\"application/pdf\"", component.Markup);
        }

        [Fact]
        public void FileInput_Renders_DefaultAccept()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFileInput<string>>();

            Assert.Contains("accept=\"image/*\"", component.Markup);
        }

        [Fact]
        public void FileInput_Renders_FileInputElement()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFileInput<string>>();

            Assert.Contains("type=\"file\"", component.Markup);
        }

        [Fact]
        public void FileInput_Renders_Title_WhenSet()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFileInput<string>>(parameters =>
            {
                parameters.Add(p => p.Title, "MyDocument.pdf");
                parameters.Add(p => p.Value, "data:application/pdf;base64,test");
            });

            Assert.Contains("MyDocument.pdf", component.Markup);
        }

        [Fact]
        public void FileInput_Renders_FileName_WhenTitleNotSet()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFileInput<string>>(parameters =>
            {
                parameters.Add(p => p.FileName, "document.pdf");
                parameters.Add(p => p.Value, "data:application/pdf;base64,test");
            });

            Assert.Contains("document.pdf", component.Markup);
        }

        [Fact]
        public void FileInput_Renders_DeleteButton_WhenValueSet()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFileInput<string>>(parameters =>
            {
                parameters.Add(p => p.Value, "data:text/plain;base64,test");
            });

            Assert.Contains("rz-icon-trash", component.Markup);
        }

        [Fact]
        public void FileInput_Renders_CustomDeleteText()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFileInput<string>>(parameters =>
            {
                parameters.Add(p => p.DeleteText, "Remove File");
                parameters.Add(p => p.Value, "data:text/plain;base64,test");
            });

            Assert.Contains("title=\"Remove File\"", component.Markup);
        }

        [Fact]
        public void FileInput_Renders_ImagePreview_ForImageFile()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFileInput<string>>(parameters =>
            {
                parameters.Add(p => p.Value, "data:image/png;base64,test");
            });

            Assert.Contains("<img", component.Markup);
        }

        [Fact]
        public void FileInput_Renders_ImageAlternateText()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFileInput<string>>(parameters =>
            {
                parameters.Add(p => p.ImageAlternateText, "User Photo");
                parameters.Add(p => p.Value, "data:image/png;base64,test");
            });

            Assert.Contains("alt=\"User Photo\"", component.Markup);
        }
    }
}


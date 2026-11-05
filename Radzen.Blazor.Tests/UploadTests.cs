using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class UploadTests
    {
        [Fact]
        public void Upload_RejectsInvalidLimits()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            Assert.Throws<InvalidOperationException>(() => ctx.RenderComponent<RadzenUpload>(p => p.Add(x => x.MaxFileSize, 1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => ctx.RenderComponent<RadzenUpload>(p => p.Add(x => x.MaxFileSize, -1)));
            Assert.Throws<ArgumentException>(() => ctx.RenderComponent<RadzenUpload>(p => p
                .Add(x => x.Url, "api/upload").Add(x => x.AllowedExtensions, new[] { "image/*" })));
            Assert.Throws<ArgumentException>(() => ctx.RenderComponent<RadzenUpload>(p => p
                .Add(x => x.Url, "api/upload").Add(x => x.AllowedExtensions, new[] { "." })));
        }

        [Fact]
        public void Upload_UpdatesLimits_WithoutRecreatingHandler()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var module = ctx.JSInterop.SetupModule("Radzen.createUpload", _ => true);
            module.SetupVoid("updateValidation", _ => true).SetVoidResult();
            var extensions = new List<string> { " .PDF ", "pdf" };
            var component = ctx.RenderComponent<RadzenUpload>(p => p
                .Add(x => x.Url, "api/upload").Add(x => x.MaxFileSize, 10)
                .Add(x => x.AllowedExtensions, extensions));
            var creation = ctx.JSInterop.Invocations["Radzen.createUpload"].Single();
            Assert.Equal(10L, creation.Arguments[7]);
            Assert.Equal(new[] { ".pdf" }, (string[])creation.Arguments[8]);
            Assert.DoesNotContain(module.Invocations, invocation => invocation.Identifier == "updateValidation");
            extensions.Add(".txt");
            component.SetParametersAndRender(p => p.Add(x => x.AllowedExtensions, extensions));
            Assert.Single(ctx.JSInterop.Invocations["Radzen.createUpload"]);
            Assert.Single(ctx.JSInterop.Invocations["Radzen.uploads"]);
            Assert.Equal(new[] { ".pdf", ".txt" }, (string[])module.Invocations["updateValidation"].Single().Arguments[1]);
        }

        [Fact]
        public async Task Upload_ReportsRejection_WithoutChangingSelection()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            UploadRejectedEventArgs rejected = null;
            var changes = 0;
            var component = ctx.RenderComponent<RadzenUpload>(p => p.Add(x => x.Url, "api/upload")
                .Add(x => x.Change, _ => changes++)
                .Add(x => x.Rejected, args => rejected = args));
            await component.InvokeAsync(() => component.Instance.OnChange(new[] { new PreviewFileInfo { Name = "keep.pdf", Size = 1 } }));
            var args = new UploadRejectedEventArgs { MaxFileSize = 10, Files = new[] {
                new UploadRejectedFile { File = new FileInfo { Name = "large.pdf", Size = 11 }, Reason = UploadRejectionReason.FileTooLarge }
            }};
            await component.InvokeAsync(() => component.Instance.OnRejected(args));
            Assert.Same(args, rejected);
            Assert.Equal(1, changes);
            Assert.True(component.Instance.HasValue);
            Assert.Contains("keep.pdf", component.Markup);
            Assert.DoesNotContain("large.pdf", component.Markup);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("api/upload")]
        public void Upload_Renders_ChooseTemplate(string url)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>(parameters => parameters
                .Add(p => p.Url, url)
                .Add(p => p.Icon, "upload")
                .Add(p => p.ChooseText, "Select attachments")
                .Add(p => p.TabIndex, 3)
                .Add(p => p.ChooseTemplate, "<span>Drop files here</span><span>Choose attachments</span>"));

            var choose = component.Find(".rz-fileupload-choose");
            Assert.Contains("rz-fileupload-choose-template", choose.ClassList);
            Assert.Contains("Drop files here", choose.TextContent);
            Assert.Equal("Select attachments", choose.GetAttribute("aria-label"));
            Assert.Equal("button", choose.GetAttribute("role"));
            Assert.Equal("3", choose.GetAttribute("tabindex"));
            Assert.Empty(component.FindAll(".rz-button-text, .rzi"));
            Assert.Single(component.FindAll("input[type=file]"));
            Assert.False(component.Find("input[type=file]").HasAttribute("style"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Disabled, true));

            Assert.Equal("true", choose.GetAttribute("aria-disabled"));
            Assert.Equal("-1", choose.GetAttribute("tabindex"));
            Assert.True(component.Find("input[type=file]").HasAttribute("disabled"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("api/upload")]
        public async Task Upload_PreservesSelection_WhenAppearanceChanges(string url)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var changes = 0;
            var component = ctx.RenderComponent<RadzenUpload>(parameters => parameters
                .Add(p => p.Url, url)
                .Add(p => p.Auto, false)
                .Add(p => p.Change, _ => changes++));
            var inputFile = url == null ? component.FindComponent<InputFile>().Instance : null;
            var selected = new PreviewFileInfo { Name = "report.pdf", Size = 42 };

            await component.InvokeAsync(() => component.Instance.OnChange(new[] { selected }));

            Assert.True(component.Instance.ShowFileList);
            Assert.Single(component.FindAll(".rz-fileupload-row"));

            component.SetParametersAndRender(parameters => parameters
                .Add(p => p.ShowFileList, false)
                .Add(p => p.ChooseTemplate, "<span>Custom content</span>"));

            Assert.Empty(component.FindAll(".rz-fileupload-content"));
            Assert.True(component.Instance.HasValue);

            component.SetParametersAndRender(parameters => parameters
                .Add(p => p.ShowFileList, true)
                .Add(p => p.ChooseTemplate, (RenderFragment)null));

            Assert.Contains("report.pdf", component.Find(".rz-fileupload-row").TextContent);
            Assert.DoesNotContain("rz-fileupload-choose-template", component.Find(".rz-fileupload-choose").ClassList);
            Assert.Single(component.FindAll(".rz-button-text"));
            Assert.Equal(1, changes);
            Assert.Single(ctx.JSInterop.Invocations["Radzen.createUpload"]);
            Assert.Single(ctx.JSInterop.Invocations["Radzen.uploads"]);
            if (inputFile != null)
            {
                Assert.Same(inputFile, component.FindComponent<InputFile>().Instance);
            }
        }

        [Fact]
        public async Task Upload_HiddenFileList_SupportsRemoveAndClear()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            UploadChangeEventArgs change = null;
            var changes = 0;
            var component = ctx.RenderComponent<RadzenUpload>(parameters => parameters
                .Add(p => p.Url, "api/upload")
                .Add(p => p.Auto, false)
                .Add(p => p.ShowFileList, false)
                .Add(p => p.Change, args => { change = args; changes++; }));

            await component.InvokeAsync(() => component.Instance.OnChange(new[]
            {
                new PreviewFileInfo { Name = "first.txt", Size = 1 },
                new PreviewFileInfo { Name = "second.txt", Size = 2 }
            }));
            Assert.Equal(2, change.Files.Count());

            await component.InvokeAsync(() => component.Instance.RemoveFile("first.txt"));
            Assert.Equal("second.txt", Assert.Single(change.Files).Name);
            Assert.True(component.Instance.HasValue);

            await component.InvokeAsync(() => component.Instance.Upload());
            Assert.Single(ctx.JSInterop.Invocations["Radzen.upload"]);

            await component.InvokeAsync(() => component.Instance.ClearFiles());
            Assert.Empty(change.Files);
            Assert.False(component.Instance.HasValue);
            Assert.Equal(3, changes);
            Assert.Empty(component.FindAll(".rz-fileupload-content"));
            Assert.Equal(2, ctx.JSInterop.Invocations["Radzen.removeFileFromUpload"].Count);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("api/upload")]
        public void Upload_ChooseTemplate_KeepsInputAttributesStyle(string url)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>(parameters => parameters
                .Add(p => p.Url, url)
                .Add(p => p.ChooseTemplate, "<span>Custom content</span>")
                .Add(p => p.InputAttributes, new Dictionary<string, object> { ["style"] = "color: red;", ["data-test"] = "1" }));

            var input = component.Find("input[type=file]");
            Assert.Equal("color: red;", input.GetAttribute("style"));
            Assert.Equal("1", input.GetAttribute("data-test"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ChooseTemplate, (RenderFragment)null));

            Assert.Equal("color: red;", component.Find("input[type=file]").GetAttribute("style"));
        }

        [Fact]
        public async Task Upload_RemoveFile_RespectsIgnoreCase()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            UploadChangeEventArgs change = null;
            var component = ctx.RenderComponent<RadzenUpload>(parameters => parameters
                .Add(p => p.Url, "api/upload")
                .Add(p => p.Auto, false)
                .Add(p => p.Change, args => change = args));

            await component.InvokeAsync(() => component.Instance.OnChange(new[]
            {
                new PreviewFileInfo { Name = "Report.pdf", Size = 1 },
                new PreviewFileInfo { Name = "report.pdf", Size = 2 }
            }));

            await component.InvokeAsync(() => component.Instance.RemoveFile("REPORT.PDF"));
            Assert.Equal(2, change.Files.Count());

            await component.InvokeAsync(() => component.Instance.RemoveFile("report.pdf"));
            Assert.Equal("Report.pdf", Assert.Single(change.Files).Name);

            await component.InvokeAsync(() => component.Instance.RemoveFile("REPORT.PDF", ignoreCase: true));
            Assert.Empty(change.Files);
        }

        [Fact]
        public void Upload_ChooseTemplate_PreservesHeaders()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>(parameters => parameters
                .Add(p => p.Url, "api/upload")
                .Add(p => p.ChooseTemplate, "<span>Custom content</span>")
                .AddChildContent<RadzenUploadHeader>(header => header
                    .Add(p => p.Name, "X-Upload-Test")
                    .Add(p => p.Value, "custom")));

            Assert.Equal("custom", component.Instance.GetHeaders()["X-Upload-Test"]);
        }

        [Fact]
        public void Upload_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>();

            Assert.Contains(@"rz-fileupload", component.Markup);
        }

        [Fact]
        public void Upload_Renders_Disabled()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
            });

            Assert.Contains("rz-state-disabled", component.Markup);
        }

        [Fact]
        public void Upload_Renders_ChooseText()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>(parameters =>
            {
                parameters.Add(p => p.ChooseText, "Select Files");
            });

            Assert.Contains("Select Files", component.Markup);
        }

        [Fact]
        public void Upload_Renders_DefaultChooseText()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>();

            Assert.Contains("Choose", component.Markup);
        }

        [Fact]
        public void Upload_Renders_Icon()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>(parameters =>
            {
                parameters.Add(p => p.Icon, "upload");
            });

            Assert.Contains("upload", component.Markup);
            Assert.Contains("rzi", component.Markup);
        }

        [Fact]
        public void Upload_Renders_Multiple_Attribute()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>(parameters =>
            {
                parameters.Add(p => p.Multiple, true);
            });

            Assert.Contains("multiple", component.Markup);
        }

        [Fact]
        public void Upload_Renders_Accept_Attribute()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>(parameters =>
            {
                parameters.Add(p => p.Accept, "image/*");
            });

            Assert.Contains("accept=\"image/*\"", component.Markup);
        }

        [Fact]
        public void Upload_Renders_DefaultTabIndex_OnChooseButton()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>();

            Assert.Equal("0", component.Find(".rz-fileupload-choose").GetAttribute("tabindex"));
        }

        [Fact]
        public void Upload_Renders_TabIndex_OnChooseButton_NotWrapper()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>(parameters =>
            {
                parameters.Add(p => p.TabIndex, 22);
            });

            Assert.Equal("22", component.Find(".rz-fileupload-choose").GetAttribute("tabindex"));
            Assert.False(component.Find(".rz-fileupload").HasAttribute("tabindex"));
        }

        [Fact]
        public async Task Upload_PassesMethodAndStream_WhenTriggeredFromCode()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>(parameters =>
            {
                parameters.Add(p => p.Auto, false);
                parameters.Add(p => p.Url, "api/upload");
                parameters.Add(p => p.Method, "PUT");
                parameters.Add(p => p.Stream, true);
            });

            await component.InvokeAsync(() => component.Instance.Upload());

            var invocation = ctx.JSInterop.Invocations["Radzen.upload"].Single();
            Assert.Equal("api/upload", invocation.Arguments[1]);
            Assert.Equal("PUT", invocation.Arguments[5]);
            Assert.Equal(true, invocation.Arguments[6]);
        }

        [Fact]
        public void Upload_Renders_DisabledTabIndex_OnChooseButton()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>(parameters =>
            {
                parameters.Add(p => p.TabIndex, 22);
                parameters.Add(p => p.Disabled, true);
            });

            Assert.Equal("-1", component.Find(".rz-fileupload-choose").GetAttribute("tabindex"));
        }

        [Fact]
        public async Task Upload_DoesNotThrow_WhenDisposedWhileRecreatingJsHandler()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var module = ctx.JSInterop.SetupModule("Radzen.createUpload", _ => true);
            var disposePlan = module.SetupVoid("dispose");

            var component = ctx.RenderComponent<RadzenUpload>(parameters => parameters.Add(p => p.Url, "upload/1"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Url, "upload/2"));

            ctx.DisposeComponents();

            disposePlan.SetVoidResult();

            var unhandled = ctx.Renderer.UnhandledException;
            var completed = await Task.WhenAny(unhandled, Task.Delay(500));
            Assert.NotSame(unhandled, completed);
            Assert.Equal(1, ctx.JSInterop.Invocations.Count(i => i.Identifier == "Radzen.createUpload"));
        }

        [Fact]
        public async Task Upload_CreatesSingleJsHandler_WhenRecreationsOverlap()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var module = ctx.JSInterop.SetupModule("Radzen.createUpload", _ => true);
            var disposePlan = module.SetupVoid("dispose");

            var component = ctx.RenderComponent<RadzenUpload>(parameters => parameters.Add(p => p.Url, "upload/1"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Url, "upload/2"));
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Url, "upload/3"));

            disposePlan.SetVoidResult();

            var unhandled = ctx.Renderer.UnhandledException;
            var completed = await Task.WhenAny(unhandled, Task.Delay(500));
            Assert.NotSame(unhandled, completed);
            Assert.Equal(2, ctx.JSInterop.Invocations.Count(i => i.Identifier == "Radzen.createUpload"));
        }

        [Fact]
        public async Task Upload_Renders_ImagePreview_OnlyForImageFiles()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>(parameters => parameters.Add(p => p.Url, "api/upload"));

            await component.InvokeAsync(() => component.Instance.OnChange(new[]
            {
                new PreviewFileInfo { Name = "photo.png", Size = 2048, ContentType = "image/png", Url = "blob:photo" },
                new PreviewFileInfo { Name = "report.pdf", Size = 4096, ContentType = "application/pdf", Url = "blob:report" },
                new PreviewFileInfo { Name = "unknown.bin", Size = 1024, Url = "blob:unknown" }
            }));

            var rows = component.FindAll(".rz-fileupload-row");
            Assert.Equal(3, rows.Count);

            var image = rows[0].QuerySelector("img");
            Assert.NotNull(image);
            Assert.Equal("blob:photo", image.GetAttribute("src"));
            Assert.Null(rows[0].QuerySelector(".rz-fileupload-file-icon"));

            Assert.Null(rows[1].QuerySelector("img"));
            Assert.NotNull(rows[1].QuerySelector(".rz-fileupload-file-icon"));

            Assert.Null(rows[2].QuerySelector("img"));
            Assert.NotNull(rows[2].QuerySelector(".rz-fileupload-file-icon"));
        }
    }
}

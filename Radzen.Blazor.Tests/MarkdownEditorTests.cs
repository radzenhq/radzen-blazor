using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class MarkdownEditorTests
    {
        static TestContext CreateContext()
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            return ctx;
        }

        [Fact]
        public void MarkdownEditor_Renders_WithClassName()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>();

            Assert.Contains("rz-markdown-editor", component.Markup);
            Assert.Contains("rz-markdown-editor-toolbar", component.Markup);
            Assert.Contains("rz-markdown-editor-textarea", component.Markup);
        }

        [Fact]
        public void MarkdownEditor_HidesToolbar_WhenShowToolbarFalse()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.ShowToolbar, false));

            Assert.DoesNotContain("rz-markdown-editor-toolbar", component.Markup);
        }

        [Fact]
        public void MarkdownEditor_EditMode_RendersNoPreview()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Value, "# Hi"));

            Assert.DoesNotContain("rz-markdown-editor-preview", component.Markup);
            Assert.DoesNotContain("hidden", component.Find("textarea").OuterHtml);
        }

        [Fact]
        public void MarkdownEditor_PreviewMode_HidesTextarea_AndRendersMarkdown()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "# Hi")
                .Add(x => x.Mode, MarkdownEditorMode.Preview));

            Assert.Contains("rz-markdown-editor-preview", component.Markup);
            Assert.Contains("<h1", component.Markup);
            Assert.True(component.Find("textarea").HasAttribute("hidden"));
        }

        [Fact]
        public void MarkdownEditor_SplitMode_RendersBoth()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "# Hi")
                .Add(x => x.Mode, MarkdownEditorMode.Split));

            Assert.Contains("rz-markdown-editor-preview", component.Markup);
            Assert.False(component.Find("textarea").HasAttribute("hidden"));
        }

        [Fact]
        public void MarkdownEditor_PreviewMode_ShowsPlaceholder_WhenEmpty()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Mode, MarkdownEditorMode.Preview));

            Assert.Contains("Nothing to preview", component.Markup);
        }

        [Fact]
        public void MarkdownEditor_ModeSwitcher_RaisesModeChanged()
        {
            using var ctx = CreateContext();
            MarkdownEditorMode? changed = null;
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.ModeChanged, m => changed = m));

            component.Find(".rz-markdown-editor-modes button:nth-child(2)").Click();

            Assert.Equal(MarkdownEditorMode.Preview, changed);
        }

        [Fact]
        public void MarkdownEditor_Input_UpdatesValue_AndRaisesValueChanged()
        {
            using var ctx = CreateContext();
            string? value = null;
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.ValueChanged, v => value = v));

            component.Find("textarea").Input("typed");

            Assert.Equal("typed", value);
        }

        [Fact]
        public void MarkdownEditor_Input_RaisesInput_OnlyWhenImmediate()
        {
            using var ctx = CreateContext();
            var count = 0;
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Input, _ => count++));
            component.Find("textarea").Input("a");
            Assert.Equal(0, count);

            component.SetParametersAndRender(p => p.Add(x => x.Immediate, true));
            component.Find("textarea").Input("b");
            Assert.Equal(1, count);
        }

        [Fact]
        public async System.Threading.Tasks.Task MarkdownEditor_ExecuteCommand_AppliesFormatterResult_AndRaisesExecute()
        {
            using var ctx = CreateContext();
            ctx.JSInterop.Setup<int[]?>("Radzen.getSelectionRange", _ => true).SetResult(new[] { 0, 5 });
            var apply = ctx.JSInterop.SetupVoid("Radzen.markdownEditorApply", _ => true);
            apply.SetVoidResult();
            string? executed = null;

            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "hello")
                .Add(x => x.Execute, args => executed = args.CommandName));

            await component.InvokeAsync(() => component.Instance.ExecuteCommandAsync(MarkdownEditorCommands.Bold));

            var invocation = Assert.Single(apply.Invocations);
            Assert.Equal(new object?[] { 0, 5, "**hello**", 2, 7 }, invocation.Arguments.Skip(1).ToArray());
            Assert.Equal("bold", executed);
        }

        [Fact]
        public async System.Threading.Tasks.Task MarkdownEditor_ExecuteCommand_UnknownCommand_RaisesExecuteWithoutApplying()
        {
            using var ctx = CreateContext();
            var apply = ctx.JSInterop.SetupVoid("Radzen.markdownEditorApply", _ => true);
            apply.SetVoidResult();
            string? executed = null;

            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Execute, args => executed = args.CommandName));

            await component.InvokeAsync(() => component.Instance.ExecuteCommandAsync("insertToday"));

            Assert.Empty(apply.Invocations);
            Assert.Equal("insertToday", executed);
        }

        [Fact]
        public void MarkdownEditor_RendersDefaultToolbar_WhenNoChildContent()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>();

            var icons = component.FindAll(".rz-markdown-editor-tools .rzi").Select(i => i.TextContent).ToList();
            Assert.Equal(new[] { "format_bold", "format_italic", "strikethrough_s", "title", "format_quote", "code", "code_blocks",
                                 "format_list_bulleted", "format_list_numbered", "checklist", "link", "image", "horizontal_rule" }, icons);
        }

        [Fact]
        public void MarkdownEditor_RendersOnlyChildContent_WhenProvided()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.AddChildContent<RadzenMarkdownEditorBold>());

            var icons = component.FindAll(".rz-markdown-editor-tools .rzi").Select(i => i.TextContent).ToList();
            Assert.Equal(new[] { "format_bold" }, icons);
        }

        [Fact]
        public void MarkdownEditor_ToolClick_ExecutesCommand()
        {
            using var ctx = CreateContext();
            ctx.JSInterop.Setup<int[]?>("Radzen.getSelectionRange", _ => true).SetResult(new[] { 0, 2 });
            var apply = ctx.JSInterop.SetupVoid("Radzen.markdownEditorApply", _ => true);
            apply.SetVoidResult();

            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "hi")
                .AddChildContent<RadzenMarkdownEditorItalic>());

            component.Find(".rz-markdown-editor-tools button").Click();

            var invocation = Assert.Single(apply.Invocations);
            Assert.Equal("_hi_", invocation.Arguments[3]);
        }

        [Fact]
        public void MarkdownEditor_Tools_AreDisabled_InPreviewMode()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Mode, MarkdownEditorMode.Preview));

            Assert.All(component.FindAll(".rz-markdown-editor-tools button"), b => Assert.True(b.HasAttribute("disabled")));
        }

        [Fact]
        public void MarkdownEditor_Tools_RegisterShortcuts()
        {
            using var ctx = CreateContext();
            ctx.RenderComponent<RadzenMarkdownEditor>();

            // Loose mode records every call; no Setup so the un-awaited OnAfterRenderAsync cannot hang on a missing result.
            var invocation = Assert.Single(ctx.JSInterop.Invocations["Radzen.createMarkdownEditor"]);
            var keys = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<string>>(invocation.Arguments[2]);
            Assert.Equal(new[] { "Ctrl+B", "Ctrl+I", "Ctrl+K" }, keys.OrderBy(k => k));
        }
    }
}

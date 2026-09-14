#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
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
            ctx.Services.AddScoped<ContextMenuService>();
            return ctx;
        }

        static BunitJSModuleInterop Module(TestContext ctx) => ctx.JSInterop.SetupModule(invocation => invocation.Identifier == "Radzen.createMarkdownEditor");

        static MarkdownEditorUpdate LastUpdate(BunitJSModuleInterop module) =>
            Assert.IsType<MarkdownEditorUpdate>(module.Invocations["update"].Last().Arguments[0]);

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
        public void MarkdownEditor_DesignMode_RendersEditableAndHidesTextarea()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Value, "# Hi"));
            var editable = component.Find(".rz-markdown-editor-design");
            Assert.Equal("true", editable.GetAttribute("contenteditable"));
            Assert.Empty(editable.InnerHtml.Trim());
            Assert.True(component.Find("textarea").HasAttribute("hidden"));
        }

        [Fact]
        public void MarkdownEditor_SourceMode_ShowsTextareaAndHidesEditable()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "# Hi")
                .Add(x => x.Mode, MarkdownEditorMode.Source));
            Assert.False(component.Find("textarea").HasAttribute("hidden"));
            Assert.True(component.Find(".rz-markdown-editor-design").HasAttribute("hidden"));
        }

        [Fact]
        public void MarkdownEditor_DisabledRemovesContentEditable()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Disabled, true));
            Assert.Equal("false", component.Find(".rz-markdown-editor-design").GetAttribute("contenteditable"));
        }

        [Fact]
        public void MarkdownEditor_ModeSwitcher_RaisesModeChanged()
        {
            using var ctx = CreateContext();
            MarkdownEditorMode? changed = null;
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.ModeChanged, m => changed = m));
            component.Find(".rz-markdown-editor-tools button[title^='View source']").Click();
            Assert.Equal(MarkdownEditorMode.Source, changed);
        }

        [Fact]
        public async Task ToolbarFollowsTheCaretAcrossSelectionReports()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Value, "# Hi\n\nplain **bold**"));

            await component.InvokeAsync(() => component.Instance.OnSelectionAsync(1, 1));
            Assert.Contains("Heading 1", component.Find(".rz-dropdown-label").TextContent);
            Assert.DoesNotContain("rz-state-active", component.Find("[title^='Bold']").ClassName);

            await component.InvokeAsync(() => component.Instance.OnSelectionAsync(15, 15));
            Assert.Contains("Normal", component.Find(".rz-dropdown-label").TextContent);
            Assert.Contains("rz-state-active", component.Find("[title^='Bold']").ClassName);

            await component.InvokeAsync(() => component.Instance.OnSelectionAsync(2, 2));
            Assert.Contains("Heading 1", component.Find(".rz-dropdown-label").TextContent);
            Assert.DoesNotContain("rz-state-active", component.Find("[title^='Bold']").ClassName);
        }

        [Fact]
        public async Task MarkdownEditor_Edit_UpdatesValue_AndRaisesValueChanged()
        {
            using var ctx = CreateContext();
            string? value = null;
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Mode, MarkdownEditorMode.Source)
                .Add(x => x.ValueChanged, v => value = v));
            var update = await component.InvokeAsync(() => component.Instance.OnEditAsync(0, 0, "typed", "insertText", 1));
            Assert.Equal("typed", value);
            Assert.Equal("typed", component.Instance.Value);
            Assert.Equal(1, update.Version);
            Assert.Null(update.Html);
            Assert.Equal((5, 5), (update.SelectionStart, update.SelectionEnd));
        }

        [Fact]
        public async Task MarkdownEditor_Edit_RaisesInput_OnlyWhenImmediate()
        {
            using var ctx = CreateContext();
            var count = 0;
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Input, _ => count++));
            await component.InvokeAsync(() => component.Instance.OnEditAsync(0, 0, "a", "insertText", 1));
            Assert.Equal(0, count);
            component.SetParametersAndRender(p => p.Add(x => x.Immediate, true));
            await component.InvokeAsync(() => component.Instance.OnEditAsync(1, 1, "b", "insertText", 2));
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task MarkdownEditor_Edit_InDesignMode_InsertsLiteralText_AndRendersIt()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Value, "old"));
            var update = await component.InvokeAsync(() => component.Instance.OnEditAsync(3, 3, " *x*", "insertText", 1));
            Assert.Equal("old \\*x\\*", component.Instance.Value);
            Assert.Equal("<p>old *x*</p>", update.Html);
            Assert.Equal((9, 9), (update.SelectionStart, update.SelectionEnd));
        }

        [Fact]
        public async Task MarkdownEditor_Enter_InAListItem_StartsANewItem()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Value, "- item"));
            var update = await component.InvokeAsync(() => component.Instance.OnInsertParagraphAsync(6, 6, 1));
            Assert.Equal("- item\n- ", component.Instance.Value);
            Assert.Contains("<li>​</li>", update!.Html);
        }

        [Fact]
        public async Task MarkdownEditor_Undo_RevertsTheEdit_AndSendsTheText()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Value, "a"));
            await component.InvokeAsync(() => component.Instance.OnEditAsync(1, 1, "b", "insertText", 1));
            var update = await component.InvokeAsync(() => component.Instance.OnUndoAsync(2));
            Assert.Equal("a", component.Instance.Value);
            Assert.Equal("a", update!.Text);
            Assert.False(component.Instance.CanUndo);
            Assert.True(component.Instance.CanRedo);
        }

        [Fact]
        public async Task MarkdownEditor_ExecuteCommand_AppliesTheFormatter_AndRaisesExecute()
        {
            using var ctx = CreateContext();
            var module = Module(ctx);
            module.Setup<int[]?>("getSelection", _ => true).SetResult(new[] { 0, 5 });
            string? executed = null;
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "hello")
                .Add(x => x.Mode, MarkdownEditorMode.Source)
                .Add(x => x.Execute, args => executed = args.CommandName));
            await component.InvokeAsync(() => component.Instance.ExecuteCommandAsync(MarkdownEditorCommands.Bold));
            var update = LastUpdate(module);
            Assert.Equal("**hello**", update.Text);
            Assert.Equal((2, 7), (update.SelectionStart, update.SelectionEnd));
            Assert.Equal("**hello**", component.Instance.Value);
            Assert.Equal("bold", executed);
        }

        [Fact]
        public async Task MarkdownEditor_ExecuteCommand_UnknownCommand_RaisesExecuteWithoutApplying()
        {
            using var ctx = CreateContext();
            var module = Module(ctx);
            string? executed = null;
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "text")
                .Add(x => x.Execute, args => executed = args.CommandName));
            var count = module.Invocations["update"].Count;
            await component.InvokeAsync(() => component.Instance.ExecuteCommandAsync("insertToday"));
            Assert.Equal(count, module.Invocations["update"].Count);
            Assert.Equal("insertToday", executed);
        }

        [Fact]
        public async Task MarkdownEditor_ExecuteCommand_InDesignMode_SendsTheRenderedHtml()
        {
            using var ctx = CreateContext();
            var module = Module(ctx);
            module.Setup<int[]?>("getSelection", _ => true).SetResult(new[] { 0, 5 });
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Value, "hello"));
            await component.InvokeAsync(() => component.Instance.ExecuteCommandAsync(MarkdownEditorCommands.Bold));
            var update = LastUpdate(module);
            Assert.Equal("<p><strong>hello</strong></p>", update.Html);
            Assert.True(component.Instance.IsActive(MarkdownEditorCommands.Bold));
        }

        [Fact]
        public void MarkdownEditor_ExecuteCommand_Link_ReadsTheSelectionBeforeTheDialogOpens()
        {
            using var ctx = CreateContext();
            var module = Module(ctx);
            var getSelection = module.Setup<int[]?>("getSelection", _ => true);
            getSelection.SetResult(new[] { 0, 2 });
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Value, "hi"));
            _ = component.InvokeAsync(() => component.Instance.ExecuteCommandAsync(MarkdownEditorCommands.Link));
            Assert.Single(getSelection.Invocations);
        }

        [Fact]
        public void MarkdownEditor_RendersDefaultToolbar_WhenNoChildContent()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>();
            var icons = component.FindAll(".rz-markdown-editor-tools > button.rz-button .rzi").Select(i => i.TextContent).ToList();
            Assert.Equal(new[] { "undo", "redo", "format_bold", "format_italic", "strikethrough_s", "format_quote", "code", "code_blocks",
                                 "format_list_bulleted", "format_list_numbered", "checklist", "link", "image", "horizontal_rule",
                                 "table_chart", "north", "south", "west", "east", "horizontal_rule", "vertical_align_center", "delete",
                                 "format_align_left", "format_align_center", "format_align_right", "code" }, icons);
            Assert.Single(component.FindComponents<RadzenMarkdownEditorFormatBlock>());
        }

        [Fact]
        public void MarkdownEditor_RendersOnlyChildContent_WhenProvided()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.AddChildContent<RadzenMarkdownEditorBold>());
            var icons = component.FindAll(".rz-markdown-editor-tools > button .rzi").Select(i => i.TextContent).ToList();
            Assert.Equal(new[] { "format_bold" }, icons);
        }

        [Fact]
        public void MarkdownEditor_ToolClick_ExecutesCommand()
        {
            using var ctx = CreateContext();
            var module = Module(ctx);
            module.Setup<int[]?>("getSelection", _ => true).SetResult(new[] { 0, 2 });
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "hi")
                .Add(x => x.Mode, MarkdownEditorMode.Source)
                .AddChildContent<RadzenMarkdownEditorItalic>());
            component.Find(".rz-markdown-editor-tools > button").Click();
            Assert.Equal("*hi*", LastUpdate(module).Text);
        }

        [Fact]
        public void MarkdownEditor_Disabled_DisablesTextarea_AndAllTools()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Disabled, true));
            Assert.True(component.Find("textarea").HasAttribute("disabled"));
            Assert.All(component.FindAll(".rz-markdown-editor-tools button"), b => Assert.True(b.HasAttribute("disabled")));
        }

        [Fact]
        public void MarkdownEditor_Visible_False_DoesNotInitialize_ThenTrue_Initializes()
        {
            using var ctx = CreateContext();
            var module = Module(ctx);
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Visible, false));
            Assert.DoesNotContain("rz-markdown-editor", component.Markup);
            ctx.JSInterop.VerifyNotInvoke("Radzen.createMarkdownEditor");
            component.SetParametersAndRender(p => p.Add(x => x.Visible, true));
            ctx.JSInterop.VerifyInvoke("Radzen.createMarkdownEditor");
        }

        [Fact]
        public void MarkdownEditor_ModeSwitcher_UpdatesInternalMode_WhenOneWayBound()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "# Hi")
                .Add(x => x.Mode, MarkdownEditorMode.Design));
            component.Find(".rz-markdown-editor-tools button[title^='View source']").Click();
            Assert.False(component.Find("textarea").HasAttribute("hidden"));
            Assert.True(component.Find(".rz-markdown-editor-design").HasAttribute("hidden"));
        }

        [Fact]
        public async Task MarkdownEditor_ExecuteShortcutAsync_AppliesRegisteredShortcut_AndIgnoresUnknownKey()
        {
            using var ctx = CreateContext();
            var module = Module(ctx);
            module.Setup<int[]?>("getSelection", _ => true).SetResult(new[] { 0, 2 });
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "hi")
                .Add(x => x.Mode, MarkdownEditorMode.Source)
                .AddChildContent<RadzenMarkdownEditorBold>());
            var count = module.Invocations["update"].Count;
            await component.InvokeAsync(() => component.Instance.ExecuteShortcutAsync("Ctrl+B"));
            Assert.Equal("**hi**", LastUpdate(module).Text);
            Assert.Equal(count + 1, module.Invocations["update"].Count);
            await component.InvokeAsync(() => component.Instance.ExecuteShortcutAsync("Ctrl+Q"));
            Assert.Equal(count + 1, module.Invocations["update"].Count);
        }

        [Fact]
        public async Task MarkdownEditor_ExecuteCommand_NormalizesCrlfInTheValue()
        {
            using var ctx = CreateContext();
            var module = Module(ctx);
            module.Setup<int[]?>("getSelection", _ => true).SetResult(new[] { 6, 11 });
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "line1\r\nline2")
                .Add(x => x.Mode, MarkdownEditorMode.Source));
            await component.InvokeAsync(() => component.Instance.ExecuteCommandAsync(MarkdownEditorCommands.Bold));
            Assert.Equal("line1\n**line2**", LastUpdate(module).Text);
        }

        [Fact]
        public void MarkdownEditor_Tools_RegisterShortcuts()
        {
            using var ctx = CreateContext();
            ctx.RenderComponent<RadzenMarkdownEditor>();
            var invocation = Assert.Single(ctx.JSInterop.Invocations["Radzen.createMarkdownEditor"]);
            var keys = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<string>>(invocation.Arguments[3]);
            Assert.Equal(new[] { "Ctrl+B", "Ctrl+I", "Ctrl+K" }, keys.OrderBy(k => k));
        }

        [Fact]
        public void CustomTool_Click_RaisesExecute_WithCommandName()
        {
            using var ctx = CreateContext();
            var module = Module(ctx);
            string? executed = null;
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Mode, MarkdownEditorMode.Source)
                .Add(x => x.Execute, args => executed = args.CommandName)
                .AddChildContent<RadzenMarkdownEditorCustomTool>(t => t
                    .Add(x => x.CommandName, "InsertToday")
                    .Add(x => x.Icon, "today")));
            var count = module.Invocations["update"].Count;
            component.Find(".rz-markdown-editor-tools > button").Click();
            Assert.Equal("InsertToday", executed);
            Assert.Equal(count, module.Invocations["update"].Count);
        }

        [Fact]
        public void CustomTool_RendersTemplate_WithEditor()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .AddChildContent<RadzenMarkdownEditorCustomTool>(t => t
                    .Add(x => x.Template, editor => b => b.AddContent(0, $"mode:{editor.Mode}"))));
            Assert.Contains("rz-markdown-editor-custom-tool", component.Markup);
            Assert.Contains("mode:Design", component.Markup);
        }

        [Fact]
        public void MarkdownEditor_ProgrammaticValueChange_SendsTheNewContent()
        {
            using var ctx = CreateContext();
            var module = Module(ctx);
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Value, "a"));
            var count = module.Invocations["update"].Count;
            component.SetParametersAndRender(p => p.Add(x => x.Value, "b"));
            Assert.Equal(count + 1, module.Invocations["update"].Count);
            var update = LastUpdate(module);
            Assert.Equal("b", update.Text);
            Assert.Equal("<p>b</p>", update.Html);
        }

        [Fact]
        public async Task MarkdownEditor_ValueEchoedByTheParent_DoesNotResendTheContent()
        {
            using var ctx = CreateContext();
            var module = Module(ctx);
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Value, "a"));
            await component.InvokeAsync(() => component.Instance.OnEditAsync(1, 1, "b", "insertText", 1));
            var count = module.Invocations["update"].Count;
            component.SetParametersAndRender(p => p.Add(x => x.Value, "ab"));
            Assert.Equal(count, module.Invocations["update"].Count);
        }

        [Fact]
        public void MarkdownEditor_ProgrammaticModeChange_SendsTheContent()
        {
            using var ctx = CreateContext();
            var module = Module(ctx);
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "a")
                .Add(x => x.Mode, MarkdownEditorMode.Source));
            var count = module.Invocations["update"].Count;
            component.SetParametersAndRender(p => p.Add(x => x.Mode, MarkdownEditorMode.Design));
            Assert.Equal(count + 1, module.Invocations["update"].Count);
            Assert.Equal("<p>a</p>", LastUpdate(module).Html);
        }

        [Fact]
        public async Task MarkdownEditor_Blur_RaisesChange_OnlyWhenTheTextChanged()
        {
            using var ctx = CreateContext();
            string? changed = null;
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "old")
                .Add(x => x.Change, v => changed = v));
            await component.InvokeAsync(() => component.Instance.OnFocusAsync());
            await component.InvokeAsync(() => component.Instance.OnBlurAsync());
            Assert.Null(changed);
            await component.InvokeAsync(() => component.Instance.OnFocusAsync());
            await component.InvokeAsync(() => component.Instance.OnEditAsync(3, 3, "!", "insertText", 1));
            await component.InvokeAsync(() => component.Instance.OnBlurAsync());
            Assert.Equal("old!", changed);
        }

        [Fact]
        public async Task MarkdownEditor_Selection_HighlightsActiveFormats()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Value, "**bold** plain"));
            await component.InvokeAsync(() => component.Instance.OnSelectionAsync(3, 3));
            Assert.True(component.Instance.IsActive(MarkdownEditorCommands.Bold));
            Assert.Contains("rz-state-active", component.Markup);
            await component.InvokeAsync(() => component.Instance.OnSelectionAsync(10, 10));
            Assert.False(component.Instance.IsActive(MarkdownEditorCommands.Bold));
        }

        [Fact]
        public async Task MarkdownEditor_Selection_ShowsTheCurrentBlockInTheFormatBlockTool()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Value, "## title"));
            await component.InvokeAsync(() => component.Instance.OnSelectionAsync(4, 4));
            Assert.Equal("h2", component.Instance.FormatBlock);
            Assert.Equal("Heading 2", component.Find(".rz-dropdown-label").TextContent);
        }

        [Fact]
        public async Task MarkdownEditor_Selection_DisablesTheFormatBlockTool_WithoutParagraphOrHeading()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Value, "para\n\n```\ncode\n```"));
            await component.InvokeAsync(() => component.Instance.OnSelectionAsync(2, 2));
            Assert.DoesNotContain("rz-state-disabled", component.Find(".rz-dropdown").ClassName);
            await component.InvokeAsync(() => component.Instance.OnSelectionAsync(11, 11));
            Assert.Contains("rz-state-disabled", component.Find(".rz-dropdown").ClassName);
        }

        [Fact]
        public void FormatBlock_Change_ExecutesFormatBlockWithTheLevel()
        {
            using var ctx = CreateContext();
            var module = Module(ctx);
            module.Setup<int[]?>("getSelection", _ => true).SetResult(new[] { 0, 0 });
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(e => e.Value, "title").Add(e => e.Mode, MarkdownEditorMode.Source));
            component.FindComponent<RadzenMarkdownEditorFormatBlock>().FindAll(".rz-dropdown-item")[3].Click();
            Assert.Equal("### title", LastUpdate(module).Text);
        }

        [Fact]
        public async Task TableToolsFollowTheCaretIntoAndOutOfATable()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "text\n\n| a | b |\n| - | -: |\n| c | d |")
                .AddChildContent<RadzenMarkdownEditorTableTools>());
            var buttons = component.FindAll(".rz-markdown-editor-tools > button");
            Assert.Equal(11, buttons.Count);
            Assert.False(buttons[0].HasAttribute("disabled"));
            Assert.All(buttons.Skip(1), b => Assert.True(b.HasAttribute("disabled")));

            await component.InvokeAsync(() => component.Instance.OnSelectionAsync(8, 8));
            buttons = component.FindAll(".rz-markdown-editor-tools > button");
            Assert.True(component.Instance.InTable);
            Assert.Equal(0, component.Instance.TableRow);
            Assert.True(buttons[5].HasAttribute("disabled"));
            Assert.All(buttons.Where((b, i) => i != 5), b => Assert.False(b.HasAttribute("disabled")));

            await component.InvokeAsync(() => component.Instance.OnSelectionAsync(33, 33));
            buttons = component.FindAll(".rz-markdown-editor-tools > button");
            Assert.Equal(1, component.Instance.TableRow);
            Assert.Equal("right", component.Instance.TableAlignment);
            Assert.False(buttons[5].HasAttribute("disabled"));
            Assert.Contains("rz-state-active", buttons[10].ClassName);
            Assert.DoesNotContain("rz-state-active", buttons[8].ClassName);
        }

        [Fact]
        public void TableCommandButtonExecutesItsCommandWithItsValue()
        {
            using var ctx = CreateContext();
            var module = Module(ctx);
            module.Setup<int[]?>("getSelection", _ => true).SetResult(new[] { 2, 2 });
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "| a | b |\n| - | - |")
                .Add(x => x.Mode, MarkdownEditorMode.Source)
                .AddChildContent<RadzenMarkdownEditorTableCommandButton>(b => b
                    .Add(x => x.TableCommand, MarkdownEditorCommands.TableAlign)
                    .Add(x => x.Value, "center")));
            component.InvokeAsync(() => component.Instance.OnSelectionAsync(2, 2)).GetAwaiter().GetResult();
            component.Find(".rz-markdown-editor-tools > button").Click();
            Assert.Equal("| a | b |\n| :-: | --- |", LastUpdate(module).Text);
        }

        [Fact]
        public void InsertTableWithAValueSkipsTheDialog()
        {
            using var ctx = CreateContext();
            var module = Module(ctx);
            module.Setup<int[]?>("getSelection", _ => true).SetResult(new[] { 0, 0 });
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Mode, MarkdownEditorMode.Source));
            component.InvokeAsync(() => component.Instance.ExecuteCommandAsync(MarkdownEditorCommands.InsertTable, "1x2")).GetAwaiter().GetResult();
            Assert.Equal("|  |  |\n| --- | --- |", LastUpdate(module).Text);
        }

        [Fact]
        public async Task MarkdownEditor_ContextMenu_InsideATable_OpensTheTableCommands()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Value, "| a | b |\n| --- | --- |\n| 1 | 2 |"));
            var service = ctx.Services.GetRequiredService<ContextMenuService>();
            ContextMenuOptions? opened = null;
            service.OnOpen += (_, options) => opened = options;

            await component.InvokeAsync(() => component.Instance.OnContextMenuAsync(10, 20, 2, 2));

            Assert.NotNull(opened);
            var items = opened!.Items!.ToList();
            Assert.Equal(10, items.Count);
            Assert.Contains(items, item => item.Value is ValueTuple<string, string?> { Item1: MarkdownEditorCommands.TableDeleteRow, Item2: null } && item.Disabled);
            Assert.Contains(items, item => item.Value is ValueTuple<string, string?> { Item1: MarkdownEditorCommands.TableAlign, Item2: "center" });
        }

        [Fact]
        public async Task MarkdownEditor_ContextMenu_OutsideATable_DoesNotOpen()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Value, "plain"));
            var service = ctx.Services.GetRequiredService<ContextMenuService>();
            var opened = false;
            service.OnOpen += (_, _) => opened = true;

            await component.InvokeAsync(() => component.Instance.OnContextMenuAsync(10, 20, 1, 1));

            Assert.False(opened);
        }
    }
}

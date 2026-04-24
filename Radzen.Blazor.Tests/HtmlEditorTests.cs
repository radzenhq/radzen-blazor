using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class HtmlEditorTests
    {
        [Fact]
        public void HtmlEditor_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.Services.AddScoped<ContextMenuService>();
            ctx.Services.AddScoped<NotificationService>();
            var component = ctx.RenderComponent<RadzenHtmlEditor>();

            Assert.Contains(@"rz-html-editor", component.Markup);
        }

        [Fact]
        public void HtmlEditor_Renders_ShowToolbar_True()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.Services.AddScoped<ContextMenuService>();
            ctx.Services.AddScoped<NotificationService>();
            var component = ctx.RenderComponent<RadzenHtmlEditor>(parameters =>
            {
                parameters.Add(p => p.ShowToolbar, true);
            });

            Assert.Contains("rz-html-editor-toolbar", component.Markup);
        }

        [Fact]
        public void HtmlEditor_Renders_ShowToolbar_False()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.Services.AddScoped<ContextMenuService>();
            ctx.Services.AddScoped<NotificationService>();
            var component = ctx.RenderComponent<RadzenHtmlEditor>(parameters =>
            {
                parameters.Add(p => p.ShowToolbar, false);
            });

            Assert.DoesNotContain("rz-html-editor-toolbar", component.Markup);
        }

        [Fact]
        public void HtmlEditor_Renders_Mode_Design()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.Services.AddScoped<ContextMenuService>();
            ctx.Services.AddScoped<NotificationService>();
            var component = ctx.RenderComponent<RadzenHtmlEditor>(parameters =>
            {
                parameters.Add(p => p.Mode, HtmlEditorMode.Design);
            });

            // Design mode shows the content editable div
            Assert.Contains("contenteditable", component.Markup);
        }

        [Fact]
        public void HtmlEditor_Renders_Mode_Source()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.Services.AddScoped<ContextMenuService>();
            ctx.Services.AddScoped<NotificationService>();
            var component = ctx.RenderComponent<RadzenHtmlEditor>(parameters =>
            {
                parameters.Add(p => p.Mode, HtmlEditorMode.Source);
            });

            // Source mode shows the textarea for HTML editing
            Assert.Contains("rz-html-editor-source", component.Markup);
        }

        [Fact]
        public void HtmlEditor_Renders_Disabled_Attribute()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.Services.AddScoped<ContextMenuService>();
            ctx.Services.AddScoped<NotificationService>();
            var component = ctx.RenderComponent<RadzenHtmlEditor>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
            });

            Assert.Contains("disabled", component.Markup);
        }

        [Fact]
        public void HtmlEditor_Renders_ContentArea()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.Services.AddScoped<ContextMenuService>();
            ctx.Services.AddScoped<NotificationService>();
            var component = ctx.RenderComponent<RadzenHtmlEditor>();

            Assert.Contains("rz-html-editor-content", component.Markup);
        }

        [Fact]
        public void HtmlEditor_DefaultToolbar_Renders_TableTools()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.Services.AddScoped<ContextMenuService>();
            ctx.Services.AddScoped<NotificationService>();

            var component = ctx.RenderComponent<RadzenHtmlEditor>();

            Assert.Contains("table_chart", component.Markup);
            Assert.Contains("merge_type", component.Markup);
            Assert.Contains("call_split", component.Markup);
        }

        [Fact]
        public void HtmlEditorTable_Renders_PropertyPanelLabels()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();

            var component = ctx.RenderComponent<RadzenHtmlEditorTable>();

            Assert.Equal("Insert table", component.Instance.Title);
            Assert.Equal("Column width (px)", component.Instance.ColumnWidthPxText);
            Assert.Equal("Border color", component.Instance.BorderColorText);
            Assert.Equal("Top", component.Instance.BorderTopText);
        }

        [Fact]
        public void HtmlEditorTable_Renders_BorderSideDefaults()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();

            var component = ctx.RenderComponent<RadzenHtmlEditorTable>();

            Assert.Equal("Right", component.Instance.BorderRightText);
            Assert.Equal("Bottom", component.Instance.BorderBottomText);
            Assert.Equal("Left", component.Instance.BorderLeftText);
            Assert.Equal("Border style", component.Instance.BorderStyleText);
        }

        [Fact]
        public void HtmlEditorTableCommandButton_Disabled_WithoutTableSelection()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.Services.AddScoped<ContextMenuService>();
            ctx.Services.AddScoped<NotificationService>();

            var component = ctx.RenderComponent<RadzenHtmlEditor>(parameters => parameters
                .AddChildContent<RadzenHtmlEditorTableCommandButton>(childParameters => childParameters
                    .Add(p => p.TableCommand, "mergeCellRight")
                    .Add(p => p.Icon, "merge_type")
                    .Add(p => p.Title, "Merge right")));

            var button = component.Find("button");

            Assert.True(button.HasAttribute("disabled"));
        }

        [Fact]
        public void HtmlEditorCommandState_Defaults_ToSuccessfulResult()
        {
            var state = new RadzenHtmlEditorCommandState();

            Assert.True(state.Success);
            Assert.Null(state.Message);
        }

        [Fact]
        public async Task HtmlEditor_ExecuteTableCommandAsync_UsesLocalizedBlockedMessage()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.Services.AddScoped<ContextMenuService>();
            ctx.Services.AddScoped<NotificationService>();

            ctx.JSInterop.Setup<RadzenHtmlEditorCommandState>("Radzen.execTableCommand", _ => true)
                .SetResult(new RadzenHtmlEditorCommandState
                {
                    Success = false,
                    Message = "table.paste.blocked"
                });

            ctx.JSInterop.Setup<HtmlEditorTableSelection>("Radzen.getTableSelection", _ => true)
                .SetResult(new HtmlEditorTableSelection());

            var component = ctx.RenderComponent<RadzenHtmlEditor>(parameters => parameters
                .Add(p => p.TableActionBlockedText, "Aktion blockiert")
                .Add(p => p.TablePasteBlockedText, "Einfügen ist für diese Zellstruktur nicht möglich."));

            await component.InvokeAsync(() => component.Instance.ExecuteTableCommandAsync("pasteCells"));

            var notificationService = ctx.Services.GetRequiredService<NotificationService>();
            var message = Assert.Single(notificationService.Messages);
            Assert.Equal(NotificationSeverity.Warning, message.Severity);
            Assert.Equal("Aktion blockiert", message.Summary);
            Assert.Equal("Einfügen ist für diese Zellstruktur nicht möglich.", message.Detail);
        }

        [Fact]
        public void HtmlEditorTable_UsesEditorTableTextFallbacks()
        {
            var editor = new RadzenHtmlEditor
            {
                TableDialogTitleText = "Tabelle einfügen",
                TableRowsText = "Zeilen",
                TableColumnsText = "Spalten",
                TableWidthText = "Breite",
                TableBorderText = "Rahmen",
                TableHeaderRowText = "Kopfzeile einschließen",
                TableEditText = "Tabelle bearbeiten",
                TableOkText = "Einfügen",
                TableUpdateText = "Aktualisieren",
                TableCancelText = "Abbrechen",
                TableInsertRowAboveText = "Zeile oben einfügen",
                TableInsertColumnLeftText = "Spalte links einfügen",
                TableColumnWidthText = "Spaltenbreite",
                TableCellBackgroundText = "Zellhintergrund",
                TableCellPaddingText = "Zellenabstand",
                TableCellTextAlignText = "Horizontal",
                TableCellVerticalAlignText = "Vertikal",
                TableCellBorderText = "Zellrahmen",
                TableColumnWidthPxText = "Spaltenbreite (px)",
                TableCellPaddingPxText = "Zellenabstand (px)",
                TableBorderStyleText = "Rahmenstil",
                TableBorderWidthPxText = "Rahmenbreite (px)",
                TableBorderColorText = "Rahmenfarbe",
                TableBorderTopText = "Oben",
                TableBorderRightText = "Rechts",
                TableBorderBottomText = "Unten",
                TableBorderLeftText = "Links",
                TableDeleteText = "Tabelle entfernen",
                TableMergeRightText = "Nach rechts verbinden"
            };

            var component = new RadzenHtmlEditorTable
            {
                Editor = editor
            };

            Assert.Equal("Tabelle einfügen", component.Title);
            Assert.Equal("Zeilen", component.RowsText);
            Assert.Equal("Spalten", component.ColumnsText);
            Assert.Equal("Breite", component.WidthText);
            Assert.Equal("Rahmen", component.BorderText);
            Assert.Equal("Kopfzeile einschließen", component.HeaderRowText);
            Assert.Equal("Tabelle bearbeiten", component.EditText);
            Assert.Equal("Einfügen", component.OkText);
            Assert.Equal("Aktualisieren", component.UpdateText);
            Assert.Equal("Abbrechen", component.CancelText);
            Assert.Equal("Zeile oben einfügen", component.InsertRowAboveText);
            Assert.Equal("Spalte links einfügen", component.InsertColumnLeftText);
            Assert.Equal("Spaltenbreite", component.ColumnWidthText);
            Assert.Equal("Zellhintergrund", component.CellBackgroundText);
            Assert.Equal("Zellenabstand", component.CellPaddingText);
            Assert.Equal("Horizontal", component.CellTextAlignText);
            Assert.Equal("Vertikal", component.CellVerticalAlignText);
            Assert.Equal("Zellrahmen", component.CellBorderText);
            Assert.Equal("Spaltenbreite (px)", component.ColumnWidthPxText);
            Assert.Equal("Zellenabstand (px)", component.CellPaddingPxText);
            Assert.Equal("Rahmenstil", component.BorderStyleText);
            Assert.Equal("Rahmenbreite (px)", component.BorderWidthPxText);
            Assert.Equal("Rahmenfarbe", component.BorderColorText);
            Assert.Equal("Oben", component.BorderTopText);
            Assert.Equal("Rechts", component.BorderRightText);
            Assert.Equal("Unten", component.BorderBottomText);
            Assert.Equal("Links", component.BorderLeftText);
            Assert.Equal("Tabelle entfernen", component.DeleteTableText);
            Assert.Equal("Nach rechts verbinden", component.MergeRightText);
        }
    }
}


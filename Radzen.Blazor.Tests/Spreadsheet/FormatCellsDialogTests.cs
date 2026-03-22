using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class FormatCellsDialogTests : TestContext
{
    public FormatCellsDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DialogService>();
    }

    private IRenderedComponent<FormatCellsDialog> RenderDialog(
        object sampleValue = null, CellDataType valueType = CellDataType.Number, string currentFormat = null)
    {
        return RenderComponent<FormatCellsDialog>(parameters => parameters
            .Add(p => p.SampleValue, sampleValue ?? 1234.5)
            .Add(p => p.ValueType, valueType)
            .Add(p => p.CurrentFormat, currentFormat));
    }

    [Fact]
    public void Dialog_Renders_CategoryList()
    {
        var cut = RenderDialog();
        // The dialog should contain a listbox for categories
        var listbox = cut.Find(".rz-listbox");
        Assert.NotNull(listbox);
    }

    [Fact]
    public void Dialog_Renders_OkCancelButtons()
    {
        var cut = RenderDialog();
        var markup = cut.Markup;
        Assert.Contains("OK", markup);
        Assert.Contains("Cancel", markup);
    }

    [Fact]
    public void Dialog_Renders_FormatCodeLabel()
    {
        var cut = RenderDialog();
        var markup = cut.Markup;
        Assert.Contains("Format code", markup);
    }

    [Fact]
    public void Dialog_Renders_SampleLabel()
    {
        var cut = RenderDialog();
        var markup = cut.Markup;
        Assert.Contains("Sample", markup);
    }

    [Fact]
    public void Dialog_Preview_ShowsFormattedValue()
    {
        var cut = RenderDialog(sampleValue: 1234.5, currentFormat: "$#,##0.00");
        var markup = cut.Markup;
        Assert.Contains("$1,234.50", markup);
    }

    [Fact]
    public void Dialog_Preview_ShowsGeneralValue()
    {
        var cut = RenderDialog(sampleValue: 1234.5);
        var markup = cut.Markup;
        // Preview shows the sample value formatted using ToString()
        Assert.Contains(1234.5.ToString(), markup);
    }
}

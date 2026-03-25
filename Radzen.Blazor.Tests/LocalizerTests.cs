using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Radzen.Blazor;
using Xunit;

namespace Radzen.Blazor.Tests;

public class LocalizerTests
{
    [Fact]
    public void Localizer_Returns_Resx_Default()
    {
        var result = Localizer.Default.Get(nameof(RadzenStrings.Spreadsheet_OK), CultureInfo.InvariantCulture);

        Assert.Equal("OK", result);
    }

    [Fact]
    public void Localizer_Returns_Key_When_Not_Found()
    {
        var result = Localizer.Default.Get("NonExistent_Key", CultureInfo.InvariantCulture);

        Assert.Equal("NonExistent_Key", result);
    }

    [Fact]
    public void Localizer_Custom_Override_Takes_Precedence()
    {
        var custom = new TestLocalizer("Spreadsheet_OK", "Aceptar");
        var localizer = new Localizer(custom);

        var result = localizer.Get(nameof(RadzenStrings.Spreadsheet_OK), CultureInfo.InvariantCulture);

        Assert.Equal("Aceptar", result);
    }

    [Fact]
    public void Localizer_Custom_Returns_Null_Falls_Back_To_Resx()
    {
        var custom = new TestLocalizer("other_key", "other_value");
        var localizer = new Localizer(custom);

        var result = localizer.Get(nameof(RadzenStrings.Spreadsheet_OK), CultureInfo.InvariantCulture);

        Assert.Equal("OK", result);
    }

    [Fact]
    public void Localizer_Returns_Resx_Default_For_Various_Keys()
    {
        var localizer = Localizer.Default;

        Assert.Equal("Sort ascending", localizer.Get(nameof(RadzenStrings.Spreadsheet_SortAscending), CultureInfo.InvariantCulture));
        Assert.Equal("Cancel", localizer.Get(nameof(RadzenStrings.Spreadsheet_Cancel), CultureInfo.InvariantCulture));
        Assert.Equal("contains", localizer.Get(nameof(RadzenStrings.Spreadsheet_FilterContains), CultureInfo.InvariantCulture));
        Assert.Equal("Format Cells...", localizer.Get(nameof(RadzenStrings.Spreadsheet_FormatCells), CultureInfo.InvariantCulture));
    }

    [Fact]
    public void AddRadzenComponents_Registers_Localizer()
    {
        var services = new ServiceCollection();
        services.AddRadzenComponents();
        var provider = services.BuildServiceProvider();

        var localizer = provider.GetService<Localizer>();

        Assert.NotNull(localizer);
    }

    [Fact]
    public void Localizer_Works_Without_ILocalizer_Registered()
    {
        var services = new ServiceCollection();
        services.AddRadzenComponents();
        var provider = services.BuildServiceProvider();

        var localizer = provider.GetRequiredService<Localizer>();
        var result = localizer.Get(nameof(RadzenStrings.Spreadsheet_OK), CultureInfo.InvariantCulture);

        Assert.Equal("OK", result);
    }

    [Fact]
    public void Localizer_Uses_Custom_ILocalizer_When_Registered()
    {
        var services = new ServiceCollection();
        services.AddRadzenComponents();
        services.AddSingleton<ILocalizer>(new TestLocalizer("Spreadsheet_OK", "Aceptar"));
        var provider = services.BuildServiceProvider();

        var localizer = provider.GetRequiredService<Localizer>();
        var result = localizer.Get(nameof(RadzenStrings.Spreadsheet_OK), CultureInfo.InvariantCulture);

        Assert.Equal("Aceptar", result);
    }

    [Fact]
    public void Localizer_Format_String_Key_Contains_Placeholder()
    {
        var localizer = Localizer.Default;
        var template = localizer.Get(nameof(RadzenStrings.Spreadsheet_SheetNameAlreadyExists), CultureInfo.InvariantCulture);
        var result = string.Format(template, "Sheet2");

        Assert.Equal("A sheet named 'Sheet2' already exists.", result);
    }

    private class TestLocalizer : ILocalizer
    {
        private readonly string key;
        private readonly string value;

        public TestLocalizer(string key, string value)
        {
            this.key = key;
            this.value = value;
        }

        public string? Get(string key, CultureInfo culture)
        {
            return key == this.key ? value : null;
        }
    }
}

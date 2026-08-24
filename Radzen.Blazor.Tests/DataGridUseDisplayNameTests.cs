using Bunit;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridUseDisplayNameTests
    {
        public class DisplayNameResources
        {
            public static string NameKey => "Localized Name";
            public static string ShortNameKey => "Localized Short Name";
        }

        public class DisplayNameModel
        {
            [Display(Name = "NameKey", ResourceType = typeof(DisplayNameResources))]
            public int LocalizedName { get; set; }

            [Display(Name = "Plain Name")]
            public int PlainName { get; set; }

            [Display(ShortName = "ShortNameKey", Name = "NameKey", ResourceType = typeof(DisplayNameResources))]
            public int ShortNameAndName { get; set; }

            [Display(ShortName = "Plain Short Name")]
            public int ShortNameOnly { get; set; }

            public int NoAttribute { get; set; }
        }

        static IRenderedComponent<RadzenDataGrid<DisplayNameModel>> Render(TestContext ctx, string property)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            return ctx.RenderComponent<RadzenDataGrid<DisplayNameModel>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<DisplayNameModel>>(p => p.Data, new[] { new DisplayNameModel() });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<DisplayNameModel>));
                    builder.AddAttribute(1, "Property", property);
                    builder.AddAttribute(2, "UseDisplayName", true);
                    builder.CloseComponent();
                });
            });
        }

        static string HeaderText(IRenderedComponent<RadzenDataGrid<DisplayNameModel>> component)
        {
            return component.Find(".rz-column-title-content").TextContent;
        }

        [Fact]
        public void DataGrid_UseDisplayName_ResolvesLocalizedNameFromResourceType()
        {
            using var ctx = new TestContext();

            var component = Render(ctx, nameof(DisplayNameModel.LocalizedName));

            Assert.Equal("Localized Name", HeaderText(component));
        }

        [Fact]
        public void DataGrid_UseDisplayName_UsesPlainName()
        {
            using var ctx = new TestContext();

            var component = Render(ctx, nameof(DisplayNameModel.PlainName));

            Assert.Equal("Plain Name", HeaderText(component));
        }

        [Fact]
        public void DataGrid_UseDisplayName_PrefersShortNameOverName()
        {
            using var ctx = new TestContext();

            var component = Render(ctx, nameof(DisplayNameModel.ShortNameAndName));

            Assert.Equal("Localized Short Name", HeaderText(component));
        }

        [Fact]
        public void DataGrid_UseDisplayName_UsesShortNameWithoutName()
        {
            using var ctx = new TestContext();

            var component = Render(ctx, nameof(DisplayNameModel.ShortNameOnly));

            Assert.Equal("Plain Short Name", HeaderText(component));
        }

        [Fact]
        public void DataGrid_UseDisplayName_LeavesTitleEmptyWithoutDisplayAttribute()
        {
            using var ctx = new TestContext();

            var component = Render(ctx, nameof(DisplayNameModel.NoAttribute));

            Assert.Equal("", HeaderText(component));
        }
    }
}

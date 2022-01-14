using Bunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DropDownDataGridTests
    {
        class TestModel
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string LastName { get; set; }
            public int Age { get; set; }
        }
        [Fact]
        public void RadzenDataGrid_Search_CaseInsensitive_DiacriticsInsensitive()
        {
            using var ctx = new TestContext();
            IRenderedComponent<RadzenDropDownDataGrid<TestModel>> component = _initiateComponent(ctx);

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.FilterCaseSensitivity, FilterCaseSensitivity.CaseInsensitive);
                parameters.Add(p => p.FilterDiacriticsSensitivity, FilterDiacriticsSensitivity.DiacriticsInsensitive);
            });
            _invokeSearch(ctx, component, "οραρ");

            Assert.Equal(2, component.Instance.View.Cast<TestModel>().ToList().Count());
        }

        private static void _invokeSearch(TestContext ctx, IRenderedComponent<RadzenDropDownDataGrid<TestModel>> component,string searchText)
        {
            var comp = component.Find(".rz-lookup-search input");
            ctx.JSInterop.Setup<String>("Radzen.getInputValue", _ => true).SetResult(searchText);
            comp.Change(searchText);
        }

        [Fact]
        public void RadzenDataGrid_Search_CaseInsensitive_SymbolsInsensitive_DiacriticsInsensitive()
        {
            using var ctx = new TestContext();
            IRenderedComponent<RadzenDropDownDataGrid<TestModel>> component = _initiateComponent(ctx);

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.FilterCaseSensitivity, FilterCaseSensitivity.CaseInsensitive);
                parameters.Add(p => p.FilterDiacriticsSensitivity, FilterDiacriticsSensitivity.DiacriticsInsensitive);
                parameters.Add(p => p.FilterSymbolsSensitivity, FilterSymbolsSensitivity.SymbolInsensitive);
            });

            _invokeSearch(ctx, component, "οραρ");


            Assert.Equal(3, component.Instance.View.Cast<TestModel>().ToList().Count());
        }
        [Fact]
        public void RadzenDataGrid_Search_Sensitive()
        {
            using var ctx = new TestContext();
            IRenderedComponent<RadzenDropDownDataGrid<TestModel>> component = _initiateComponent(ctx);

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.FilterCaseSensitivity, FilterCaseSensitivity.Default);
                parameters.Add(p => p.FilterDiacriticsSensitivity, FilterDiacriticsSensitivity.Default);
                parameters.Add(p => p.FilterSymbolsSensitivity, FilterSymbolsSensitivity.Default);
            });

            _invokeSearch(ctx, component, "οραρ");


            Assert.Single(component.Instance.View.Cast<TestModel>().ToList());
        }
        [Fact]
        public void RadzenDataGrid_Search_DiacriticsInSensitive_EndsWith()
        {
            using var ctx = new TestContext();
            IRenderedComponent<RadzenDropDownDataGrid<TestModel>> component = _initiateComponent(ctx);

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.FilterOperator, StringFilterOperator.EndsWith);
                parameters.Add(p => p.FilterDiacriticsSensitivity, FilterDiacriticsSensitivity.Default);
             });

            _invokeSearch(ctx, component, "άρη");


            Assert.Single(component.Instance.View.Cast<TestModel>().ToList());
        }
        [Fact]
        public void RadzenDataGrid_Search_SymbolInSensitive_StartsWith_AllStringProperties()
        {
            using var ctx = new TestContext();
            IRenderedComponent<RadzenDropDownDataGrid<TestModel>> component = _initiateComponent(ctx);

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.FilterOperator, StringFilterOperator.StartsWith);
                parameters.Add(p => p.FilterSymbolsSensitivity, FilterSymbolsSensitivity.SymbolInsensitive);
                parameters.Add(p => p.AllowFilteringByAllStringColumns, true);
            });

            _invokeSearch(ctx, component, "Λητ");


            Assert.Single(component.Instance.View.Cast<TestModel>().ToList());
        }
        [Fact]
        public void RadzenDataGrid_Search_SymbolInSensitive_EndsWith_AllStringProperties()
        {
            using var ctx = new TestContext();
            IRenderedComponent<RadzenDropDownDataGrid<TestModel>> component = _initiateComponent(ctx);

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.FilterOperator, StringFilterOperator.EndsWith);
                parameters.Add(p => p.FilterSymbolsSensitivity, FilterSymbolsSensitivity.SymbolInsensitive);
                parameters.Add(p => p.AllowFilteringByAllStringColumns, true);
            });

            _invokeSearch(ctx, component, "ητώ");


            Assert.Single(component.Instance.View.Cast<TestModel>().ToList());
        }
        private static IRenderedComponent<RadzenDropDownDataGrid<TestModel>> _initiateComponent(TestContext ctx)
        {
            List<TestModel> testModels = new List<TestModel>();
            testModels.Add(new TestModel { Id = Guid.NewGuid(), Age = 7, LastName = "Ξαγοράρη", Name = " Άρτεμις" });
            testModels.Add(new TestModel { Id = Guid.NewGuid(), Age = 4, LastName = "Ξαγορ%άρης", Name = "Φοίβος" });
            testModels.Add(new TestModel { Id = Guid.NewGuid(), Age = 44, LastName = "Ξαγοραρης", Name = "Κών/νος" });
            testModels.Add(new TestModel { Id = Guid.NewGuid(), Age = 44, LastName = "Δούμουρα", Name = " Λητώ)" });
            var component = ctx.RenderComponent<RadzenDropDownDataGrid<TestModel>>(parameterBuilder => parameterBuilder.Add(p => p.Data, testModels)
            .Add(p => p.AllowFiltering, true).Add(p => p.TextProperty, "LastName")
            .Add<RadzenDataGridColumn<dynamic>>(p => p.Columns, c => c.Add(y => y.Property, "LastName").Add(y => y.Filterable, true))
            .Add<RadzenDataGridColumn<dynamic>>(p => p.Columns, c => c.Add(y => y.Property, "Name").Add(y => y.Filterable, true)));
            ctx.JSInterop.Setup<String>("Radzen.repositionPopup", _ => true);
            return component;
        }
    }
}

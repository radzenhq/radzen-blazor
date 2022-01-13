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
            List<TestModel> testModels = new List<TestModel>();
            testModels.Add(new TestModel { Id = Guid.NewGuid(), Age = 7, LastName = "Ξαγοράρη", Name = " Άρτεμις" });
            testModels.Add(new TestModel { Id = Guid.NewGuid(), Age = 4, LastName = "Ξαγοράρης", Name = "Φοίβος" });
            testModels.Add(new TestModel { Id = Guid.NewGuid(), Age = 44, LastName = "Ξαγοράρης", Name = "Κών/νος" });
            testModels.Add(new TestModel { Id = Guid.NewGuid(), Age = 44, LastName = "Δούμουρα", Name = " Λητώ" });
            var component = ctx.RenderComponent<RadzenDropDownDataGrid<TestModel>>(parameterBuilder => parameterBuilder.Add  (p => p.Data, testModels)
            .Add(p=>p.AllowFiltering,true).Add(p=>p.FilterCaseSensitivity, FilterCaseSensitivity.CaseInsensitive).Add(p => p.FilterDiacriticsSensitivity, FilterDiacriticsSensitivity.DiacriticsInsensitive));
             var raised = false;
            LoadDataArgs newArgs = null;

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.AllowFiltering, true);
                parameters.Add(p => p.FilterCaseSensitivity, FilterCaseSensitivity.CaseInsensitive);
                parameters.Add(p => p.FilterDiacriticsSensitivity, FilterDiacriticsSensitivity.DiacriticsInsensitive);
                });

            var comp = component.Find(".rz-lookup-search input"); 
            ctx.JSInterop.Setup<String>("Radzen.getInputValue", _ => true).SetResult("οραρ");
            comp.Change("οραρ");

            Assert.Equal(3, component.Instance.View.Cast<TestModel>().ToList().Count());
         }
    }
}

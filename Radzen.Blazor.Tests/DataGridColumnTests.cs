using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Radzen.Blazor.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridColumnTests
    {
        class TestModel
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
        }
        class Testable : RadzenDataGridColumn<TestModel>
        {
            public Testable()
            {
                Grid = new RadzenDataGrid<TestModel>();
            }
            public void PublicMorozov_OnInitialized()
            {
                OnInitialized();
            }
            public Type PublicMorozov_FilterPropertyType()
            {
                var propertyInfo = this.GetType()
                    .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .FirstOrDefault(x => x.Name == "FilterPropertyType");

                return propertyInfo?.GetValue(this) as Type;
            }
        }

        [Fact]
        public void FilterPropertyType_Assigned_From_Type_Parameter()
        {
            var column = new Testable()
            {
                Property = nameof(TestModel.Id),
                Type = typeof(string),
                FilterProperty = null
            };

            column.PublicMorozov_OnInitialized();

            Assert.Equal(typeof(string), column.PublicMorozov_FilterPropertyType());
            Assert.Equal(FilterOperator.Contains, column.FilterOperator);
        }

        [Fact]
        public void FilterPropertyType_Assigned_From_FilterProperty_Parameter()
        {
            var column = new Testable()
            {
                Property = nameof(TestModel.Id),
                Type = null,
                FilterProperty = nameof(TestModel.Name)
            };

            column.PublicMorozov_OnInitialized();

            Assert.Equal(typeof(string), column.PublicMorozov_FilterPropertyType());
            Assert.Equal(FilterOperator.Contains, column.FilterOperator);
        }

        [Fact]
        public void FilterPropertyType_Assigned_From_ColumnType()
        {
            var column = new Testable()
            {
                Property = nameof(TestModel.Id),
                Type = null,
                FilterProperty = null
            };

            column.PublicMorozov_OnInitialized();

            Assert.Equal(typeof(Guid), column.PublicMorozov_FilterPropertyType());
            Assert.Equal(FilterOperator.Equals, column.FilterOperator);
        }

        [Fact]
        public void FilterPropertyType_Assigned_From_Type_If_FilterProperty_Is_Fake_Field()
        {
            var column = new Testable()
            {
                Property = nameof(TestModel.Id),
                Type = typeof(decimal),
                FilterProperty = "NotExistsField"
            };

            column.PublicMorozov_OnInitialized();

            Assert.Equal(typeof(decimal), column.PublicMorozov_FilterPropertyType());
        }
    }
}

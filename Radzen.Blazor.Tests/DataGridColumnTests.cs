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
        class Testable<TItem> : RadzenDataGridColumn<TItem> 
        {
			public Testable() {
                Grid = new RadzenDataGrid<TItem>();
            }
            public void PublicMorozov_OnInitialized() 
            {
                OnInitialized();
            }
        }

        [Fact]
        public void DataGridColumn_FilterPropertyType_Used_From_Type_Parameter()
        {
            
            var column = new Testable<int>() 
            { 
                Type = typeof(string)
            };

            column.PublicMorozov_OnInitialized();
            Assert.Equal(column.FilterOperator, FilterOperator.Contains);
        }
    }
}

using Bunit;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridValueGetterTests
    {
        class Address
        {
            public string City { get; set; }
            public int ZipCode { get; set; }
        }

        struct Detail
        {
            public int Level { get; set; }
        }

        class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public Address Address { get; set; }
            public Detail? Detail { get; set; }
        }

        static List<Person> People() => new()
        {
            new Person { Id = 1, Name = "Charlie", Address = new Address { City = "Paris" } },
            new Person { Id = 2, Name = "Alice", Address = new Address { City = "Berlin" } },
            new Person { Id = 3, Name = "Bob", Address = new Address { City = "London" } },
        };

        static List<Person> PeopleWithNullAddress() => new()
        {
            new Person { Id = 1, Name = "Charlie", Address = new Address { City = "Paris", ZipCode = 75001 }, Detail = new Detail { Level = 7 } },
            new Person { Id = 2, Name = "NoAddress", Address = null, Detail = null },
        };

        static IRenderedComponent<RadzenDataGrid<Person>> RenderGrid(TestContext ctx, RenderFragment columns)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            return ctx.RenderComponent<RadzenDataGrid<Person>>(parameterBuilder =>
            {
                parameterBuilder.Add(p => p.Data, People());
                parameterBuilder.Add(p => p.AllowSorting, true);
                parameterBuilder.Add(p => p.Columns, columns);
            });
        }

        static IRenderedComponent<RadzenDataGrid<Person>> RenderGridWith(TestContext ctx, List<Person> data, RenderFragment columns)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");
            return ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, data);
                p.Add(g => g.Columns, columns);
            });
        }

        // A null intermediate on a nested path must render an empty cell, matching the reflection-based value
        // access the compiled getter replaced - not a NullReferenceException, and not the leaf type's default
        // (e.g. "0" for an int leaf).

        [Fact]
        public void StringProperty_NestedValueTypeLeaf_NullIntermediate_RendersEmptyNotDefault()
        {
            using var ctx = new TestContext();
            var component = RenderGridWith(ctx, PeopleWithNullAddress(), builder =>
            {
                builder.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                builder.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), "Address.ZipCode");
                builder.CloseComponent();
            });

            var cells = component.FindAll(".rz-cell-data");
            Assert.Equal("75001", cells[0].TextContent.Trim());
            Assert.Equal("", cells[1].TextContent.Trim()); // null Address -> empty, not "0"
        }

        [Fact]
        public void StringProperty_NullableValueTypeIntermediate_NullIntermediate_RendersEmptyNotDefault()
        {
            using var ctx = new TestContext();
            // "Detail.Level" where Detail is a Nullable<Detail> (a value type). A null Detail must render
            // empty, not the leaf int's default "0".
            var component = RenderGridWith(ctx, PeopleWithNullAddress(), builder =>
            {
                builder.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                builder.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), "Detail.Level");
                builder.CloseComponent();
            });

            var cells = component.FindAll(".rz-cell-data");
            Assert.Equal("7", cells[0].TextContent.Trim());
            Assert.Equal("", cells[1].TextContent.Trim()); // null Detail -> empty, not "0"
        }

        [Fact]
        public void StringProperty_ExplicitNullableValueInPath_NullIntermediate_RendersEmptyNotThrows()
        {
            using var ctx = new TestContext();
            // "Detail.Value.Level" writes the Nullable<Detail>.Value access explicitly. A null Detail must
            // guard the .Value (rendering empty), not throw InvalidOperationException.
            var component = RenderGridWith(ctx, PeopleWithNullAddress(), builder =>
            {
                builder.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                builder.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), "Detail.Value.Level");
                builder.CloseComponent();
            });

            var cells = component.FindAll(".rz-cell-data");
            Assert.Equal("7", cells[0].TextContent.Trim());
            Assert.Equal("", cells[1].TextContent.Trim()); // null Detail -> empty, not a throw
        }

        [Fact]
        public void StringProperty_HasValueInPath_KeepsBooleanSemantics()
        {
            using var ctx = new TestContext();
            // "Detail.HasValue" must read the boolean, returning false for a null Detail rather than empty.
            var component = RenderGridWith(ctx, PeopleWithNullAddress(), builder =>
            {
                builder.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                builder.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), "Detail.HasValue");
                builder.CloseComponent();
            });

            var cells = component.FindAll(".rz-cell-data");
            Assert.Equal("True", cells[0].TextContent.Trim());
            Assert.Equal("False", cells[1].TextContent.Trim());
        }

        [Fact]
        public void StringProperty_NestedReferenceLeaf_NullIntermediate_RendersEmpty()
        {
            using var ctx = new TestContext();
            var component = RenderGridWith(ctx, PeopleWithNullAddress(), builder =>
            {
                builder.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                builder.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), "Address.City");
                builder.CloseComponent();
            });

            var cells = component.FindAll(".rz-cell-data");
            Assert.Equal("Paris", cells[0].TextContent.Trim());
            Assert.Equal("", cells[1].TextContent.Trim());
        }

        [Fact]
        public void ChangingProperty_RebuildsCachedValueGetter()
        {
            using var ctx = new TestContext();
            var component = RenderGrid(ctx, builder =>
            {
                builder.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                builder.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), "Name");
                builder.CloseComponent();
            });

            var column = component.Instance.ColumnsCollection.Single();
            Assert.Equal("Charlie", column.GetValue(People()[0]));

            // A reused column instance whose Property parameter changes must not keep the old getter.
            column.Property = "Id";
            Assert.Equal("1", column.GetValue(People()[0]));
        }
    }
}

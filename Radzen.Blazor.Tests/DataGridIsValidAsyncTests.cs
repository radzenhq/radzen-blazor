using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridIsValidAsyncTests
    {
        public class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        static async Task<IRenderedComponent<RadzenDataGrid<Person>>> RenderGridInEditMode(TestContext ctx, Person person)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, new List<Person> { person });
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent<RadzenDataGridColumn<Person>>(0);
                    builder.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), nameof(Person.Name));
                    builder.CloseComponent();
                });
            });

            await component.InvokeAsync(() => component.Instance.EditRow(person));

            return component;
        }

        static void AddAsyncValidator(EditContext editContext, bool valid)
        {
            var messages = new ValidationMessageStore(editContext);

            editContext.OnValidationRequested += (sender, args) => args.AddAsyncValidator(async token =>
            {
                await Task.Yield();

                messages.Clear();

                if (!valid)
                {
                    messages.Add(editContext.Field(nameof(Person.Name)), "Name is taken");
                }
            });
        }

        [Fact]
        public async Task IsValidAsync_ReturnsTrue_WhenNoRowsAreEdited()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenDataGrid<Person>>(p => p.Add(g => g.Data, new List<Person>()));

            Assert.True(await component.InvokeAsync(() => component.Instance.IsValidAsync()));
        }

        [Fact]
        public async Task IsValidAsync_AwaitsAsyncValidators()
        {
            using var ctx = new TestContext();
            var person = new Person { Id = 1, Name = "Person 1" };
            var component = await RenderGridInEditMode(ctx, person);

            AddAsyncValidator(component.Instance.editContexts[person], valid: false);

            Assert.False(await component.InvokeAsync(() => component.Instance.IsValidAsync()));
        }

        [Fact]
        public async Task IsValidAsync_ReturnsTrue_WhenAsyncValidatorsPass()
        {
            using var ctx = new TestContext();
            var person = new Person { Id = 1, Name = "Person 1" };
            var component = await RenderGridInEditMode(ctx, person);

            AddAsyncValidator(component.Instance.editContexts[person], valid: true);

            Assert.True(await component.InvokeAsync(() => component.Instance.IsValidAsync()));
        }

        [Fact]
        public async Task IsValid_Throws_WhenAsyncValidatorIsRegistered()
        {
            using var ctx = new TestContext();
            var person = new Person { Id = 1, Name = "Person 1" };
            var component = await RenderGridInEditMode(ctx, person);

            AddAsyncValidator(component.Instance.editContexts[person], valid: true);

            Assert.Throws<InvalidOperationException>(() => component.Instance.IsValid);
        }
    }
}

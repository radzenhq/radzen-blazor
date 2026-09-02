using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    // Pins the compatibility effects of omitting edit cascades from read-only rows.
    public class DataGridRowCascadeBehaviourTests
    {
        public class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        static List<Person> People(int n) =>
            Enumerable.Range(1, n).Select(i => new Person { Id = i, Name = "Person " + i }).ToList();

        // Distinguishes subtree replacement from an in-place render.
        public class InitCounter : ComponentBase
        {
            public static int Initialised;
            protected override void OnInitialized() => Initialised++;
            protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b)
                => b.AddContent(0, "probe");
        }

        static IRenderedComponent<RadzenDataGrid<Person>> EditableGrid(TestContext ctx, List<Person> data) =>
            ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, data);
                p.Add<RenderFragment>(g => g.Columns, cb =>
                {
                    cb.OpenComponent<RadzenDataGridColumn<Person>>(0);
                    cb.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), nameof(Person.Name));
                    cb.AddAttribute(2, nameof(RadzenDataGridColumn<Person>.EditTemplate),
                        (RenderFragment<Person>)(item => eb => eb.AddContent(0, "editing " + item.Name)));
                    cb.CloseComponent();
                });
            });

        // Blazor retains omitted component parameters, so leaving edit mode must supply null explicitly.
        [Fact]
        public async Task CancellingAnEdit_ClearsTheRowEditContext()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var data = People(2);
            var component = EditableGrid(ctx, data);

            RadzenDataGridRow<Person> Row() => component.FindComponents<RadzenDataGridRow<Person>>()
                .Single(r => ReferenceEquals(r.Instance.Item, data[0])).Instance;

            Assert.Null(Row().EditContext);

            await component.InvokeAsync(() => component.Instance.EditRow(data[0]));

            Assert.NotNull(Row().EditContext);

            await component.InvokeAsync(() => component.Instance.CancelEditRow(data[0]));

            Assert.Null(Row().EditContext);
        }

        [Fact]
        public async Task UpdatingARow_ClearsTheRowEditContext()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var data = People(2);
            var component = EditableGrid(ctx, data);

            RadzenDataGridRow<Person> Row() => component.FindComponents<RadzenDataGridRow<Person>>()
                .Single(r => ReferenceEquals(r.Instance.Item, data[0])).Instance;

            await component.InvokeAsync(() => component.Instance.EditRow(data[0]));

            Assert.NotNull(Row().EditContext);

            await component.InvokeAsync(() => component.Instance.UpdateRow(data[0]));

            Assert.Null(Row().EditContext);
        }

        [Fact]
        public void ReadOnlyRow_NoLongerShadowsAnEnclosingForm()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var data = People(2);
            var model = new Person { Id = 99, Name = "Outer" };

            var form = ctx.RenderComponent<RadzenTemplateForm<Person>>(p =>
            {
                p.Add(f => f.Data, model);
                p.Add<RenderFragment<EditContext>>(f => f.ChildContent, _ => builder =>
                {
                    builder.OpenComponent<RadzenDataGrid<Person>>(0);
                    builder.AddAttribute(1, nameof(RadzenDataGrid<Person>.Data), data);
                    builder.AddAttribute(2, nameof(RadzenDataGrid<Person>.Columns), (RenderFragment)(cb =>
                    {
                        cb.OpenComponent<RadzenDataGridColumn<Person>>(0);
                        cb.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), nameof(Person.Name));
                        cb.AddAttribute(2, nameof(RadzenDataGridColumn<Person>.Template),
                            (RenderFragment<Person>)(person => tb =>
                            {
                                tb.OpenComponent<RadzenTextBox>(0);
                                tb.AddAttribute(1, nameof(RadzenTextBox.Name), "RowBox");
                                tb.CloseComponent();
                            }));
                        cb.CloseComponent();
                    }));
                    builder.CloseComponent();
                });
            });

            Assert.NotNull(form.Instance.FindComponent("RowBox"));
        }

        [Fact]
        public void TogglingEditMode_RebuildsTheRowSubtree()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var data = People(2);
            InitCounter.Initialised = 0;

            var component = ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, data);
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent<RadzenDataGridColumn<Person>>(0);
                    builder.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), nameof(Person.Id));
                    builder.AddAttribute(2, nameof(RadzenDataGridColumn<Person>.Template),
                        (RenderFragment<Person>)(_ => tb =>
                        {
                            tb.OpenComponent<InitCounter>(0);
                            tb.CloseComponent();
                        }));
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenDataGridColumn<Person>>(3);
                    builder.AddAttribute(4, nameof(RadzenDataGridColumn<Person>.Property), nameof(Person.Name));
                    builder.AddAttribute(5, nameof(RadzenDataGridColumn<Person>.EditTemplate),
                        (RenderFragment<Person>)(person => eb => eb.AddContent(0, "editing")));
                    builder.CloseComponent();
                });
            });

            var afterFirstRender = InitCounter.Initialised;
            Assert.Equal(2, afterFirstRender);   // one probe per row

            component.InvokeAsync(() => component.Instance.EditRow(data[0])).GetAwaiter().GetResult();

            // Switching cascade branches rebuilds stateful components in otherwise unchanged columns.
            Assert.True(InitCounter.Initialised > afterFirstRender,
                "entering edit mode is expected to rebuild the row subtree");
        }

        [Fact]
        public void WhileEditing_ReRendersDoNotRebuildTheRow()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var data = People(2);
            InitCounter.Initialised = 0;

            var component = ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, data);
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent<RadzenDataGridColumn<Person>>(0);
                    builder.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), nameof(Person.Id));
                    builder.AddAttribute(2, nameof(RadzenDataGridColumn<Person>.EditTemplate),
                        (RenderFragment<Person>)(_ => tb =>
                        {
                            tb.OpenComponent<InitCounter>(0);
                            tb.CloseComponent();
                        }));
                    builder.CloseComponent();
                });
            });

            component.InvokeAsync(() => component.Instance.EditRow(data[0])).GetAwaiter().GetResult();

            var afterEnteringEdit = InitCounter.Initialised;

            // Remaining in the same branch must preserve the editor subtree.
            for (var i = 0; i < 3; i++)
            {
                component.InvokeAsync(() => component.Instance.Reload()).GetAwaiter().GetResult();
            }

            Assert.Equal(afterEnteringEdit, InitCounter.Initialised);
        }
    }
}

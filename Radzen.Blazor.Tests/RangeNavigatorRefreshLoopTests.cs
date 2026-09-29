using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class RangeNavigatorRefreshLoopTests
    {
        class Point
        {
            public DateTime Date { get; set; }
            public double Value { get; set; }
        }

        class PerRenderDataHost : ComponentBase
        {
            public int Renders { get; private set; }

            public int SeriesDataRequests { get; private set; }

            [Parameter]
            public double Offset { get; set; }

            IEnumerable<Point> CreateData()
            {
                SeriesDataRequests++;

                return new[]
                {
                    new Point { Date = new DateTime(2024, 1, 1), Value = 42000 + Offset },
                    new Point { Date = new DateTime(2024, 2, 1), Value = 38000 + Offset },
                    new Point { Date = new DateTime(2024, 3, 1), Value = 45000 + Offset },
                };
            }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                Renders++;

                builder.OpenComponent<RadzenRangeNavigator>(0);
                builder.AddAttribute(1, nameof(RadzenRangeNavigator.ShowAxis), true);
                builder.AddAttribute(2, nameof(RadzenRangeNavigator.ChildContent), (RenderFragment)(child =>
                {
                    child.OpenComponent<RadzenRangeNavigatorLineSeries<Point>>(0);
                    child.AddAttribute(1, "Data", CreateData());
                    child.AddAttribute(2, "CategoryProperty", nameof(Point.Date));
                    child.AddAttribute(3, "ValueProperty", nameof(Point.Value));
                    child.CloseComponent();
                }));
                builder.CloseComponent();
            }
        }

        static T WithinTimeout<T>(Func<T> action)
        {
            var task = Task.Run(action);

            Assert.True(task.Wait(TimeSpan.FromSeconds(15)), "The range navigator kept re-rendering and never settled.");

            return task.Result;
        }

        [Fact]
        public void RangeNavigator_Settles_WhenSeriesDataIsCreatedOnEveryRender()
        {
            using var ctx = CreateChartContext(800, 60);

            var host = WithinTimeout(() => ctx.RenderComponent<PerRenderDataHost>());

            Assert.Contains("<svg", host.Markup);
            Assert.InRange(host.Instance.Renders, 1, 4);
            Assert.InRange(host.Instance.SeriesDataRequests, 1, 4);
        }

        [Fact]
        public void RangeNavigator_StillRefreshes_WhenRecreatedDataHasDifferentValues()
        {
            using var ctx = CreateChartContext(800, 60);

            var host = WithinTimeout(() => ctx.RenderComponent<PerRenderDataHost>());
            var navigator = host.FindComponent<RadzenRangeNavigator>();
            var before = navigator.Instance.ValueScale.Input.End;

            WithinTimeout(() => { host.SetParametersAndRender(p => p.Add(x => x.Offset, 10000)); return true; });

            Assert.True(navigator.Instance.ValueScale.Input.End > before);
        }
    }
}

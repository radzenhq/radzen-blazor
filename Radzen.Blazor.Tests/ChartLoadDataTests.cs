using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;
using static Radzen.Blazor.Tests.ChartTestHelper;

namespace Radzen.Blazor.Tests
{
    public class ChartLoadDataTests
    {
        class Reading
        {
            public DateTime Time { get; set; }
            public double Value { get; set; }
        }

        static readonly DateTime Start = new DateTime(2024, 1, 1);
        static readonly DateTime End = new DateTime(2025, 1, 1);

        class Host
        {
            public List<ChartLoadDataEventArgs> Calls { get; } = new();
            public List<Reading> Data { get; set; } = new();
            public bool SetRange { get; set; } = true;

            public RenderFragment Content => builder =>
            {
                builder.OpenComponent<RadzenLineSeries<Reading>>(0);
                builder.AddAttribute(1, nameof(RadzenLineSeries<Reading>.CategoryProperty), nameof(Reading.Time));
                builder.AddAttribute(2, nameof(RadzenLineSeries<Reading>.ValueProperty), nameof(Reading.Value));
                builder.AddAttribute(3, nameof(RadzenLineSeries<Reading>.Data), Data);
                builder.CloseComponent();

                if (SetRange)
                {
                    builder.OpenComponent<RadzenCategoryAxis>(4);
                    builder.AddAttribute(5, nameof(RadzenCategoryAxis.Min), (object)Start);
                    builder.AddAttribute(6, nameof(RadzenCategoryAxis.Max), (object)End);
                    builder.CloseComponent();
                }
            };

            public void Load(ChartLoadDataEventArgs args)
            {
                Calls.Add(args);

                if (args.Start is DateTime from && args.End is DateTime to)
                {
                    var step = (to - from) / 50;

                    Data = Enumerable.Range(0, 51).Select(i => new Reading { Time = from + step * i, Value = i % 7 }).ToList();
                }
            }
        }

        static async Task<IRenderedComponent<RadzenChart>> Render(TestContext ctx, Host host)
        {
            var chart = ctx.RenderComponent<RadzenChart>(p => p
                .Add(x => x.AllowZoom, true)
                .Add(x => x.LoadData, host.Load)
                .Add(x => x.ChildContent, host.Content));

            await chart.InvokeAsync(() => chart.Instance.Resize(700, 300));

            chart.SetParametersAndRender(p => p.Add(x => x.ChildContent, host.Content));

            return chart;
        }

        [Fact]
        public async Task LoadData_IsRaisedWithTheFullRangeOnceTheChartIsSized()
        {
            using var ctx = CreateChartContext();

            var host = new Host();

            await Render(ctx, host);

            var args = host.Calls.Last();

            Assert.Equal(0, args.ViewStart);
            Assert.Equal(1, args.ViewEnd);
            Assert.Equal(Start, args.Start);
            Assert.Equal(End, args.End);
            Assert.InRange(args.Width, 400, 700);
        }

        [Fact]
        public async Task LoadData_IsNotRaisedAgainWhenTheHandlerAssignsData()
        {
            using var ctx = CreateChartContext();

            var host = new Host();
            var chart = await Render(ctx, host);
            var calls = host.Calls.Count;

            for (var i = 0; i < 3; i++)
            {
                chart.SetParametersAndRender(p => p.Add(x => x.ChildContent, host.Content));
            }

            Assert.Equal(calls, host.Calls.Count);
            Assert.Equal(51, host.Data.Count);
        }

        [Fact]
        public async Task LoadData_IsRaisedWithTheVisibleRangeAfterZoom()
        {
            using var ctx = CreateChartContext();

            var host = new Host();
            var chart = await Render(ctx, host);
            var calls = host.Calls.Count;

            await chart.InvokeAsync(() => chart.Instance.OnWheel(350, -3));

            Assert.Equal(calls + 1, host.Calls.Count);

            var args = host.Calls.Last();
            var start = Assert.IsType<DateTime>(args.Start);
            var end = Assert.IsType<DateTime>(args.End);

            Assert.True(args.ViewEnd - args.ViewStart < 1);
            Assert.True(start > Start);
            Assert.True(end < End);
            Assert.Equal((End - Start).TotalDays * (args.ViewEnd - args.ViewStart), (end - start).TotalDays, 3);
        }

        [Fact]
        public async Task LoadData_IsRaisedWhenTheChartIsResized()
        {
            using var ctx = CreateChartContext();

            var host = new Host();
            var chart = await Render(ctx, host);
            var calls = host.Calls.Count;
            var width = host.Calls.Last().Width;

            await chart.InvokeAsync(() => chart.Instance.Resize(700, 300));

            Assert.Equal(calls, host.Calls.Count);

            await chart.InvokeAsync(() => chart.Instance.Resize(1000, 300));

            Assert.Equal(calls + 1, host.Calls.Count);
            Assert.True(host.Calls.Last().Width > width);
        }

        [Fact]
        public async Task LoadData_IsRaisedByReload()
        {
            using var ctx = CreateChartContext();

            var host = new Host();
            var chart = await Render(ctx, host);
            var calls = host.Calls.Count;

            await chart.InvokeAsync(() => chart.Instance.Reload());

            Assert.Equal(calls + 1, host.Calls.Count);
        }

        [Fact]
        public async Task LoadData_HasNoRangeWhenTheAxisHasNone()
        {
            using var ctx = CreateChartContext();

            var host = new Host { SetRange = false };

            await Render(ctx, host);

            Assert.NotEmpty(host.Calls);
            Assert.Null(host.Calls[0].Start);
            Assert.Null(host.Calls[0].End);
        }
    }
}

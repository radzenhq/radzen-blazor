using System;
using Radzen;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public abstract class SchedulerViewBase : ComponentBase, ISchedulerView, IDisposable
    {
        public abstract string Title { get; }
        public abstract string Text { get; set; }

        [CascadingParameter]
        public IScheduler Scheduler { get; set; }

        public void Dispose()
        {
            Scheduler?.RemoveView(this);
        }

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(Text), Text))
            {
                if (Scheduler != null)
                {
                    await Scheduler.Reload();
                }
            }

            await base.SetParametersAsync(parameters);

            await Scheduler.AddView(this);
        }

        public abstract DateTime Next();

        public abstract DateTime Prev();

        public abstract RenderFragment Render();

        public abstract DateTime StartDate { get; }
        public abstract DateTime EndDate { get; }
    }
}
using System;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public interface ISchedulerView
    {
        string Title { get; }
        string Text { get; }

        DateTime Next();

        DateTime Prev();

        RenderFragment Render();

        DateTime StartDate { get; }
        DateTime EndDate { get; }
    }
}
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;

namespace Radzen
{
    /// <summary>
    /// Supplies information about a <see cref="RadzenResourceScheduler{TItem}.SlotRender" /> event that is being raised.
    /// </summary>
    public class ResourceSchedulerColumnStyles    {

        public bool ColumnExpanded { get; set; }
        public string ColumnMaximumWidth { get; set; }
        public string ColumnMinimumWidth { get; set; }
        public string ColumnWidth { get; set; }
        public string ColumnVisibility { get; set; }
        public string ResourceMaximumWidth { get; set; }
        public string ResourceMinimumWidth { get; set; }
        public string ResourceWidth { get; set; }
        public string ResourceVisibility { get; set; }
        public string ResourceHeight { get; set; }
        public string SchedulerMaximumWidth { get; set; }
        public string SchedulerMinimumWidth { get; set; }
        public string SchedulerWidth { get; set; }
        public string SchedulerVisibility { get; set; }
        public string SchedulerHeight { get; set; }
    }
}
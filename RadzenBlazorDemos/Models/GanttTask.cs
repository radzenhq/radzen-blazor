using System;

namespace RadzenBlazorDemos.Models
{
#nullable enable
    public class GanttTask
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }

        public string? Name { get; set; }

        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public double? Progress { get; set; }

        public DateTime? BaselineStart { get; set; }
        public DateTime? BaselineEnd { get; set; }
    }
#nullable disable
}


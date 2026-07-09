using System;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// Computes accumulated workload values per timeline column for the Gantt histogram.
    /// A task contributes its value to every column its date range overlaps.
    /// Milestones (Start == End) contribute to the column containing their date.
    /// </summary>
    internal static class GanttHistogramLayout
    {
        internal static double[] Compute(
            IReadOnlyList<(DateTime Start, DateTime End)> columns,
            IReadOnlyList<(DateTime Start, DateTime End, double Value)> items)
        {
            var values = new double[columns.Count];

            for (var i = 0; i < columns.Count; i++)
            {
                var (columnStart, columnEnd) = columns[i];
                double sum = 0;

                foreach (var item in items)
                {
                    var overlaps = item.Start == item.End
                        ? item.Start >= columnStart && item.Start < columnEnd
                        : item.Start < columnEnd && item.End > columnStart;

                    if (overlaps)
                    {
                        sum += item.Value;
                    }
                }

                values[i] = sum;
            }

            return values;
        }
    }
}

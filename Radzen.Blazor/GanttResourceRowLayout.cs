using System;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// Computes lane assignments for overlapping task bars within resource rows.
    /// Uses a greedy sweep-line algorithm: tasks sorted by start date are assigned to the first available lane.
    /// </summary>
    internal static class GanttResourceRowLayout
    {
        /// <summary>
        /// Computes lane assignments for tasks grouped by resource row.
        /// </summary>
        /// <param name="appointmentsByRow">Tasks grouped by their resource row index.</param>
        /// <param name="laneByItem">Output: maps each task (appointment.Data) to its lane index within the row.</param>
        /// <param name="laneCountByRow">Output: maps each row index to the number of lanes needed.</param>
        internal static void ComputeLanes(
            IReadOnlyDictionary<int, List<AppointmentData>> appointmentsByRow,
            out Dictionary<object, int> laneByItem,
            out Dictionary<int, int> laneCountByRow)
        {
            laneByItem = new Dictionary<object, int>();
            laneCountByRow = new Dictionary<int, int>();

            foreach (var kvp in appointmentsByRow)
            {
                var row = kvp.Key;
                var appointments = kvp.Value;

                if (appointments.Count <= 1)
                {
                    laneCountByRow[row] = 1;
                    if (appointments.Count == 1 && appointments[0].Data != null)
                    {
                        laneByItem[appointments[0].Data!] = 0;
                    }
                    continue;
                }

                // Sort by start date, then by end date descending (longer tasks first)
                var sorted = new List<AppointmentData>(appointments);
                sorted.Sort((a, b) =>
                {
                    var cmp = a.Start.CompareTo(b.Start);
                    if (cmp != 0)
                    {
                        return cmp;
                    }
                    return b.End.CompareTo(a.End);
                });

                // Greedy lane assignment: track when each lane becomes free
                var laneEnds = new List<DateTime>();
                int maxLane = 0;

                foreach (var appointment in sorted)
                {
                    if (appointment.Data == null)
                    {
                        continue;
                    }

                    int assignedLane = -1;
                    for (int lane = 0; lane < laneEnds.Count; lane++)
                    {
                        if (appointment.Start >= laneEnds[lane])
                        {
                            assignedLane = lane;
                            laneEnds[lane] = appointment.End;
                            break;
                        }
                    }

                    if (assignedLane == -1)
                    {
                        assignedLane = laneEnds.Count;
                        laneEnds.Add(appointment.End);
                    }

                    laneByItem[appointment.Data] = assignedLane;
                    if (assignedLane > maxLane)
                    {
                        maxLane = assignedLane;
                    }
                }

                laneCountByRow[row] = maxLane + 1;
            }
        }
    }
}

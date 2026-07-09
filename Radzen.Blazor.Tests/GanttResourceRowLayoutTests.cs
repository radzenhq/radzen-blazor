using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class GanttResourceRowLayoutTests
    {
        [Fact]
        public void ComputeLanes_EmptyInput_ReturnsEmptyResults()
        {
            var appointmentsByRow = new Dictionary<int, List<AppointmentData>>();

            GanttResourceRowLayout.ComputeLanes(appointmentsByRow, out var laneByItem, out var laneCountByRow);

            Assert.Empty(laneByItem);
            Assert.Empty(laneCountByRow);
        }

        [Fact]
        public void ComputeLanes_SingleTaskPerRow_AllInLaneZero()
        {
            var task1 = new object();
            var task2 = new object();
            var appointmentsByRow = new Dictionary<int, List<AppointmentData>>
            {
                [0] = new() { new AppointmentData { Data = task1, Start = new DateTime(2026, 1, 1), End = new DateTime(2026, 1, 5) } },
                [1] = new() { new AppointmentData { Data = task2, Start = new DateTime(2026, 1, 3), End = new DateTime(2026, 1, 8) } }
            };

            GanttResourceRowLayout.ComputeLanes(appointmentsByRow, out var laneByItem, out var laneCountByRow);

            Assert.Equal(0, laneByItem[task1]);
            Assert.Equal(0, laneByItem[task2]);
            Assert.Equal(1, laneCountByRow[0]);
            Assert.Equal(1, laneCountByRow[1]);
        }

        [Fact]
        public void ComputeLanes_NonOverlappingTasks_SameLane()
        {
            var task1 = new object();
            var task2 = new object();
            var appointmentsByRow = new Dictionary<int, List<AppointmentData>>
            {
                [0] = new()
                {
                    new AppointmentData { Data = task1, Start = new DateTime(2026, 1, 1), End = new DateTime(2026, 1, 5) },
                    new AppointmentData { Data = task2, Start = new DateTime(2026, 1, 5), End = new DateTime(2026, 1, 10) }
                }
            };

            GanttResourceRowLayout.ComputeLanes(appointmentsByRow, out var laneByItem, out var laneCountByRow);

            Assert.Equal(0, laneByItem[task1]);
            Assert.Equal(0, laneByItem[task2]);
            Assert.Equal(1, laneCountByRow[0]);
        }

        [Fact]
        public void ComputeLanes_OverlappingTasks_DifferentLanes()
        {
            var task1 = new object();
            var task2 = new object();
            var appointmentsByRow = new Dictionary<int, List<AppointmentData>>
            {
                [0] = new()
                {
                    new AppointmentData { Data = task1, Start = new DateTime(2026, 1, 1), End = new DateTime(2026, 1, 10) },
                    new AppointmentData { Data = task2, Start = new DateTime(2026, 1, 5), End = new DateTime(2026, 1, 15) }
                }
            };

            GanttResourceRowLayout.ComputeLanes(appointmentsByRow, out var laneByItem, out var laneCountByRow);

            Assert.Equal(0, laneByItem[task1]);
            Assert.Equal(1, laneByItem[task2]);
            Assert.Equal(2, laneCountByRow[0]);
        }

        [Fact]
        public void ComputeLanes_ThreeOverlappingTasks_ThreeLanes()
        {
            var task1 = new object();
            var task2 = new object();
            var task3 = new object();
            var appointmentsByRow = new Dictionary<int, List<AppointmentData>>
            {
                [0] = new()
                {
                    new AppointmentData { Data = task1, Start = new DateTime(2026, 1, 1), End = new DateTime(2026, 1, 20) },
                    new AppointmentData { Data = task2, Start = new DateTime(2026, 1, 5), End = new DateTime(2026, 1, 15) },
                    new AppointmentData { Data = task3, Start = new DateTime(2026, 1, 8), End = new DateTime(2026, 1, 12) }
                }
            };

            GanttResourceRowLayout.ComputeLanes(appointmentsByRow, out var laneByItem, out var laneCountByRow);

            Assert.Equal(0, laneByItem[task1]);
            Assert.Equal(1, laneByItem[task2]);
            Assert.Equal(2, laneByItem[task3]);
            Assert.Equal(3, laneCountByRow[0]);
        }

        [Fact]
        public void ComputeLanes_PartialOverlap_ReusesLanes()
        {
            var task1 = new object();
            var task2 = new object();
            var task3 = new object();
            var appointmentsByRow = new Dictionary<int, List<AppointmentData>>
            {
                [0] = new()
                {
                    new AppointmentData { Data = task1, Start = new DateTime(2026, 1, 1), End = new DateTime(2026, 1, 5) },
                    new AppointmentData { Data = task2, Start = new DateTime(2026, 1, 3), End = new DateTime(2026, 1, 8) },
                    new AppointmentData { Data = task3, Start = new DateTime(2026, 1, 6), End = new DateTime(2026, 1, 10) }
                }
            };

            GanttResourceRowLayout.ComputeLanes(appointmentsByRow, out var laneByItem, out var laneCountByRow);

            // task1 in lane 0, task2 overlaps so goes to lane 1
            // task3 starts after task1 ends, so reuses lane 0
            Assert.Equal(0, laneByItem[task1]);
            Assert.Equal(1, laneByItem[task2]);
            Assert.Equal(0, laneByItem[task3]);
            Assert.Equal(2, laneCountByRow[0]);
        }

        [Fact]
        public void ComputeLanes_MultipleRows_IndependentLaneComputation()
        {
            var taskA1 = new object();
            var taskA2 = new object();
            var taskB1 = new object();
            var appointmentsByRow = new Dictionary<int, List<AppointmentData>>
            {
                [0] = new()
                {
                    new AppointmentData { Data = taskA1, Start = new DateTime(2026, 1, 1), End = new DateTime(2026, 1, 10) },
                    new AppointmentData { Data = taskA2, Start = new DateTime(2026, 1, 5), End = new DateTime(2026, 1, 15) }
                },
                [1] = new()
                {
                    new AppointmentData { Data = taskB1, Start = new DateTime(2026, 1, 1), End = new DateTime(2026, 1, 10) }
                }
            };

            GanttResourceRowLayout.ComputeLanes(appointmentsByRow, out var laneByItem, out var laneCountByRow);

            Assert.Equal(2, laneCountByRow[0]);
            Assert.Equal(1, laneCountByRow[1]);
            Assert.Equal(0, laneByItem[taskA1]);
            Assert.Equal(1, laneByItem[taskA2]);
            Assert.Equal(0, laneByItem[taskB1]);
        }

        [Fact]
        public void ComputeLanes_NullDataItems_Skipped()
        {
            var task1 = new object();
            var appointmentsByRow = new Dictionary<int, List<AppointmentData>>
            {
                [0] = new()
                {
                    new AppointmentData { Data = null, Start = new DateTime(2026, 1, 1), End = new DateTime(2026, 1, 5) },
                    new AppointmentData { Data = task1, Start = new DateTime(2026, 1, 3), End = new DateTime(2026, 1, 8) }
                }
            };

            GanttResourceRowLayout.ComputeLanes(appointmentsByRow, out var laneByItem, out var laneCountByRow);

            Assert.Single(laneByItem);
            Assert.Equal(0, laneByItem[task1]);
        }

        [Fact]
        public void ComputeLanes_SameStartDifferentEnd_LongerTaskFirst()
        {
            var taskShort = new object();
            var taskLong = new object();
            var appointmentsByRow = new Dictionary<int, List<AppointmentData>>
            {
                [0] = new()
                {
                    new AppointmentData { Data = taskShort, Start = new DateTime(2026, 1, 1), End = new DateTime(2026, 1, 5) },
                    new AppointmentData { Data = taskLong, Start = new DateTime(2026, 1, 1), End = new DateTime(2026, 1, 20) }
                }
            };

            GanttResourceRowLayout.ComputeLanes(appointmentsByRow, out var laneByItem, out var laneCountByRow);

            // Longer task should be assigned lane 0 (sorted first by start, then by end descending)
            Assert.Equal(0, laneByItem[taskLong]);
            Assert.Equal(1, laneByItem[taskShort]);
            Assert.Equal(2, laneCountByRow[0]);
        }
    }
}

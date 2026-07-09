using Xunit;

namespace Radzen.Blazor.Tests
{
    public class GanttViewModeTests
    {
        [Fact]
        public void GanttViewMode_HasTaskAndResourceValues()
        {
            Assert.Equal(0, (int)GanttViewMode.Task);
            Assert.Equal(1, (int)GanttViewMode.Resource);
        }

        [Fact]
        public void GanttViewMode_DefaultIsTask()
        {
            var mode = default(GanttViewMode);
            Assert.Equal(GanttViewMode.Task, mode);
        }
    }
}

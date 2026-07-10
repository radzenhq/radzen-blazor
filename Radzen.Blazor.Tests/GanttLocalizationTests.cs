using System.Globalization;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class GanttLocalizationTests
    {
        [Theory]
        [InlineData("en")]
        [InlineData("de")]
        [InlineData("es")]
        [InlineData("fr")]
        [InlineData("it")]
        [InlineData("ja")]
        public void GanttResourceViewKeys_HaveTranslations(string culture)
        {
            var cultureInfo = new CultureInfo(culture);
            var keys = new[]
            {
                nameof(RadzenStrings.Gantt_UnassignedText),
                nameof(RadzenStrings.Gantt_HistogramLabel),
                nameof(RadzenStrings.Gantt_DayViewText),
                nameof(RadzenStrings.Gantt_WeekViewText),
                nameof(RadzenStrings.Gantt_MonthViewText),
                nameof(RadzenStrings.Gantt_YearViewText),
                nameof(RadzenStrings.Gantt_YearsViewText),
                nameof(RadzenStrings.Gantt_QuarterFormat)
            };

            foreach (var key in keys)
            {
                var value = RadzenStrings.ResourceManager.GetString(key, cultureInfo);
                Assert.False(string.IsNullOrWhiteSpace(value));
            }
        }

        [Fact]
        public void GanttResourceViewKeys_ReturnExpectedGermanTranslations()
        {
            var german = new CultureInfo("de");

            Assert.Equal("Nicht zugewiesen", RadzenStrings.ResourceManager.GetString(nameof(RadzenStrings.Gantt_UnassignedText), german));
            Assert.Equal("Auslastung", RadzenStrings.ResourceManager.GetString(nameof(RadzenStrings.Gantt_HistogramLabel), german));
            Assert.Equal("Woche", RadzenStrings.ResourceManager.GetString(nameof(RadzenStrings.Gantt_WeekViewText), german));
        }

        [Fact]
        public void GanttResourceViewKeys_ReturnExpectedEnglishDefaults()
        {
            var english = CultureInfo.InvariantCulture;

            Assert.Equal("Unassigned", RadzenStrings.ResourceManager.GetString(nameof(RadzenStrings.Gantt_UnassignedText), english));
            Assert.Equal("Workload", RadzenStrings.ResourceManager.GetString(nameof(RadzenStrings.Gantt_HistogramLabel), english));
            Assert.Equal("Day", RadzenStrings.ResourceManager.GetString(nameof(RadzenStrings.Gantt_DayViewText), english));
            Assert.Equal("Week", RadzenStrings.ResourceManager.GetString(nameof(RadzenStrings.Gantt_WeekViewText), english));
            Assert.Equal("Month", RadzenStrings.ResourceManager.GetString(nameof(RadzenStrings.Gantt_MonthViewText), english));
            Assert.Equal("Year", RadzenStrings.ResourceManager.GetString(nameof(RadzenStrings.Gantt_YearViewText), english));
        }
    }
}

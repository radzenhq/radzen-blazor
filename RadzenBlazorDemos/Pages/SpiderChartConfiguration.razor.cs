using Radzen.Blazor;
using System;

namespace RadzenBlazorDemos.Pages
{
    public partial class SpiderChartConfiguration
    {
        private SpiderChartGridShape gridShape = SpiderChartGridShape.Polygon;
        private ColorScheme colorScheme = ColorScheme.Palette;
        private bool showLegend = true;
        private string valueFormat = "percent";

        private Func<double, string> GetValueFormatter()
        {
            return valueFormat switch
            {
                "percent" => (value) => $"{value:F0}%",
                "number" => (value) => value.ToString("F1"),
                "score" => (value) => $"{value:F0}/100",
                "currency" => (value) => $"${value:F0}",
                _ => (value) => value.ToString("F1")
            };
        }

        private class PerformanceData
        {
            public string Department { get; set; }
            public string DepartmentName { get; set; }
            public string Metric { get; set; }
            public string MetricLabel { get; set; }
            public double Score { get; set; }
        }

        private PerformanceData[] performanceData =
        [
            new PerformanceData
            {
                Department = "ehab",
                DepartmentName = "Ehab Hussein",
                Metric = "csharp",
                MetricLabel = "C#",
                Score = 95
            },
            new PerformanceData
            {
                Department = "ehab",
                DepartmentName = "Ehab Hussein",
                Metric = "ai",
                MetricLabel = "AI/ML",
                Score = 88
            },
            new PerformanceData
            {
                Department = "ehab",
                DepartmentName = "Ehab Hussein",
                Metric = "webapp",
                MetricLabel = "Web App Testing",
                Score = 92
            },
            new PerformanceData
            {
                Department = "ehab",
                DepartmentName = "Ehab Hussein",
                Metric = "api",
                MetricLabel = "API Testing",
                Score = 90
            },
            new PerformanceData
            {
                Department = "ehab",
                DepartmentName = "Ehab Hussein",
                Metric = "burp",
                MetricLabel = "Burp Suite",
                Score = 94
            },
            new PerformanceData
            {
                Department = "ehab",
                DepartmentName = "Ehab Hussein",
                Metric = "scripting",
                MetricLabel = "Scripting",
                Score = 87
            },
            new PerformanceData
            {
                Department = "ehab",
                DepartmentName = "Ehab Hussein",
                Metric = "reporting",
                MetricLabel = "Reporting",
                Score = 85
            },
            new PerformanceData
            {
                Department = "ehab",
                DepartmentName = "Ehab Hussein",
                Metric = "cloud",
                MetricLabel = "Cloud Security",
                Score = 82
            },
            new PerformanceData
            {
                Department = "mohamed",
                DepartmentName = "Mohamed Samy",
                Metric = "csharp",
                MetricLabel = "C#",
                Score = 78
            },
            new PerformanceData
            {
                Department = "mohamed",
                DepartmentName = "Mohamed Samy",
                Metric = "ai",
                MetricLabel = "AI/ML",
                Score = 72
            },
            new PerformanceData
            {
                Department = "mohamed",
                DepartmentName = "Mohamed Samy",
                Metric = "webapp",
                MetricLabel = "Web App Testing",
                Score = 88
            },
            new PerformanceData
            {
                Department = "mohamed",
                DepartmentName = "Mohamed Samy",
                Metric = "api",
                MetricLabel = "API Testing",
                Score = 85
            },
            new PerformanceData
            {
                Department = "mohamed",
                DepartmentName = "Mohamed Samy",
                Metric = "burp",
                MetricLabel = "Burp Suite",
                Score = 91
            },
            new PerformanceData
            {
                Department = "mohamed",
                DepartmentName = "Mohamed Samy",
                Metric = "scripting",
                MetricLabel = "Scripting",
                Score = 93
            },
            new PerformanceData
            {
                Department = "mohamed",
                DepartmentName = "Mohamed Samy",
                Metric = "reporting",
                MetricLabel = "Reporting",
                Score = 80
            },
            new PerformanceData
            {
                Department = "mohamed",
                DepartmentName = "Mohamed Samy",
                Metric = "cloud",
                MetricLabel = "Cloud Security",
                Score = 76
            },
            new PerformanceData
            {
                Department = "tony",
                DepartmentName = "Tony Dorrell",
                Metric = "csharp",
                MetricLabel = "C#",
                Score = 82
            },
            new PerformanceData
            {
                Department = "tony",
                DepartmentName = "Tony Dorrell",
                Metric = "ai",
                MetricLabel = "AI/ML",
                Score = 75
            },
            new PerformanceData
            {
                Department = "tony",
                DepartmentName = "Tony Dorrell",
                Metric = "webapp",
                MetricLabel = "Web App Testing",
                Score = 95
            },
            new PerformanceData
            {
                Department = "tony",
                DepartmentName = "Tony Dorrell",
                Metric = "api",
                MetricLabel = "API Testing",
                Score = 93
            },
            new PerformanceData
            {
                Department = "tony",
                DepartmentName = "Tony Dorrell",
                Metric = "burp",
                MetricLabel = "Burp Suite",
                Score = 89
            },
            new PerformanceData
            {
                Department = "tony",
                DepartmentName = "Tony Dorrell",
                Metric = "scripting",
                MetricLabel = "Scripting",
                Score = 84
            },
            new PerformanceData
            {
                Department = "tony",
                DepartmentName = "Tony Dorrell",
                Metric = "reporting",
                MetricLabel = "Reporting",
                Score = 92
            },
            new PerformanceData
            {
                Department = "tony",
                DepartmentName = "Tony Dorrell",
                Metric = "cloud",
                MetricLabel = "Cloud Security",
                Score = 88
            }
        ];
    }
}
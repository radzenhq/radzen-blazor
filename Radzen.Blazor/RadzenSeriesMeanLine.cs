using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// Displays the mean of a chart series.
    /// </summary>
    /// <example>
    /// <code>
    ///   &lt;RadzenChart&gt;
    ///       &lt;RadzenLineSeries Data=@revenue CategoryProperty="Quarter" ValueProperty="Revenue"&gt;
    ///          &lt;RadzenSeriesMeanLine /&gt;
    ///       &lt;/RadzenLineSeries&gt;
    ///   &lt;/RadzenChart&gt;
    ///   @code {
    ///       class DataItem
    ///       {
    ///           public string Quarter { get; set; }
    ///           public double Revenue { get; set; }
    ///       }
    ///       DataItem[] revenue = new DataItem[]
    ///       {
    ///           new DataItem { Quarter = "Q1", Revenue = 234000 },
    ///           new DataItem { Quarter = "Q2", Revenue = 284000 },
    ///           new DataItem { Quarter = "Q3", Revenue = 274000 },
    ///           new DataItem { Quarter = "Q4", Revenue = 294000 }
    ///       };
    ///   }
    /// </code>
    /// </example>
    public partial class RadzenSeriesMeanLine : RadzenSeriesValueLine
    {
        /// <ihnheritdoc />
        public override double Value
        {
            get
            {
                return Series.GetMean();
            }
            set
            {
                throw new InvalidOperationException("Value is computed and cannot be set");
            }
        }

        /// <ihnheritdoc />
        protected override string Name => "Mean";
    }
}

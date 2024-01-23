namespace Radzen.Blazor
{
    /// <summary>
    /// Supplies information about a <see cref="RadzenSteps.CanChange" /> event that is being raised.
    /// </summary>
    public class StepsCanChangeEventArgs
    {
        /// <summary>
        /// Index of clicked step.
        /// </summary>
        public int SelectedIndex { get; set; }
        /// <summary>
        /// Index of clicked step.
        /// </summary>
        public int NewIndex { get; set; }
        /// <summary>
        /// Has step change action been prevented from occuring.
        /// </summary>
        public bool IsDefaultPrevented { get; private set; }
        /// <summary>
        /// Prevent the change of the step.
        /// </summary>
        public void PreventDefault()
        {
            IsDefaultPrevented = true;
        }
    }
}
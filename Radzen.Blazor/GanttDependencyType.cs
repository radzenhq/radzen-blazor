namespace Radzen.Blazor
{
    /// <summary>
    /// Specifies the type of a Gantt dependency link.
    /// </summary>
    public enum GanttDependencyType
    {
        /// <summary>
        /// Finish-to-Start: the successor cannot start until the predecessor finishes.
        /// </summary>
        FinishToStart,

        /// <summary>
        /// Start-to-Start: the successor cannot start until the predecessor starts.
        /// </summary>
        StartToStart,

        /// <summary>
        /// Finish-to-Finish: the successor cannot finish until the predecessor finishes.
        /// </summary>
        FinishToFinish,

        /// <summary>
        /// Start-to-Finish: the successor cannot finish until the predecessor starts.
        /// </summary>
        StartToFinish
    }
}

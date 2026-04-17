namespace OsClasses
{
    /// <summary>
    /// Shared state between the scheduler algorithms and the UI.
    /// Avoids circular project references by keeping pause/cancel state
    /// in the class library instead of the GUI project.
    /// </summary>
    public static class SchedulerState
    {
        public static bool IsPaused { get; set; } = false;
        public static bool IsCancelled { get; set; } = false;
    }
}

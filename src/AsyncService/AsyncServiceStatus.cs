namespace AsyncService
{
    /// <summary>
    /// Internal <see cref="AsyncServiceBase"/> service states
    /// </summary>
    public enum AsyncServiceStatus
    {
        ContinuePending = 5,
        Paused = 7,
        PausePending = 6,
        Running = 4,
        StartPending = 2,
        Stopped = 1,
        StopPending = 3
    }
}
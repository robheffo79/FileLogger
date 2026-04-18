namespace HeffernanTech.Extensions.FileLogging
{
    /// <summary>
    /// Defines when log files should roll over.
    /// </summary>
    public enum FileLogRollingMode
    {
        None = 0,
        Daily = 1,
        Size = 2,
        DailyAndSize = 3
    }
}
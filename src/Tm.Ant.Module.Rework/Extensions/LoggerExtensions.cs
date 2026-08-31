namespace Tm.Ant.Module.Rework.Extensions;

internal static class LoggerExtensions
{
    public static void LogComm(this ITmLogger logger, string message)
    {
        logger.LogInformation(message, LogCategory.Comm, LogDisplayMode.TOUI);
    }

    public static void LogAlarm(this ITmLogger logger, string message)
    {
        logger.LogError(message, LogCategory.ALARM, LogDisplayMode.TOUI);
    }
    /// <summary>
    /// 记录工位操作信息
    /// 示例: <para>[工位1] RFID 读取治具码 25X001, 绑定的产品码为: A, B</para>
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="workplaceNo"></param>
    /// <param name="message"></param>
    public static void LogWorkplaceInfo(this ITmLogger logger, int workplaceNo, string message)
    {
        var result = string.Format("[{0}{1}] {2}",
            "Workplace-",
            workplaceNo,
            message);
        logger.LogComm(result);
    } 
}

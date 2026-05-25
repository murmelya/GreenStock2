using NLog;

namespace GreenStock.Logging;


public static class AppLogger
{
    public static ILogger For<T>() => LogManager.GetLogger(typeof(T).FullName);

    public static ILogger For(string name) => LogManager.GetLogger(name);

  
    public static void Shutdown() => LogManager.Shutdown();
}

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using log4net;

namespace GlobalLogger
{
    public static class Log4NetHandler
    {
        public enum LogLevel
        {
            INFO,
            DEBUG,
            ERROR,
            WARN
        }

        private static readonly ILog log = LogManager.GetLogger("AGNSharpBot");

        public static void Log(string message,
            LogLevel logLevel,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string memberFilePath = "",
            [CallerLineNumber] int memberLineNumber = 0,
            Exception exception = null)
        {
            var date = DateTime.Now;
            var callingAssemblyName = Assembly.GetCallingAssembly().GetName().Name;
            message = $"[{callingAssemblyName}] | {message}";

            switch (logLevel)
            {
                case LogLevel.INFO:
                    log.Info(message);
                    break;

                case LogLevel.ERROR:
                    log.Error(message, exception);
                    break;

                case LogLevel.DEBUG:
                    log.Debug(message, exception);
                    break;

                case LogLevel.WARN:
                    log.Warn(message, exception);
                    break;
            }
        }
    }
}
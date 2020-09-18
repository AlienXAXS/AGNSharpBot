using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;

namespace GlobalLogger
{
    public static class Log4NetHandler
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("AGNSharpBot");

        public enum LogLevel
        {
            INFO,
            DEBUG,
            ERROR,
            WARN,
        }

        public static void Log(string message,
            LogLevel logLevel,
            [System.Runtime.CompilerServices.CallerMemberName]
            string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath]
            string memberFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber]
            int memberLineNumber = 0,
            Exception exception = null)
        {

            var date = DateTime.Now;
            var callingAssemblyName = Assembly.GetCallingAssembly().GetName().Name;
            message = $"[{callingAssemblyName}] | {message}";


#if DEBUG

            Console.WriteLine($"{logLevel}: {message}");
            return;
#endif

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

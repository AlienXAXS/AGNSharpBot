using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GlobalLogger.AdvancedLogger
{
    public class AdvancedLoggerHandler
    {
        private static readonly AdvancedLoggerHandler IAdvancedLoggerHandler;
        public static AdvancedLoggerHandler Instance = IAdvancedLoggerHandler ?? (IAdvancedLoggerHandler = new AdvancedLoggerHandler());

        private readonly List<Logger> _loggers = new List<Logger>();

        public Logger GetLogger()
        {
            var logName = Assembly.GetCallingAssembly().GetName().Name ?? "UNKNOWN";

            Logger foundLogger = null;
            if (_loggers.Count != 0)
            {
                foundLogger = _loggers.DefaultIfEmpty(null).FirstOrDefault(x => x.LogName == logName);
            }

            if (foundLogger != null)
                return foundLogger;
            else
            {
                var newLogger = new Logger() { LogName = logName };
                _loggers.Add(newLogger);
                return newLogger;
            }
        }
    }
}
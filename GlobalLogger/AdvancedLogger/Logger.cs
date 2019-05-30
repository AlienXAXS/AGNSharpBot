using System;
using System.Linq;
using System.Reflection;

namespace GlobalLogger.AdvancedLogger
{
    public class Logger
    {
        private RetentionOptions _retentionOptions = new RetentionOptions() {Compress = false, Days = 1};
        public string LogName;

        private bool _toConsole = false;

        public Logger()
        {
            try
            {
                // Setup folder structure here
                if (!System.IO.Directory.Exists("Logs"))
                    System.IO.Directory.CreateDirectory("Logs");

                //TODO: Retention & Compression
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to create the Logs Directory, error is: {ex.Message}\r\n\r\n{ex.StackTrace}");
            }
        }

        public Logger SetRetentionOptions(RetentionOptions retentionOptions)
        {
            _retentionOptions = retentionOptions;
            return this;
        }

        public Logger OutputToConsole(bool value)
        {
            _toConsole = value;
            return this;
        }

        public void Log(string message)
        {
            var logName = Assembly.GetCallingAssembly().GetName().Name;
            var date = DateTime.Now;

            var logFolderPath = $"Logs\\{date.Year}{date.Month:00}\\{logName}";
            var logFilePath = $"{logFolderPath}\\{date.Year}-{date.Month:00}-{date.Day:00}.log";
            
            try
            {
                if (!System.IO.Directory.Exists(logFolderPath))
                    System.IO.Directory.CreateDirectory(logFolderPath);

                string compiledString = "";
                foreach (var splitString in message.Split(Environment.NewLine.ToCharArray()))
                {
                    var str = $"[{date.Year}/{date.Month:00}/{date.Day:00} @ {date.Hour:00}:{date.Minute:00}:{date.Second:00}] T:{System.Threading.Thread.CurrentThread.ManagedThreadId:00000} - {logName} - {splitString}";
                    compiledString += str + Environment.NewLine;

                    if ( _toConsole )
                        Console.WriteLine(str);
                }

                System.IO.File.AppendAllText(logFilePath, compiledString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to log for {logName} to {logFilePath}.\r\n\r\nError Message:{ex.Message}\r\n\r\nLog Contents:{message}");
            }
        }
    }
}

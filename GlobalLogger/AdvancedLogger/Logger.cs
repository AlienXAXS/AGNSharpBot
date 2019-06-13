using System;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace GlobalLogger.AdvancedLogger
{
    public class Logger
    {
        private RetentionOptions _retentionOptions = new RetentionOptions() {Compress = false, Days = 1};
        public string LogName;

        private bool _toConsole = false;
        private object lockable = new object();

        private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1,1);

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

        public void Log(string message, [System.Runtime.CompilerServices.CallerMemberName]
            string memberName = "", [System.Runtime.CompilerServices.CallerFilePath]
            string memberFilePath = "", [System.Runtime.CompilerServices.CallerLineNumber]
            int memberLineNumber = 0)
        {

            _semaphoreSlim.Wait();

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
                    var str =
                        $"[{date.Year}/{date.Month:00}/{date.Day:00} @ {date.Hour:00}:{date.Minute:00}:{date.Second:00}] T:{System.Threading.Thread.CurrentThread.ManagedThreadId:00000} - {logName} - {splitString}";
                    compiledString += str + Environment.NewLine;

                    if (_toConsole)
                        Console.WriteLine(str);
                }

                System.IO.File.AppendAllText(logFilePath, compiledString);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"###########################################\r\n"+
                    $"Unable to log for {logName} to {logFilePath}.\r\n\r\n" +
                    $"Error Message:{ex.Message}\r\n\r\n" + 
                    $"Log Contents:{message}\r\n\r\n" + 
                    $"Calling Routine: {memberName} @ {memberLineNumber}\r\n" +
                    $"###########################################\r\n");
            }

            _semaphoreSlim.Release();
        }
    }
}

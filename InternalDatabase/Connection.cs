using SQLite.Net.Interop;
using System;
using System.IO;
using System.Linq;

//using SQLiteConnection = SQLite.SQLiteConnection;

namespace InternalDatabase
{
    public class Connection
    {
        public SQLite.SQLiteConnection DbConnection;
        public string DatabaseName;
        private readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars();

        public Connection(string name)
        {
            DatabaseName = name;

            var dbFileName = new string($"{DatabaseName}.db".Where(ch => !_invalidFileNameChars.Contains(ch)).ToArray());
            var databasePath = $"Data\\{dbFileName}";

            try
            {
                if (!Directory.Exists("Data"))
                    Directory.CreateDirectory("Data");

                if (!File.Exists(databasePath))
                {
                    System.Data.SQLite.SQLiteConnection.CreateFile(databasePath);
                }

                DbConnection = new SQLite.SQLiteConnection(databasePath); // (new SQLite.Net.Platform.Win32.SQLitePlatformWin32(), databasePath);

                GlobalLogger.Log4NetHandler.Log($"[DATABASE] DB Connection for {dbFileName} established!", GlobalLogger.Log4NetHandler.LogLevel.INFO);
            }
            catch (Exception ex)
            {
                GlobalLogger.Log4NetHandler.Log($"Database module failed during Connection creation", GlobalLogger.Log4NetHandler.LogLevel.ERROR, exception:ex);
            }
        }

        public Connection RegisterTable<T>()
        {
            if (DbConnection == null) throw new Exceptions.DatabaseNotConnected();
            DbConnection.CreateTable<T>();

            return this;
        }
    }
}
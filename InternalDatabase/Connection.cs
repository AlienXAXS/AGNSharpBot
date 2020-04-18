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
        private ISQLitePlatform sqlitePlatform;
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

                GlobalLogger.AdvancedLogger.AdvancedLoggerHandler.Instance.GetLogger().Log($"[DATABASE] New DB Connection for {dbFileName} established!");
            }
            catch (Exception ex)
            {
                //TODO: Catch the different types of exceptions here (io failure, sql failure, etc)
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
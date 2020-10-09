using System;
using System.IO;
using System.Linq;
using GlobalLogger;
using SQLite;

//using SQLiteConnection = SQLite.SQLiteConnection;

namespace InternalDatabase
{
    public class Connection
    {
        private readonly char[] _invalidFileNameChars = Path.GetInvalidFileNameChars();
        public string DatabaseName;
        public SQLiteConnection DbConnection;

        public Connection(string name)
        {
            DatabaseName = name;

            var dbFileName =
                new string($"{DatabaseName}.db".Where(ch => !_invalidFileNameChars.Contains(ch)).ToArray());
            var databasePath = $"Data\\{dbFileName}";

            try
            {
                if (!Directory.Exists("Data"))
                    Directory.CreateDirectory("Data");

                if (!File.Exists(databasePath)) System.Data.SQLite.SQLiteConnection.CreateFile(databasePath);

                DbConnection =
                    new SQLiteConnection(
                        databasePath); // (new SQLite.Net.Platform.Win32.SQLitePlatformWin32(), databasePath);

                Log4NetHandler.Log($"[DATABASE] DB Connection for {dbFileName} established!",
                    Log4NetHandler.LogLevel.INFO);
            }
            catch (Exception ex)
            {
                Log4NetHandler.Log("Database module failed during Connection creation", Log4NetHandler.LogLevel.ERROR,
                    exception: ex);
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
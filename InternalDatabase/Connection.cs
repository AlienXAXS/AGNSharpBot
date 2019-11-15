using System;
using System.IO;
using System.Linq;
using SQLiteConnection = SQLite.SQLiteConnection;

//using SQLiteConnection = SQLite.SQLiteConnection;

namespace InternalDatabase
{
    public class Connection
    {
        public SQLiteConnection DbConnection;
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
                    System.Data.SQLite.SQLiteConnection.CreateFile(databasePath);

                DbConnection = new SQLiteConnection(databasePath);
            }
            catch (Exception)
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
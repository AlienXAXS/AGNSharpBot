﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using SQLite;
using SQLitePCL;

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

            string dbFileName = new string($"{DatabaseName}.db".Where(ch => !_invalidFileNameChars.Contains(ch)).ToArray());
            string databasePath = $"Data\\{dbFileName}";

            try
            {
                if (!System.IO.Directory.Exists("Data"))
                    System.IO.Directory.CreateDirectory("Data");

                if (!System.IO.File.Exists(databasePath))
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
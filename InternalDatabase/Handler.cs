using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GlobalLogger;

namespace InternalDatabase
{
    public class Handler
    {
        private static readonly Handler _instance;
        public static Handler Instance = _instance ?? (_instance = new Handler());
        private readonly List<Connection> _connections = new List<Connection>();

        /// <summary>
        ///     Gets a connection from the current calling assembly name, dynamically creates a new database file and connection if
        ///     it does not exist.
        /// </summary>
        /// <returns></returns>
        public Connection GetConnection()
        {
            var callingName = Assembly.GetCallingAssembly().GetName().Name;
            return GetConnection(callingName) ?? NewConnection(callingName);
        }

        public Connection GetConnection(string connectionName)
        {
            return _connections.DefaultIfEmpty(null).FirstOrDefault(x => x.DatabaseName.Equals(connectionName));
        }

        public Connection NewConnection()
        {
            return NewConnection(Assembly.GetCallingAssembly().GetName().Name);
        }

        public Connection NewConnection(string connectionName)
        {
            var foundConnection = _connections?.DefaultIfEmpty(null)
                .FirstOrDefault(x => x?.DatabaseName == connectionName);

            if (foundConnection != null) return foundConnection;

            Log4NetHandler.Log($"SQLite database instance requested from {connectionName}",
                Log4NetHandler.LogLevel.INFO);

            _connections.Add(new Connection(connectionName));
            return _connections[_connections.Count - 1];
        }
    }
}
using Newtonsoft.Json;
using PermissionHandler.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using GlobalLogger;

namespace PermissionHandler
{
    internal class Database
    {
        // The db in memory
        private List<Node> _nodes = new List<Node>();

        // where the db is stored
        private const string StorageLocation = "db.json";

        public Database()
        {
            Load();
        }

        /// <summary>
        /// Adds a new permission node, but only the path
        /// </summary>
        /// <param name="path">The path string (Assembly.Namespace.Class.MethodName)</param>
        /// <returns>The created node</returns>
        public Node AddPermission(string path)
        {
            var existingPathNode = GetRootPermissionNode(path);
            if (existingPathNode != null) return existingPathNode;

            var newNode = new Node().AssignPath(path);
            _nodes.Add(newNode);
            Save();
            return newNode;
        }

        public List<Node> GetData()
        {
            return _nodes;
        }

        public Node AddPermission(string path, ulong owner, NodePermission permission, OwnerType ownerType)
        {
            // first check to see if this path node already exists, if so we must append to it.
            var existingPathNode = GetRootPermissionNode(path);
            if (existingPathNode != null)
            {
                existingPathNode.AssignOwner(owner, permission, ownerType);
                Save();
                return existingPathNode;
            }

            // It's a new node path, make it and assign the user.
            var newNode = new Node().AssignPath(path).AssignOwner(owner, permission, ownerType);

            _nodes.Add(newNode);
            Save();

            return newNode;
        }

        public Node UpdatePermission(string path, ulong owner, NodePermission permission, OwnerType ownerType)
        {
            // Find the root path node
            var node = GetRootPermissionNode(path);

            if (node == null)
                throw new Exception($"Unable to Update Permission for {owner}, unable to find root node called {path}");

            var updatedNode = node.UpdateOwner(owner, permission, ownerType);

            Save();
            return updatedNode;
        }

        public void RemovePermission(string path, ulong owner)
        {
            // Find the root path node
            var node = GetRootPermissionNode(path);
            if (node == null)
                throw new Exception($"Unable to Remove Permission for {owner}, unable to find root node called {path}");

            node.RemoveOwner(owner);
            Save();
        }

        private Node GetRootPermissionNode(string path)
        {
            return _nodes.Where(x => x.Path.Equals(path)).DefaultIfEmpty(null).FirstOrDefault();
        }

        #region Save / Load Functionality

        public void Load()
        {
            lock (_nodes)
            {
                try
                {
                    if (!System.IO.File.Exists(StorageLocation))
                        throw new Exception(
                            "Unable to load permissions database as the database has yet to be used, this error should go away once you use it");
                    _nodes = JsonConvert.DeserializeObject<List<Node>>(System.IO.File.ReadAllText(StorageLocation));
                }
                catch (Exception ex)
                {
                    Log4NetHandler.Log($"[THREAD {System.Threading.Thread.CurrentThread.ManagedThreadId}] Unable to load permission database",
                        Log4NetHandler.LogLevel.ERROR, exception:ex);
                }
            }
        }

        public void Save()
        {
            lock (_nodes)
            {
                try
                {
                    System.IO.File.WriteAllText(StorageLocation,
                        JsonConvert.SerializeObject(_nodes, Formatting.Indented));
                }
                catch (Exception ex)
                {
                    Log4NetHandler.Log(
                        $"[THREAD {System.Threading.Thread.CurrentThread.ManagedThreadId}] Unable to save permission database",
                        Log4NetHandler.LogLevel.ERROR, exception: ex);
                }
            }
        }

        #endregion Save / Load Functionality
    }
}
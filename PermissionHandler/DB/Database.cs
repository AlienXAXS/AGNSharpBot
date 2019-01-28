﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GlobalLogger;
using Newtonsoft.Json;
using PermissionHandler.DB;

namespace PermissionHandler
{
    class Database
    {
        // The db in memory
        private List<Node> _nodes = new List<Node>();

        // where the db is stored
        private const string StorageLocation = "db.json";

        public Database()
        {
            Load();
        }

        public Node AddPermission(string path, string owner, NodePermission permission, OwnerType ownerType)
        {
            // first check to see if this path node already exists, if so we must append to it.
            var existingPathNode = GetRootPermissionNode(path);
            if (existingPathNode != null)
            {
                existingPathNode.AssignOwner(owner, permission, ownerType);
                return existingPathNode;
            }

            // It's a new node path, make it and assign the user.
            var newNode = new Node().AssignPath(path).AssignOwner(owner, permission, ownerType);

            _nodes.Add(newNode);
            Save();

            return newNode;
        }

        public Node UpdatePermission(string path, string owner, NodePermission permission, OwnerType ownerType)
        {
            // Find the root path node
            var node = GetRootPermissionNode(path);

            if ( node == null )
                throw new Exception($"Unable to Update Permission for {owner}, unable to find root node called {path}");

            var updatedNode = node.UpdateOwner(owner, permission, ownerType);

            return updatedNode;
        }

        public void RemovePermission(string path, string owner)
        {
            // Find the root path node
            var node = GetRootPermissionNode(path);
            if (node == null)
                throw new Exception($"Unable to Remove Permission for {owner}, unable to find root node called {path}");

            node.RemoveOwner(owner);
        }

        private Node GetRootPermissionNode(string path)
        {
            return _nodes.Where(x => x.Path.Equals(path)).DefaultIfEmpty(null).FirstOrDefault();
        }

        #region Save / Load Functionality
        public void Load()
        {
            try
            {
                if ( !System.IO.File.Exists(StorageLocation) )
                    throw new Exception("Unable to load permissions database as the database has yet to be used, this error should go away once you use it");
                _nodes = JsonConvert.DeserializeObject<List<Node>>(System.IO.File.ReadAllText(StorageLocation));
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(
                    $"[THREAD {System.Threading.Thread.CurrentThread.ManagedThreadId}] Unable to load permission database, error follows:\r\n{ex.Message}\r\n{ex.StackTrace}",
                    Logger.LoggerType.ConsoleAndDiscord).Wait();
            }
        }

        public void Save()
        {
            try
            {
                System.IO.File.WriteAllText(StorageLocation, JsonConvert.SerializeObject(_nodes, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(
                    $"[THREAD {System.Threading.Thread.CurrentThread.ManagedThreadId}] Unable to save permission database, error follows:\r\n{ex.Message}\r\n{ex.StackTrace}",
                    Logger.LoggerType.ConsoleAndDiscord).Wait();
            }
        }
        #endregion
    }
}
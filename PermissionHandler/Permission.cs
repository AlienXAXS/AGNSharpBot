﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Discord.WebSocket;
using GlobalLogger;
using PermissionHandler.DB;

namespace PermissionHandler
{
    public class Permission
    {
        // Instance
        private static readonly Permission _instance;

        public static Permission Instance = _instance ?? (_instance = new Permission());

        private readonly Database _database = new Database();

        // Permission Type AdvancedLoggerHandler Stuff
        private readonly List<string> _registeredPermissionPaths = new List<string>();

        public void RegisterPermission<T>(Assembly assembly)
        {
            var t = typeof(T);

            //var namespaceClasses = assembly.GetTypes().Where(x => x.Namespace != null && x.Namespace.Equals(typeof(T).Namespace, StringComparison.Ordinal) && x.IsClass);

            foreach (var thisMethod in t.GetMethods())
            {
                var requiredAttributeFound =
                    thisMethod.GetCustomAttributesData().Any(x => x.AttributeType.Name.Equals("Command"));

                if (!requiredAttributeFound) continue;

                var pathName = $"{typeof(T)}.{thisMethod.Name}";
                _registeredPermissionPaths.Add(pathName);
                _database.AddPermission(pathName);

                Log4NetHandler.Log($"Dynamically registered a new permission path node: {pathName}",
                    Log4NetHandler.LogLevel.INFO);
            }
        }

        public List<string> GetRegisteredPermissionPaths()
        {
            return _registeredPermissionPaths;
        }

        public Node Add(string path, ulong owner, ulong guildId, NodePermission permission, OwnerType ownerType)
        {
            var foundPath = _database.GetData().DefaultIfEmpty(null).FirstOrDefault(x => x.Path.Equals(path));
            if (foundPath == null)
                throw new Exception("The supplied path is invalid");

            if (foundPath.Permissions.Any(x => x.Owner == owner))
                throw new Exception(
                    "The supplied user is already a member of this permission path, either modify them or remove them");

            return _database.AddPermission(path, owner, guildId, permission, ownerType);
        }

        public void Remove(string path, ulong owner, ulong guildId)
        {
            var foundPath = _database.GetData().DefaultIfEmpty(null).FirstOrDefault(x => x.Path.Equals(path));
            if (foundPath == null)
                throw new Exception("The supplied path is invalid");

            if (foundPath.Permissions.Where(x => x.Owner == owner).DefaultIfEmpty(null).FirstOrDefault() == null)
                throw new Exception("The supplied user is not a member of this permission path");

            _database.RemovePermission(path, owner, guildId);
        }

        public bool CheckPermission(SocketGuildUser sktUser,
            [CallerMemberName] string path = "")
        {
            if (sktUser.Roles.Any(x => x.Permissions.Administrator))
                return true;

            var permissionNode = _database.GetData().DefaultIfEmpty(null).FirstOrDefault(x => x.Path.Equals(path));
            if (permissionNode == null)
                return false;

            if (permissionNode.Permissions.Count == 0)
                return false;

            var canPermit = false;
            var foundExplicitRoleDeny = false;

            // Check users roles
            foreach (var role in sktUser.Roles)
            {
                var rolePerm = permissionNode.Permissions.DefaultIfEmpty(null)
                    .FirstOrDefault(x => x.Owner.Equals(role.Id) && x.GuildId.Equals(sktUser.Guild.Id));

                switch (rolePerm?.Permission)
                {
                    case NodePermission.Allow:
                        canPermit = true;
                        break;

                    case NodePermission.Deny:
                        foundExplicitRoleDeny = true;
                        break;
                }

                if (canPermit)
                    break;
            }

            // Find explicit user allow
            if (!canPermit)
                canPermit = permissionNode.Permissions
                    .Any(x => x.Owner.Equals(sktUser.Id) && x.GuildId.Equals(sktUser.Guild.Id) && x.Permission == NodePermission.Allow);

            // Find explicit user deny
            var foundExplicitDeny = permissionNode.Permissions
                .Any(x => x.Owner.Equals(sktUser.Id) && x.GuildId.Equals(sktUser.Guild.Id) && x.Permission == NodePermission.Deny);

            return canPermit && !foundExplicitDeny && !foundExplicitRoleDeny;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Discord.Commands;
using Discord.WebSocket;
using GlobalLogger;
using PermissionHandler.DB;

namespace PermissionHandler
{
    public class Permission
    {
        // Instance
        private static Permission _instance;
        public static Permission Instance = _instance ?? (_instance = new Permission());

        private readonly Database _database = new Database();

        // Permission Type Handler Stuff
        private readonly List<string> _registeredPermissionPaths = new List<string>();

        public void RegisterPermission<T>(Assembly assembly)
        {
            var t = typeof(T);

            //var namespaceClasses = assembly.GetTypes().Where(x => x.Namespace != null && x.Namespace.Equals(typeof(T).Namespace, StringComparison.Ordinal) && x.IsClass);

            foreach (var thisMethod in t.GetMethods())
            {
                var requiredAttributeFound =
                    thisMethod.GetCustomAttributesData().Any(x => x.AttributeType.Name.Equals("Command"));

                if (requiredAttributeFound)
                {
                    var pathName = $"{typeof(T)}.{thisMethod.Name}";
                    _registeredPermissionPaths.Add(pathName);
                    _database.AddPermission(pathName);
                    Logger.Instance.Log($"Dynamically registered a new permission path node: {pathName}",
                        Logger.LoggerType.ConsoleAndDiscord);
                }
            }
        }

        public List<string> GetRegisteredPermissionPaths()
        {
            return _registeredPermissionPaths;
        }

        public bool CheckPermission(SocketGuildUser sktUser,
            [System.Runtime.CompilerServices.CallerMemberName]
            string path = "")
        {
            if (sktUser.Roles.Any(x => x.Permissions.Administrator))
                return true;

            var permissionNode = _database.GetData().DefaultIfEmpty(null).FirstOrDefault(x => x.Path.Equals(path));
            if (permissionNode == null)
                throw new Exception(
                    $"Possible unknown permission path with {path}, user {sktUser.Username} attempted a command that I did not recognise");

            var canPermit = false;
            var foundExplicitDeny = false;

            // Check users roles
            foreach (var role in sktUser.Roles)
            {
                var rolePerm = permissionNode.Permissions.DefaultIfEmpty(null)
                    .FirstOrDefault(x => x.Owner.Equals(role.Id));
                switch (rolePerm.Permission)
                {
                    case NodePermission.Allow:
                        canPermit = true;
                        break;
                    case NodePermission.Deny:
                        foundExplicitDeny = true;
                        break;
                }
            }

            // Find explicit user deny
            foundExplicitDeny = permissionNode.Permissions
                .Any(x => x.Owner.Equals(sktUser.Id) && x.Permission == NodePermission.Deny);

            return canPermit && !foundExplicitDeny;
        }
    }
}


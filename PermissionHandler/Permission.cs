using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Discord.WebSocket;
using GlobalLogger;

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
                var requiredAttributeFound = thisMethod.GetCustomAttributesData().Any(x => x.AttributeType.Name.Equals("Command"));

                if (requiredAttributeFound)
                {
                    var pathName = $"{typeof(T)}.{thisMethod.Name}";
                    _registeredPermissionPaths.Add(pathName);
                    _database.AddPermission(pathName);
                    Logger.Instance.Log($"Dynamically registered a new permission path node: {pathName}", Logger.LoggerType.ConsoleAndDiscord).Wait();
                }
            }
        }

        public bool CheckPermission(SocketGuildUser sktUser,
            [System.Runtime.CompilerServices.CallerMemberName]
            string memberName = "")
        {
            if (sktUser.Roles.Any(x => x.Permissions.Administrator))
            {
                return true;
            }

            return false;
        }
    }
}


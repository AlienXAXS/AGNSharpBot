using System;
using System.Collections.Generic;
using System.Linq;

namespace PermissionHandler.DB
{
    public enum NodePermission
    {
        Allow,
        Deny
    }

    public enum OwnerType
    {
        User,
        Role
    }

    public class SubNode
    {
        public ulong Owner; // Who owns this permission
        public ulong GuildId;
        public OwnerType OwnerType;
        public NodePermission Permission; // The permission the owner has

        public SubNode(ulong owner, ulong guildId, NodePermission permission, OwnerType ownerType)
        {
            Owner = owner;
            GuildId = guildId;
            Permission = permission;
            OwnerType = ownerType;
        }
    }

    public class Node
    {
        public string Path;
        public List<SubNode> Permissions = new List<SubNode>();

        public Node AssignPath(string path)
        {
            Path = path;
            return this;
        }

        public Node AssignOwner(ulong owner, ulong guildId, NodePermission permission, OwnerType ownerType)
        {
            Permissions.Add(new SubNode(owner, guildId, permission, ownerType));
            return this;
        }

        public Node UpdateOwner(ulong owner, ulong guildId, NodePermission permission, OwnerType ownerType)
        {
            var ownerNode = Permissions.Where(x => x.Owner.Equals(owner)).DefaultIfEmpty(null).FirstOrDefault();

            if (ownerNode == null)
                throw new Exception(
                    $"Unable to find owner {owner} for path {Path}");

            ownerNode.Permission = permission;
            ownerNode.OwnerType = ownerType;
            ownerNode.GuildId = guildId;

            return this;
        }

        public Node RemoveOwner(ulong owner, ulong guildId)
        {
            // Removes all of the owner nodes from this permission path that match the owner param
            Permissions.RemoveAll(node => node.Owner.Equals(owner) && node.GuildId.Equals(guildId));
            return this;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public NodePermission Permission; // The permission the owner has
        public OwnerType OwnerType;

        public SubNode(ulong owner, NodePermission permission, OwnerType ownerType)
        {
            Owner = owner;
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

        public Node AssignOwner(ulong owner, NodePermission permission, OwnerType ownerType)
        {
            Permissions.Add(new SubNode(owner, permission, ownerType));
            return this;
        }

        public Node UpdateOwner(ulong owner, NodePermission permission, OwnerType ownerType)
        {
            var ownerNode = Permissions.Where(x => x.Owner.Equals(owner)).DefaultIfEmpty(null).FirstOrDefault();

            if (ownerNode == null)
                throw new Exception(
                    $"Unable to find owner {owner} for path {Path}");

            ownerNode.Permission = permission;
            ownerNode.OwnerType = ownerType;

            return this;
        }

        public Node RemoveOwner(ulong owner)
        {
            // Removes all of the owner nodes from this permission path that match the owner param
            Permissions.RemoveAll(node => node.Owner.Equals(owner));
            return this;
        }
    }
}

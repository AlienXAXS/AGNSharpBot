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

    class SubNode
    {
        public string Owner; // Who owns this permission
        public NodePermission Permission; // The permission the owner has
        public OwnerType OwnerType;

        public SubNode(string owner, NodePermission permission, OwnerType ownerType)
        {
            Owner = owner;
            Permission = permission;
            OwnerType = ownerType;
        }
    }

    class Node
    {
        public string Path;
        private List<SubNode> _nodes = new List<SubNode>();

        public Node AssignPath(string path)
        {
            Path = path;
            return this;
        }

        public Node AssignOwner(string owner, NodePermission permission, OwnerType ownerType)
        {
            _nodes.Add(new SubNode(owner, permission, ownerType));
            return this;
        }

        public Node UpdateOwner(string owner, NodePermission permission, OwnerType ownerType)
        {
            var ownerNode = _nodes.Where(x => x.Owner.Equals(owner)).DefaultIfEmpty(null).FirstOrDefault();

            if (ownerNode == null)
                throw new Exception(
                    $"Unable to find owner {owner} for path {Path}");

            ownerNode.Permission = permission;
            ownerNode.OwnerType = ownerType;

            return this;
        }

        public Node RemoveOwner(string owner)
        {
            // Removes all of the owner nodes from this permission path that match the owner param
            _nodes.RemoveAll(node => node.Owner.Equals(owner));
            return this;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameToRole.Admin
{
    internal class Command : Attribute
    {
        public string Value { get; set; }
        public string Description { get; set; }

        public Command(string commandString, string Description)
        {
            Value = commandString;
            this.Description = Description;
        }
    }

    internal class Permissions : Attribute
    {

        public PermissionTypes Value { get; set; }

        public enum PermissionTypes
        {
            Administrator,
            Guest
        }

        public Permissions(PermissionTypes permissions)
        {
            Value = permissions;
        }
    }
}

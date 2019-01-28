using System;

namespace CommandHandler
{
    public class Command : Attribute
    {
        public string Value { get; set; }
        public string Description { get; set; }

        public Command(string commandString, string Description)
        {
            Value = commandString;
            this.Description = Description;
        }
    }

    public class Permissions : Attribute
    {
        public PermissionTypes Value { get; set; }

        public enum PermissionTypes
        {
            Guest
        }

        public Permissions(PermissionTypes permissions)
        {
            Value = permissions;
        }
    }

    public class Alias : Attribute
    {
        public string[] Value { get; set; }

        public Alias(params string[] aliases)
        {
            Value = aliases;
        }
    }
}

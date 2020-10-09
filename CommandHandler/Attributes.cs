using System;

namespace CommandHandler
{
    public class Command : Attribute
    {
        public Command(string commandString, string Description)
        {
            Value = commandString;
            this.Description = Description;
        }

        public string Value { get; set; }
        public string Description { get; set; }
    }

    public class Permissions : Attribute
    {
        public enum PermissionTypes
        {
            Guest
        }

        public Permissions(PermissionTypes permissions)
        {
            Value = permissions;
        }

        public PermissionTypes Value { get; set; }
    }

    public class Alias : Attribute
    {
        public Alias(params string[] aliases)
        {
            Value = aliases;
        }

        public string[] Value { get; set; }
    }
}
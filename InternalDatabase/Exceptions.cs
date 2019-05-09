using System;

namespace InternalDatabase
{
    public class Exceptions
    {
        public class DatabaseNotConnected : Exception
        { }

        public class TableNotCreated : Exception
        { }

        public class TableCreationFailure : Exception
        {
        }
    }
}
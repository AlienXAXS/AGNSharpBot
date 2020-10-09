using System;

namespace PUBGWeekly
{
    public class ExceptionOverloads
    {
        public class PlayerAlreadyRegistered : Exception
        {
        }

        public class PlayerNotFound : Exception
        {
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBGWeekly
{
    public class ExceptionOverloads
    {
        public class PlayerAlreadyRegistered : Exception
        { }

        public class PlayerNotFound : Exception
        {
        }
    }
}

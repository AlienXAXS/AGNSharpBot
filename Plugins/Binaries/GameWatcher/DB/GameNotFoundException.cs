using System;
using System.Collections.Generic;
using System.Text;

namespace GameWatcher.DB
{
    class GameNotFoundException : Exception
    {
        public GameNotFoundException()
        {
        }

        public GameNotFoundException(string message)
            : base(message)
        {
        }
    }
}

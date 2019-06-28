using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auditor.WebServer.Models.Messages
{
    class PostData
    {
        public string DatetimeRange_From { get; set; }
        public string DatetimeRange_To { get; set; }
        public string User { get; set; }
    }
}

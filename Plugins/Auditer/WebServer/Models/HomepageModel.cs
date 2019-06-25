﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auditor.WebServer.Models
{
    class HomepageModel
    {
        public int OnlineUsers { get; set; }
        public int OfflineUsers { get; set; }
        public long RecordedMessages { get; set; }
        public long DeletedMessagesSaved { get; set; }
        public long ImagesSaved { get; set; }
        public long AuditedUsers { get; set; }
        public long AuditedDataRowCount { get; set; }
        public string DatabaseFileSize { get; set; }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toxy.Common
{
    class ChatLogItem
    {
        public string PublicKey { get; set; }
        public string From { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Message { get; set; }
    }
}

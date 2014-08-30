﻿using System;
using System.IO;
using System.Threading;
using Toxy.Views;

namespace Toxy.Common
{
    public class MessageData
    {
        public MessageData()
        {
            TimeStamp = default(DateTime);
        }

        public string Username { get; set; }
        public string Message { get; set; }
        public int Id { get; set; }
        public bool IsAction { get; set; }
        public bool IsSelf { get; set; }
        public DateTime TimeStamp { get; set; }
    }

    public class FileTransfer
    {
        public Thread Thread { get; set; }
        public int FriendNumber { get; set; }
        public int FileNumber { get; set; }
        public ulong FileSize { get; set; }
        public string FileName { get; set; }
        public Stream Stream { get; set; }
        public bool Finished { get; set; }
        public bool IsSender { get; set; }

        public FileTransferControl Control { get; set; }
    }
}

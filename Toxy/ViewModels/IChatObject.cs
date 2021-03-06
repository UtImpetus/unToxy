﻿using SharpTox.Core;
using System;

namespace Toxy.ViewModels
{
    public interface IChatObject
    {
        string Name { get; set; }
        string PublicKey { get; set; }
        int ChatNumber { get; set; }
        bool Selected { get; set; }
        bool HasNewMessage { get; set; }
        int NewMessageCount { get; set; }
        string StatusMessage { get; set; }
        string AdditionalInfo { get; set; }
        ToxUserStatus ToxStatus { get; set; }
        bool Visible { get; set; }
        string GroupName { get; set; }
        Action<IFriendObject, string> MoveToContactGroupAction { get; set; }
    }
}
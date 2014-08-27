using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toxy.Common;

namespace Toxy.Utils
{
    public static class GroupChatHelpers
    {
        public static void AddNewGoupToConfig(string group_public_key, int friendNumber, Config configuation)
        {
            if (configuation.GroupChats == null)
            {
                configuation.GroupChats = new GroupChat[] { new GroupChat() { PublicKey = group_public_key, GroupNumber = -1,FriendNumber=friendNumber, Name = string.Format("Chat #{0}", group_public_key) } };
            }
            else
            {
                var groupList = new List<GroupChat>(configuation.GroupChats);
                groupList.Add(new GroupChat() { Name = string.Format("Chat #{0}", group_public_key), GroupNumber = -1, FriendNumber = friendNumber, PublicKey = group_public_key });
                configuation.GroupChats = groupList.ToArray();
            }
            ConfigTools.Save(configuation, "config.xml");
        }

        internal static void RemoveGroupFromConfig(Config config, string publicKey)
        {
            if (config.GroupChats != null)
            {
                var groupList = new List<GroupChat>(config.GroupChats);
                groupList.RemoveAll(v=>v.PublicKey==publicKey);
                config.GroupChats = groupList.ToArray();
                ConfigTools.Save(config, "config.xml");
            }
        }

        internal static void RenameGroup(Config config, string publicKey, string newName)
        {
            if (config.GroupChats != null)
            {
                var group = config.GroupChats.FirstOrDefault(v=>v.PublicKey==publicKey);
                if (group != null)
                {
                    group.Name = newName;
                    ConfigTools.Save(config, "config.xml");
                }
            }
        }
    }
}
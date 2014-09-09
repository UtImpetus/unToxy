using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toxy.Common
{
    public static class Logger
    {
        static string lastmessage = string.Empty;
        public static void LogException(Exception ex)
        {
            if (ex.Message == lastmessage)
            {
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " +1");
            }
            else
            {
                File.AppendAllText("log.txt", DateTime.Now.ToString() + " " + ex.ToString());
            }
            lastmessage = ex.Message;
        }
    }
}

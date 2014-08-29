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
        public static void LogException(Exception ex)
        {
            File.AppendAllText("log.txt", DateTime.Now.ToString() + " " + ex.ToString());
        }
    }
}

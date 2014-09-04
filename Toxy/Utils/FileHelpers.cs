using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toxy.Utils
{
    public static class FileHelpers
    {
        public static string GetUniqueFilename(string newFileName)
        {
            if (File.Exists(newFileName))
            {
                var fileCount = 0;
                do
                {
                    fileCount++;
                    var ext = Path.GetExtension(newFileName);
                    var fileWithoutExt = newFileName.Remove(newFileName.Length - ext.Length);
                    newFileName = fileWithoutExt + "(" + fileCount.ToString() + ")" + ext;
                }
                while (File.Exists(newFileName));
            }
            return newFileName;
        }
    }
}

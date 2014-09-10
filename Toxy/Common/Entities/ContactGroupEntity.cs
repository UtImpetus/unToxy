using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toxy.Common
{
    [Serializable]
    public class ContactGroupEntity
    {
        public string PublicKey { get; set; }
        public string GroupName { get; set; }
    }
}

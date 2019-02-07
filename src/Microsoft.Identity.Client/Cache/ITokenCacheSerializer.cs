using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Cache
{
    internal interface ITokenCacheSerializer
    {
        void Deserialize(byte[] bytes);
        byte[] Serialize();
    }
}

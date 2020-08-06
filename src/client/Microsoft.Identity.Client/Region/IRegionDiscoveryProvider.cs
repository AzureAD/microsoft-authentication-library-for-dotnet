using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Region
{
    internal interface IRegionDiscoveryProvider
    {
        Task<string> getRegionAsync();
    }
}

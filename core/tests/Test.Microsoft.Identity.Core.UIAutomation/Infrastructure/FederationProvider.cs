using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Microsoft.Identity.Core.UIAutomation.infrastructure
{
    public enum FederationProvider
    {
        Unknown = 0,
        None = 1,
        AdfsV2 = 2,
        AdfsV3 = 3,
        AdfsV4 = 4,
        PingFederateV83 = 5,
        Shibboleth = 6
    }
}

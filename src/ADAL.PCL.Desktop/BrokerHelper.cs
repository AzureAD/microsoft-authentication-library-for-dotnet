using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    class BrokerHelper : IBrokerHelper
    {
        public bool SkipBroker { get; set; }
        public IPlatformParameters PlatformParameters { get; set; }

        public bool CanInvokeBroker {
            get { return false; } 
        }

        public Task<AuthenticationResultEx> AcquireTokenUsingBroker(IDictionary<string, string> brokerPayload)
        {
            throw new NotImplementedException();
        }

        public Task<AuthenticationResultEx> AcquireTokenSilentUsingBroker(IDictionary<string, string> brokerPayload)
        {
            throw new NotImplementedException();
        }
    }
}

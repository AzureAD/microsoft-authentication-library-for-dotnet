using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal static class SilentAuthStrategyFactory
    {
        public static ISilentAuthStrategy GetSilentAuthStrategy(SilentRequest request, IServiceBundle servicebundle, AuthenticationRequestParameters parameters, AcquireTokenSilentParameters silentParameters)
        {
            if (parameters.IsBrokerConfigured && servicebundle.PlatformProxy.CanBrokerSupportSilentAuth())
            {
                return new SilentBrokerAuthStretegy(request, servicebundle, parameters, silentParameters);
            }
            else
            {
                return new SilentClientAuthStretegy(request, servicebundle, parameters, silentParameters);
            }
        }
    }
}

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;

namespace Test.ADAL.NET.Unit
{
    public class PlatformParametersFactory
    {
        public static IPlatformParameters CreateDefault()
        {
#if DESKTOP
            return new PlatformParameters(PromptBehavior.Auto);
#elif NET_CORE
            return new PlatformParameters();
#else
            throw new NotImplementedException(nameof(PlatformParametersFactory));
#endif
        }
    }
}

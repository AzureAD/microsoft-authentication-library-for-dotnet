using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Core.Instance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test.Microsoft.Identity.Core.Unit.Mocks;

namespace Test.MSAL.NET.Unit
{
    internal class TestCommon
    {
        public static void ResetStateAndInitMsal()
        {
            ModuleInitializer.ForceModuleInitializationTestOnly();
            ResetState();
        }

        public static void ResetState()
        {
            Authority.ValidatedAuthorities.Clear();
            AadInstanceDiscovery.Instance.Cache.Clear();

            Logger.LogCallback = null;
            Logger.PiiLoggingEnabled = false;
            Logger.Level = LogLevel.Info;
            Logger.DefaultLoggingEnabled = false;

        }

        public static void MockInstanceDiscoveryAndOpenIdRequest(MockHttpManager mockHttpManager)
        {
            mockHttpManager.AddInstanceDiscoveryMockHandler();
            mockHttpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);
        }

    }
}

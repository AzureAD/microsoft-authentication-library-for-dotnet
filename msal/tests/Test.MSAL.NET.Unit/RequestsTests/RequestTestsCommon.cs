using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Core.Http;
using Microsoft.Identity.Core.Instance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Test.Microsoft.Identity.Core.Unit.Mocks;

namespace Test.MSAL.NET.Unit.RequestsTests
{
    public class RequestTestsCommon
    {

        public static void MockInstanceDiscoveryAndOpenIdRequest()
        {
            HttpMessageHandlerFactory.AddMockHandler(
                MockHelpers.CreateInstanceDiscoveryMockHandler(
                TestConstants.GetDiscoveryEndpoint(TestConstants.AuthorityCommonTenant)));

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });
        }

        public static void InitializeRequestTests()
        {
            ModuleInitializer.ForceModuleInitializationTestOnly();
            Authority.ValidatedAuthorities.Clear();
            AadInstanceDiscovery.Instance.Cache.Clear();
            HttpClientFactory.ReturnHttpClientForMocks = true;
            HttpMessageHandlerFactory.ClearMockHandlers();
        }
    }
}

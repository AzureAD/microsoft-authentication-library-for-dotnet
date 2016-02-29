using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.MSAL.NET.Unit.Mocks;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    public class UserRealDiscoveryResponseTests
    {
        [TestMethod]
        [TestCategory("UserRealDiscoveryResponseTests")]
        public void NullRealmUriTest()
        {
            Task<UserRealmDiscoveryResponse> task = null;
            try
            {
                task = UserRealmDiscoveryResponse.CreateByDiscoveryAsync(null,
                    "some@user.com", null);
                task.Wait();
                Assert.Fail("UriFormatException should be thrown here.");
            }
            catch (AggregateException ae)
            {
                Assert.IsTrue(ae.InnerException is UriFormatException);
            }
        }

        [TestMethod]
        [TestCategory("UserRealDiscoveryResponseTests")]
        public void FederatedUserTest()
        {
            MockHttpMessageHandler mockHandler = new MockHttpMessageHandler();
            string response = "{\"ver\":\"1.0\",\"account_type\":\"Federated\",\"domain_name\":\"microsoft.com\",\"federation_protocol\":\"WSTrust\",\"federation_metadata_url\":\"https://msft.sts.microsoft.com/adfs/services/trust/mex\",\"federation_active_auth_url\":\"https://msft.sts.microsoft.com/adfs/services/trust/2005/usernamemixed\"}";
            mockHandler.ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response)
            };

            mockHandler.Method = HttpMethod.Get;
            mockHandler.Url = "https://someurl.com/someone@microsoft.com";
            mockHandler.QueryParams=new Dictionary<string,string>() { { "api-version", "1.0" } };

            HttpMessageHandlerFactory.MockHandler = mockHandler;
            Task<UserRealmDiscoveryResponse> task =  UserRealmDiscoveryResponse.CreateByDiscoveryAsync("https://someurl.com/",
                    "someone@microsoft.com", null);
                task.Wait();
            UserRealmDiscoveryResponse discoveryResponse = task.Result;
            Assert.IsNotNull(discoveryResponse);
            Assert.AreEqual("Federated", discoveryResponse.AccountType);
            Assert.AreEqual("https://msft.sts.microsoft.com/adfs/services/trust/2005/usernamemixed", discoveryResponse.FederationActiveAuthUrl);
            Assert.AreEqual("https://msft.sts.microsoft.com/adfs/services/trust/mex", discoveryResponse.FederationMetadataUrl);
            Assert.AreEqual("WSTrust", discoveryResponse.FederationProtocol);
            Assert.AreEqual("1.0", discoveryResponse.Version);
        }

        [TestMethod]
        [TestCategory("UserRealDiscoveryResponseTests")]
        public void LiveIdUserTest()
        {
            MockHttpMessageHandler mockHandler = new MockHttpMessageHandler();
            string response = "{\"ver\":\"1.0\",\"account_type\":\"Federated\",\"domain_name\":\"live.com\",\"federation_protocol\":\"WSTrust\",\"federation_metadata_url\":\"\",\"federation_active_auth_url\":\"https://login.live.com/rst2.srf\"}";
            mockHandler.ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response)
            };

            mockHandler.Method = HttpMethod.Get;
            mockHandler.Url = "https://someurl.com/someone@live.com";
            mockHandler.QueryParams = new Dictionary<string, string>() { { "api-version", "1.0" } };

            HttpMessageHandlerFactory.MockHandler = mockHandler;
            Task<UserRealmDiscoveryResponse> task = UserRealmDiscoveryResponse.CreateByDiscoveryAsync("https://someurl.com/",
                "someone@live.com", null);
            task.Wait();
            UserRealmDiscoveryResponse discoveryResponse = task.Result;
            Assert.IsNotNull(discoveryResponse);
            Assert.AreEqual("Federated", discoveryResponse.AccountType);
            Assert.AreEqual("https://login.live.com/rst2.srf", discoveryResponse.FederationActiveAuthUrl);
            Assert.AreEqual(string.Empty, discoveryResponse.FederationMetadataUrl);
            Assert.AreEqual("WSTrust", discoveryResponse.FederationProtocol);
            Assert.AreEqual("1.0", discoveryResponse.Version);
        }


        [TestMethod]
        [TestCategory("UserRealDiscoveryResponseTests")]
        public void ManagedUserTest()
        {
            MockHttpMessageHandler mockHandler = new MockHttpMessageHandler();
            string response = "{\"ver\":\"1.0\",\"account_type\":\"Managed\",\"domain_name\":\"managed.onmicrosoft.com\"}";
            mockHandler.ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response)
            };

            mockHandler.Method = HttpMethod.Get;
            mockHandler.Url = "https://someurl.com/someone@managed.onmicrosoft.com";
            mockHandler.QueryParams = new Dictionary<string, string>() { { "api-version", "1.0" } };

            HttpMessageHandlerFactory.MockHandler = mockHandler;
            Task<UserRealmDiscoveryResponse> task = UserRealmDiscoveryResponse.CreateByDiscoveryAsync("https://someurl.com/",
                "someone@managed.onmicrosoft.com", null);
            task.Wait();
            UserRealmDiscoveryResponse discoveryResponse = task.Result;
            Assert.IsNotNull(discoveryResponse);
            Assert.AreEqual("Managed", discoveryResponse.AccountType);
            Assert.IsNull(discoveryResponse.FederationActiveAuthUrl);
            Assert.IsNull(discoveryResponse.FederationMetadataUrl);
            Assert.IsNull(discoveryResponse.FederationProtocol);
            Assert.AreEqual("1.0", discoveryResponse.Version);
        }
    }
}

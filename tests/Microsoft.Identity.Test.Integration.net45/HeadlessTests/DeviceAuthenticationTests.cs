using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Integration.net45.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Microsoft.Identity.Test.Integration.net45.HeadlessTests
{
    [TestClass]
    public class DeviceAuthenticationTests
    {
        private const string _claims = "{\"access_token\":{\"deviceid\":{\"essential\":true}}}";
        private const string _publicClientId = "4b0db8c2-9f26-4417-8bde-3f0e3656f8e0";
        private const string _deviceAuthuser = "idlabca@msidlab8.onmicrosoft.com";

#if DESKTOP
        [TestMethod]
#if !IS_WIN8_TESTRUN
        [Ignore]
#endif
        public void PKeyAuthNonInteractiveTest()
        {
            //Arrange
            var msalPublicClient = PublicClientApplicationBuilder
                .Create(_publicClientId)
                .WithTestLogging()
                .Build();

            //Act
            var authResult = msalPublicClient.AcquireTokenByUsernamePassword(TestConstants.s_scope, _deviceAuthuser, new NetworkCredential("", LabUserHelper.FetchUserPassword("msidlab8")).SecurePassword)
            .WithClaims(JObject.Parse(_claims).ToString())
            .ExecuteAsync(CancellationToken.None).Result;

            //Assert
            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            Assert.IsTrue(string.Equals(_deviceAuthuser, authResult.Account.Username, StringComparison.InvariantCultureIgnoreCase));
        }
#endif
    }
}

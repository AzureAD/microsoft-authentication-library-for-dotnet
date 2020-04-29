// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Threading;
using Microsoft.Identity.Client;
using Microsoft.Identity.Json.Linq;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.net45.HeadlessTests
{
    [TestClass]
    public class DeviceAuthenticationTests
    {
        private const string _claims = "{\"access_token\":{\"deviceid\":{\"essential\":true}}}";
        private const string _publicClientId = "4b0db8c2-9f26-4417-8bde-3f0e3656f8e0";
        private const string _deviceAuthuser = "idlabca@msidlab8.onmicrosoft.com";

        [TestMethod]

        public void PKeyAuthNonInteractiveTest()
        {
            //Arrange
            var msalPublicClient = PublicClientApplicationBuilder
                .Create(_publicClientId)
                .Build();

            //Act
            var authResult = msalPublicClient.AcquireTokenByUsernamePassword(new[] { "user.read" }, _deviceAuthuser, new NetworkCredential("", LabUserHelper.FetchUserPassword("msidlab8")).SecurePassword)
            .WithClaims(JObject.Parse(_claims).ToString())
            .ExecuteAsync(CancellationToken.None).Result;

            //Assert
            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            Assert.IsTrue(string.Equals(_deviceAuthuser, authResult.Account.Username, StringComparison.InvariantCultureIgnoreCase));
        }

    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.Identity.Client;
using Microsoft.Identity.Json.Linq;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.net45.HeadlessTests
{
    [TestClass]
    public class DeviceAuthenticationTests
    {
        private const string _claims = "{\"access_token\":{\"deviceid\":{\"essential\":true}}}";
        private const string _deviceAuthuser = "idlabca@msidlab8.onmicrosoft.com";

        [TestMethod]

        public async void PKeyAuthNonInteractiveTestAsync()
        {
            if (Environment.OSVersion.Version.Major > 10)
            {
                //This test cannot run on Windows 10 or greater
                Assert.Inconclusive();
            }

            //Arrange
            var labResponse = await LabUserHelper.GetSpecificUserAsync(_deviceAuthuser).ConfigureAwait(false);
            var factory = new HttpSnifferClientFactory();
            var msalPublicClient = PublicClientApplicationBuilder
                .Create(labResponse.App.AppId)
                .WithHttpClientFactory(factory)
                .Build();

            //Act
            var authResult = msalPublicClient.AcquireTokenByUsernamePassword(new[] { "user.read" }, _deviceAuthuser, new NetworkCredential("", labResponse.User.GetOrFetchPassword()).SecurePassword)
            .WithClaims(JObject.Parse(_claims).ToString())
            .ExecuteAsync(CancellationToken.None).Result;

            //Assert
            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            Assert.IsTrue(string.Equals(_deviceAuthuser, authResult.Account.Username, StringComparison.InvariantCultureIgnoreCase));

            var (req, res) = factory.RequestsAndResponses
                .Where(x => x.Item1.RequestUri.AbsoluteUri == labResponse.Lab.Authority + "organizations/oauth2/v2.0/token" 
                         && x.Item2.StatusCode == HttpStatusCode.OK).ElementAt(1);

            var AuthHeader = req.Headers.Single(h => h.Key == "Authorization").Value.FirstOrDefault();

            Assert.IsTrue(!string.IsNullOrEmpty(AuthHeader));
            Assert.IsTrue(AuthHeader.Contains("PKeyAuth"));
        }

    }
}

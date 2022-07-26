// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Json.Linq;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.Win8
{
    [TestClass]
    public class DeviceAuthenticationTests
    {
        private const string _claims = "{\"access_token\":{\"deviceid\":{\"essential\":true}}}";
        private const string _deviceAuthuser = "idlabca@msidlab8.onmicrosoft.com";

        
        [TestMethod]
        public async Task PKeyAuthNonInteractiveTestAsync()
        {
            //Arrange
            var labResponse = await LabUserHelper.GetSpecificUserAsync(_deviceAuthuser).ConfigureAwait(false);
            var factory = new HttpSnifferClientFactory();
            var msalPublicClient = PublicClientApplicationBuilder
                .Create(labResponse.App.AppId)
                .WithAuthority("https://login.microsoftonline.com/organizations/")
                .WithHttpClientFactory(factory)
                .Build();

            //Act
            var authResult = msalPublicClient.AcquireTokenByUsernamePassword(
                 new[] { "user.read" },
                 labResponse.User.Upn,
                 labResponse.User.GetOrFetchPassword())
             .WithClaims(JObject.Parse(_claims).ToString())
             .ExecuteAsync(CancellationToken.None).Result;

            //Assert
            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            Assert.IsTrue(string.Equals(_deviceAuthuser, authResult.Account.Username, StringComparison.InvariantCultureIgnoreCase));

            //Assert that the PKeyAuth header is used and the token response is successful
            var (req, res) = factory.RequestsAndResponses
                .Where(x => x.Item1.Headers.Authorization != null
                && x.Item1.Headers.Authorization.Scheme.Contains(PKeyAuthConstants.PKeyAuthName)
                && x.Item2.StatusCode == HttpStatusCode.OK).FirstOrDefault();

            Assert.IsNotNull(req);
            Assert.IsNotNull(res);
        }
    }
}

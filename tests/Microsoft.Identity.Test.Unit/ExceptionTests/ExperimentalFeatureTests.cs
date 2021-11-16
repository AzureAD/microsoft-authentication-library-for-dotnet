// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ExceptionTests
{
    [TestClass]
    public class ExperimentalFeatureTests
    {
#if DESKTOP
        [TestMethod]
        public async Task ExperimentalFeatureExceptionAsync()
        {
            var cca = ConfidentialClientApplicationBuilder.Create(Guid.NewGuid().ToString()).WithClientSecret("some-secret").Build();
            MsalClientException ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => cca.AcquireTokenForClient(new[] { "scope" }).WithProofOfPossession(null).ExecuteAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(MsalError.ExperimentalFeature, ex.ErrorCode);
        }
#endif

        [TestMethod]
        public async Task HybridSpaExperimentalFeatureExceptionAsync()
        {
            var cca = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                .WithRedirectUri(TestConstants.RedirectUri)
                .WithClientSecret(TestConstants.ClientSecret)
                .BuildConcrete();

            MsalClientException ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                () => cca.AcquireTokenByAuthorizationCode(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                        .WithSpaAuthorizationCode(true).ExecuteAsync())
                        .ConfigureAwait(false);
            
            Assert.AreEqual(MsalError.ExperimentalFeature, ex.ErrorCode);
        }
    }
}

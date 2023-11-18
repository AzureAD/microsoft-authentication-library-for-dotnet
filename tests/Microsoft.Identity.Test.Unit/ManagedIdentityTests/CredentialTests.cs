// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
#if !DESKTOP
    [TestClass]
#endif
    public class CredentialTests : TestBase
    {
        internal const string CredentialEndpoint = "http://169.254.169.254/metadata/identity/credential";
        internal const string MtlsEndpoint = "https://centraluseuap.mtlsauth.microsoft.com/72f988bf-86f1-41af-91ab-2d7cd011db47/oauth2/v2.0/token";
        internal const string AzureResource = "http://vault.azure.net";
        
        [TestMethod]
        public async Task CredentialHappyPathAsync()
        {
            using (new SoftwareKeyProvider())
            using (var httpManager = new MockHttpManager(isManagedIdentity: true))
            {
                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithExperimentalFeatures(true)
                    .WithClientCapabilities(new string[] { "CP1" })
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                IManagedIdentityApplication mi = miBuilder.Build();

                httpManager.AddManagedIdentityCredentialMockHandler(
                    CredentialEndpoint,
                    sendHeaders: true,
                    MockHelpers.GetSuccessfulCredentialResponse());          

                httpManager.AddManagedIdentityCredentialMockHandler(
                    MtlsEndpoint,
                    sendHeaders: false,
                    MockHelpers.GetSuccessfulMtlsResponse());

                var result = await mi.AcquireTokenForManagedIdentity(AzureResource).ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                result = await mi.AcquireTokenForManagedIdentity(AzureResource)
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

    }
}

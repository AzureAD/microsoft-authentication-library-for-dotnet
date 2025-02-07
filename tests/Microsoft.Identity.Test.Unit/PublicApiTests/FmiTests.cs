// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class FmiTests : TestBase
    {
        [TestMethod]
        public async Task FmiEnsureWithFmiPathFunctions()
        {
            using (var httpManager = new MockHttpManager())
            {
                var fmiClientId = "urn:microsoft:identity:fmi";
                var app = ConfidentialClientApplicationBuilder.Create(fmiClientId)
                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                              .WithRedirectUri(TestConstants.RedirectUri)
                                              .WithClientSecret(TestConstants.ClientSecret)
                                              .WithHttpManager(httpManager)
                                              .WithExperimentalFeatures()
                                              .BuildConcrete();

                var appCacheAccess = app.AppTokenCache.RecordAccess();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage( expectedPostData: new Dictionary<string, string>() { { OAuth2Parameter.FmiPath, "fmiPath" } });

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                        .WithFmiPath("fmiPath")
                                        .ExecuteAsync(CancellationToken.None)
                                        .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("header.payload.signature", result.AccessToken);
                Assert.AreEqual(fmiClientId, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().First().ClientId);

                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                        .WithFmiPath("fmiPath")
                                        .ExecuteAsync(CancellationToken.None)
                                        .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("header.payload.signature", result.AccessToken);
                Assert.AreEqual(fmiClientId, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().First().ClientId);
            }
        }
    }
}

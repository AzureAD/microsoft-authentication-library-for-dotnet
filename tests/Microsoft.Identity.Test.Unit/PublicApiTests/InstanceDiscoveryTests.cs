// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.RP;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.ClientCredential;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Microsoft.Identity.Client.Extensibility;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class InstanceDiscoveryTests : TestBase
    {
        [DataTestMethod]
        [DataRow("login.microsoftonline.com", DisplayName = "Public")]
        [DataRow("login.microsoftonline.us", DisplayName = "UsGov")]
        [DataRow("login.microsoftonline.de", DisplayName = "GermanyLegacy")]
        [DataRow("login.partner.microsoftonline.cn", DisplayName = "China")]
        [DataRow("login.sovcloud-identity.fr", DisplayName = "Fr")]
        [DataRow("login.sovcloud-identity.de", DisplayName = "De")]
        [DataRow("login.sovcloud-identity.sg", DisplayName = "Sg")]
        public async Task InstanceDiscoveryHappensOnKnownCloud(string discoveryHost)
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAuthority($"https://{discoveryHost}/tenant")
                                                              .WithRedirectUri(TestConstants.RedirectUri)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)                                      
                                                              .BuildConcrete();

                Uri expectedDiscoveryEndpoint = new Uri($"https://{discoveryHost}/tenant/discovery/instance");
                httpManager.AddInstanceDiscoveryMockHandler(customDiscoveryEndpoint: expectedDiscoveryEndpoint);
                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                handler.ExpectedUrl = $"https://{discoveryHost}/tenant/oauth2/v2.0/token";

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                
            }
        }

        [TestMethod]
        public async Task ConfidentialClientUsingSecretNoInstanceDiscoveryTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                              .WithRedirectUri(TestConstants.RedirectUri)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .WithInstanceDiscovery(false)
                                                              .BuildConcrete();

                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);
                Assert.IsNotNull("header.payload.signature", result.AccessToken);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());

                Assert.IsNotNull(app.UserTokenCache);
                Assert.IsNotNull(app.AppTokenCache);

                appCacheAccess.AssertAccessCounts(1, 1);
                userCacheAccess.AssertAccessCounts(0, 0);
            }
        }

        [TestMethod]
        [WorkItem(5545)] // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/5545
        public async Task NoInstanceDiscovery_AirgappedCould_TestAsync()
        {
            using (var httpManager = new MockHttpManager(disableInternalRetries: true))
            using (new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", TestConstants.Region);

                // Instance discovery explicitly disabled
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAuthority(TestConstants.AuthorityNotKnownTenanted)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                                                              .WithInstanceDiscovery(false)
                                                              .Build();

                // Direct token request (no instance discovery mock!)                
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                      .ExecuteAsync(CancellationToken.None)
                                      .ConfigureAwait(false);

                // Assert happens when httpManager disposes and checks for unconsumed handlers 
            }
        }

        [TestMethod]
        [WorkItem(5546)] // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/5546
        public async Task HttpErrorsInDiscoveryShouldBeIgnored_AirgappedCould_TestAsync()
        {
            using (var httpManager = new MockHttpManager(disableInternalRetries: true))
            using (new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("REGION_NAME", TestConstants.Region);

                // Instance discovery explicitly disabled
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAuthority(TestConstants.AuthorityNotKnownTenanted)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .WithAzureRegion(ConfidentialClientApplication.AttemptRegionDiscovery)
                                                              .Build();

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        ExceptionToThrow = new HttpRequestException("Simulated network error")
                    });
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                                      .ExecuteAsync(CancellationToken.None)
                                      .ConfigureAwait(false);

                // Assert happens when httpManager disposes and checks for unconsumed handlers 
            }
        }
    }
}

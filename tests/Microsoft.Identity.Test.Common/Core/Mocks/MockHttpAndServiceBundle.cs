// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal class MockHttpAndServiceBundle : IDisposable
    {
        public MockHttpAndServiceBundle(
            LogCallback logCallback = null,
            bool isExtendedTokenLifetimeEnabled = false,
            string authority = ClientApplicationBase.DefaultAuthority,
            string testName = null,
            bool isMultiCloudSupportEnabled = false,
            bool isInstanceDiscoveryEnabled = true,
            IPlatformProxy platformProxy = null)
        {
            HttpManager = new MockHttpManager(testName);
            ServiceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(
                HttpManager,
                logCallback: logCallback,
                isExtendedTokenLifetimeEnabled: isExtendedTokenLifetimeEnabled,
                authority: authority,
                isMultiCloudSupportEnabled: isMultiCloudSupportEnabled,
                isInstanceDiscoveryEnabled: isInstanceDiscoveryEnabled,
                platformProxy: platformProxy);
        }

        public IServiceBundle ServiceBundle { get; }
        public MockHttpManager HttpManager { get; }

        public void Dispose()
        {
            HttpManager.Dispose();
        }

        public AuthenticationRequestParameters CreateAuthenticationRequestParameters(
            string authority,
            IEnumerable<string> scopes = null,
            ITokenCacheInternal tokenCache = null,
            IAccount account = null,
            IDictionary<string, string> extraQueryParameters = null,
            string claims = null,
            ApiEvent.ApiIds apiId = ApiEvent.ApiIds.None,
            bool validateAuthority = false)
        {
            scopes ??= TestConstants.s_scope;
            tokenCache ??= new TokenCache(ServiceBundle, false);

            var commonParameters = new AcquireTokenCommonParameters
            {
                Scopes = scopes ?? TestConstants.s_scope,
                ExtraQueryParameters = extraQueryParameters ?? new Dictionary<string, string>(),
                Claims = claims,
                ApiId = apiId
            };

            var authorityObj = Authority.CreateAuthority(authority, validateAuthority);
            var requestContext = new RequestContext(ServiceBundle, Guid.NewGuid());
            AuthenticationRequestParameters authenticationRequestParameters =
                new AuthenticationRequestParameters(
                    ServiceBundle,
                    tokenCache,
                    commonParameters,
                    requestContext,
                    authorityObj)
                {
                    Account = account,
                };

            authenticationRequestParameters.RequestContext.ApiEvent = new ApiEvent(Guid.NewGuid());

            return authenticationRequestParameters;
        }
    }
}

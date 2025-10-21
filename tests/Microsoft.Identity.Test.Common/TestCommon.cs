// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Instance.Oidc;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.Kerberos;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common.Core.Mocks;
using NSubstitute;
using static Microsoft.Identity.Client.TelemetryCore.Internal.Events.ApiEvent;
using Microsoft.Identity.Client.Http.Retry;

namespace Microsoft.Identity.Test.Common
{
    internal static class TestCommon
    {
        public static void ResetInternalStaticCaches()
        {
            // This initializes the classes so that the statics inside them are fully initialized, and clears any cached content in them.
            new InstanceDiscoveryManager(
                Substitute.For<IHttpManager>(),
                true, null, null);
            OidcRetrieverWithCache.ResetCacheForTest();
            AuthorityManager.ClearValidationCache();
            SingletonThrottlingManager.GetInstance().ResetCache();
        }

        public static object GetPropValue(object src, string propName)
        {
            object result = null;
            try
            {
                result = src.GetType().GetProperty(propName).GetValue(src, null);
            }
            catch
            {
                Console.WriteLine($"Property with name {propName}");
            }

            return result;
        }

        public static IServiceBundle CreateServiceBundleWithCustomHttpManager(
            IHttpManager httpManager,
            LogCallback logCallback = null,
            string authority = ClientApplicationBase.DefaultAuthority,
            bool isExtendedTokenLifetimeEnabled = false,
            bool enablePiiLogging = false,
            string clientId = TestConstants.ClientId,
            bool clearCaches = true,
            bool validateAuthority = true,
            bool isLegacyCacheEnabled = true,
            bool isMultiCloudSupportEnabled = false, 
            MsalClientType applicationType = MsalClientType.PublicClient,
            bool isInstanceDiscoveryEnabled = true,
            IPlatformProxy platformProxy = null)
        {
            var appConfig = new ApplicationConfiguration(applicationType)
            {
                ClientId = clientId,
                HttpManager = httpManager,
                RedirectUri = PlatformProxyFactory.CreatePlatformProxy(null).GetDefaultRedirectUri(clientId),
                LoggingCallback = logCallback,
                LogLevel = LogLevel.Verbose,
                EnablePiiLogging = enablePiiLogging,
                IsExtendedTokenLifetimeEnabled = isExtendedTokenLifetimeEnabled,
                Authority = Authority.CreateAuthority(authority, validateAuthority),
                LegacyCacheCompatibilityEnabled = isLegacyCacheEnabled,
                MultiCloudSupportEnabled = isMultiCloudSupportEnabled,
                IsInstanceDiscoveryEnabled = isInstanceDiscoveryEnabled,
                PlatformProxy = platformProxy,
                RetryPolicyFactory = new RetryPolicyFactory()
            };
            return new ServiceBundle(appConfig, clearCaches);
        }

        public static IServiceBundle CreateDefaultServiceBundle()
        {
            return CreateServiceBundleWithCustomHttpManager(null);
        }

        public static IServiceBundle CreateDefaultAdfsServiceBundle()
        {
            return CreateServiceBundleWithCustomHttpManager(null, authority: TestConstants.OnPremiseAuthority);
        }

        public static IServiceBundle CreateDefaultB2CServiceBundle()
        {
            return CreateServiceBundleWithCustomHttpManager(null, authority: TestConstants.B2CAuthority);
        }

        public static AuthenticationRequestParameters CreateAuthenticationRequestParameters(
            IServiceBundle serviceBundle,
            Authority authority = null,
            HashSet<string> scopes = null,
            RequestContext requestContext = null,
            ApiIds apiID = ApiIds.None)
        {
            var commonParameters = new AcquireTokenCommonParameters
            {
                Scopes = scopes ?? TestConstants.s_scope,
                ApiId = apiID
            };

            authority ??= Authority.CreateAuthority(TestConstants.AuthorityTestTenant);
            requestContext ??= new RequestContext(serviceBundle, Guid.NewGuid(), commonParameters.MtlsCertificate)
            {
                ApiEvent = new Client.TelemetryCore.Internal.Events.ApiEvent(Guid.NewGuid())
            };

            return new AuthenticationRequestParameters(
                serviceBundle,
                new TokenCache(serviceBundle, false),
                commonParameters,
                requestContext,
                authority)
            {
            };
        }

        public static KeyValuePair<string, IEnumerable<string>> GetCcsHeaderFromSnifferFactory(HttpSnifferClientFactory factory)
        {
            if (factory.RequestsAndResponses.Any())
            {
                var (req, res) = factory.RequestsAndResponses.Single(x => x.Item1.RequestUri.AbsoluteUri.Contains("oauth2/v2.0/token") &&
                x.Item2.StatusCode == HttpStatusCode.OK);

                return req.Headers.Single(h => h.Key == Constants.CcsRoutingHintHeader);
            }

            throw new MsalClientException("Could not find CCS Header in sniffer factory.");
        }

        public static bool YieldTillSatisfied(Func<bool> func, int maxTimeInMilliSec = 30000)
        {
            int iCount = maxTimeInMilliSec / 100;
            while (iCount > 0)
            {
                if (func())
                {
                    return true;
                }
                Thread.Yield();
                Thread.Sleep(100);
                iCount--;
            }

            return false;
        }

        public static MsalAccessTokenCacheItem UpdateATWithRefreshOn(
            ITokenCacheAccessor accessor,
            DateTimeOffset? refreshOn = null,
            bool expired = false)
        {
            MsalAccessTokenCacheItem atItem = accessor.GetAllAccessTokens().Single();

            refreshOn ??= DateTimeOffset.UtcNow - TimeSpan.FromMinutes(30);

            atItem = atItem.WithRefreshOn(refreshOn);

            Assert.IsTrue(atItem.ExpiresOn > DateTime.UtcNow + TimeSpan.FromMinutes(10));

            if (expired)
            {
                atItem = atItem.WithExpiresOn(DateTime.UtcNow - TimeSpan.FromMinutes(1));
            }

            accessor.SaveAccessToken(atItem);

            return atItem;
        }
    }
}

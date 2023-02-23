// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.CacheImpl;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;
using static Microsoft.Identity.Client.TelemetryCore.Internal.Events.ApiEvent;
 
namespace Microsoft.Identity.Client
{
    /// <inheritdoc />
    public abstract partial class ApplicationBase : IApplicationBase
    {
        /// <summary>
        /// Default Authority used for interactive calls.
        /// </summary>
        internal const string DefaultAuthority = "https://login.microsoftonline.com/common/";

        internal IServiceBundle ServiceBundle { get; }

        /// <summary>
        /// Details on the configuration of the ClientApplication for debugging purposes.
        /// </summary>
        public IAppConfig AppConfig => ServiceBundle.Config;

        /// <summary>
        /// User token cache. It holds access tokens, id tokens and refresh tokens for accounts. It's used
        /// and updated silently if needed when calling <see cref="AcquireTokenSilent(IEnumerable{string}, IAccount)"/>
        /// or one of the overrides of <see cref="AcquireTokenSilent(IEnumerable{string}, IAccount)"/>.
        /// It is updated by each AcquireTokenXXX method, with the exception of <c>AcquireTokenForClient</c> which only uses the application
        /// cache (see <c>IConfidentialClientApplication</c>).
        /// </summary>
        /// <remarks>On .NET Framework and .NET Core you can also customize the token cache serialization.
        /// See https://aka.ms/msal-net-token-cache-serialization. This is taken care of by MSAL.NET on mobile platforms and on UWP.
        /// It is recommended to use token cache serialization for web site and web api scenarios.
        /// </remarks>
        public ITokenCache UserTokenCache => UserTokenCacheInternal;

        internal ITokenCacheInternal UserTokenCacheInternal { get; }

        internal ApplicationBase(ApplicationConfiguration config)
        {
            ServiceBundle = Internal.ServiceBundle.Create(config);
            ICacheSerializationProvider defaultCacheSerialization = ServiceBundle.PlatformProxy.CreateTokenCacheBlobStorage();

            if (config.UserTokenLegacyCachePersistenceForTest != null)
            {
                UserTokenCacheInternal = new TokenCache(ServiceBundle, config.UserTokenLegacyCachePersistenceForTest, false, defaultCacheSerialization);
            }
            else
            {
                UserTokenCacheInternal = config.UserTokenCacheInternalForTest ?? new TokenCache(ServiceBundle, false, defaultCacheSerialization);
            }
        }

        internal virtual async Task<AuthenticationRequestParameters> CreateRequestParametersAsync(
            AcquireTokenCommonParameters commonParameters,
            RequestContext requestContext,
            ITokenCacheInternal cache)
        {
            var authority = await Instance.Authority.CreateAuthorityForRequestAsync(
               requestContext,
               commonParameters.AuthorityOverride).ConfigureAwait(false);

            return new AuthenticationRequestParameters(
                ServiceBundle,
                cache,
                commonParameters,
                requestContext,
                authority);
        }

        internal static void GuardMobileFrameworks()
        {
#if ANDROID || iOS || WINDOWS_APP || MAC
            throw new PlatformNotSupportedException(
                "Confidential Client flows are not available on mobile platforms or on Mac." +
                "See https://aka.ms/msal-net-confidential-availability for details.");
#endif
        }
    }
}

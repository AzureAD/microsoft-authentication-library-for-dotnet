// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client
{
    /// <inheritdoc/>
    public abstract class ApplicationBase : IApplicationBase
    {
        /// <summary>
        /// Default authority used for interactive calls.
        /// </summary>
        internal const string DefaultAuthority = "https://login.microsoftonline.com/common/";

        internal IServiceBundle ServiceBundle { get; }

        internal ApplicationBase(ApplicationConfiguration config)
        {
            ServiceBundle = Internal.ServiceBundle.Create(config);  
        }

        internal virtual async Task<AuthenticationRequestParameters> CreateRequestParametersAsync(
            AcquireTokenCommonParameters commonParameters,
            RequestContext requestContext,
            ITokenCacheInternal cache)
        {
            Instance.Authority authority = await Instance.Authority.CreateAuthorityForRequestAsync(
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
#if ANDROID || iOS || MAC
            throw new PlatformNotSupportedException(
                "Confidential client and managed identity flows are not available on mobile platforms and on Mac." +
                "See https://aka.ms/msal-net-confidential-availability and https://aka.ms/msal-net-managed-identity for details.");
#endif
        }
    }
}

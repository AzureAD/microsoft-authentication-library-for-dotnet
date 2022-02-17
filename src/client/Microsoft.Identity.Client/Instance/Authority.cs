// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance
{
    /// <remarks>
    /// Must be kept immutable
    /// </remarks>
    [DebuggerDisplay("{AuthorityInfo.CanonicalAuthority}")]
    internal abstract class Authority
    {
        protected Authority(AuthorityInfo authorityInfo)
        {
            if (authorityInfo == null)
            {
                throw new ArgumentNullException(nameof(authorityInfo));
            }

            // Don't reuse the same authority info, instead copy it
            // to prevent objects updating each other's details
            AuthorityInfo = new AuthorityInfo(authorityInfo);
        }

        public AuthorityInfo AuthorityInfo { get; }

        #region Builders

        /// <summary>
        /// Figures out the authority based on the authority from the config and the authority from the request,
        /// and optionally the homeAccountTenantId, which has an impact on AcquireTokenSilent
        ///
        /// The algorithm is:
        ///
        /// 1. If there is no request authority (i.e. no authority override), use the config authority.
        ///     1.1. For AAD, if the config authority is "common" etc, try to use the tenanted version with the home account tenant ID
        /// 2. If there is a request authority, try to use it.
        ///     2.1. If the request authority is not "common", then use it
        ///     2.2  If the request authority is "common", ignore it, and use 1.1
        ///
        /// Special cases:
        ///
        /// - if the authority is not defined at the application level and the request level is not AAD, use the request authority
        /// - if the authority is defined at app level, and the request level authority of is of different type, throw an exception
        ///
        /// </summary>
        public static async Task<Authority> CreateAuthorityForRequestAsync(
            RequestContext requestContext,
            AuthorityInfo requestAuthorityInfo,
            IAccount account = null)
        {
            var configAuthorityInfo = requestContext.ServiceBundle.Config.Authority.AuthorityInfo;

            if (configAuthorityInfo == null)
            {
                throw new ArgumentNullException(nameof(configAuthorityInfo));
            }

            ValidateTypeMismatch(configAuthorityInfo, requestAuthorityInfo);

            await ValidateSameHostAsync(requestAuthorityInfo, requestContext).ConfigureAwait(false);

            switch (configAuthorityInfo.AuthorityType)
            {
                // ADFS is tenant-less, no need to consider tenant
                case AuthorityType.Adfs:
                    return requestAuthorityInfo == null ?
                        new AdfsAuthority(configAuthorityInfo) :
                        new AdfsAuthority(requestAuthorityInfo);

                case AuthorityType.B2C:

                    if (requestAuthorityInfo != null)
                    {
                        return new B2CAuthority(requestAuthorityInfo);
                    }
                    return new B2CAuthority(configAuthorityInfo);

                case AuthorityType.Aad:

                    bool updateEnvironment = requestContext.ServiceBundle.Config.MultiCloudSupportEnabled && account != null;

                    if (requestAuthorityInfo == null)
                    {
                        return updateEnvironment ?
                            CreateAuthorityWithTenant(CreateAuthorityWithEnvironment(configAuthorityInfo, account.Environment).AuthorityInfo, account?.HomeAccountId?.TenantId) :
                            CreateAuthorityWithTenant(configAuthorityInfo, account?.HomeAccountId?.TenantId);
                    }

                    // In case the authority is defined only at the request level
                    if (configAuthorityInfo.IsDefaultAuthority &&
                        requestAuthorityInfo.AuthorityType != AuthorityType.Aad)
                    {
                        return CreateAuthority(requestAuthorityInfo);
                    }

                    var requestAuthority = updateEnvironment ? 
                        new AadAuthority(CreateAuthorityWithEnvironment(requestAuthorityInfo, account?.Environment).AuthorityInfo) :
                        new AadAuthority(requestAuthorityInfo);
                    if (!requestAuthority.IsCommonOrganizationsOrConsumersTenant())
                    {
                        return requestAuthority;
                    }

                    return updateEnvironment ?
                            CreateAuthorityWithTenant(CreateAuthorityWithEnvironment(configAuthorityInfo, account.Environment).AuthorityInfo, account?.HomeAccountId?.TenantId) :
                            CreateAuthorityWithTenant(configAuthorityInfo, account?.HomeAccountId?.TenantId);

                default:
                    throw new MsalClientException(
                        MsalError.InvalidAuthorityType,
                        "Unsupported authority type");
            }
        }

        private static void ValidateTypeMismatch(AuthorityInfo configAuthorityInfo, AuthorityInfo requestAuthorityInfo)
        {
            if (!configAuthorityInfo.IsDefaultAuthority &&
                            requestAuthorityInfo != null &&
                            configAuthorityInfo.AuthorityType != requestAuthorityInfo.AuthorityType)
            {
                throw new MsalClientException(
                    MsalError.AuthorityTypeMismatch,
                    MsalErrorMessage.AuthorityTypeMismatch(
                        configAuthorityInfo.AuthorityType,
                        requestAuthorityInfo.AuthorityType));
            }
        }

        public static Authority CreateAuthority(string authority, bool validateAuthority = false)
        {
            return CreateAuthority(AuthorityInfo.FromAuthorityUri(authority, validateAuthority));
        }

        public static Authority CreateAuthority(AuthorityInfo authorityInfo)
        {
            switch (authorityInfo.AuthorityType)
            {
                case AuthorityType.Adfs:
                    return new AdfsAuthority(authorityInfo);

                case AuthorityType.B2C:
                    return new B2CAuthority(authorityInfo);

                case AuthorityType.Aad:
                    return new AadAuthority(authorityInfo);

                default:
                    throw new MsalClientException(
                        MsalError.InvalidAuthorityType,
                        $"Unsupported authority type {authorityInfo.AuthorityType}");
            }
        }

        internal static Authority CreateAuthorityWithTenant(AuthorityInfo authorityInfo, string tenantId)
        {
            Authority initialAuthority = CreateAuthority(authorityInfo);

            if (string.IsNullOrEmpty(tenantId))
            {
                return initialAuthority;
            }

            string tenantedAuthority = initialAuthority.GetTenantedAuthority(tenantId);

            return CreateAuthority(tenantedAuthority, authorityInfo.ValidateAuthority);
        }

        internal static Authority CreateAuthorityWithEnvironment(AuthorityInfo authorityInfo, string environment)
        {
            var uriBuilder = new UriBuilder(authorityInfo.CanonicalAuthority)
            {
                Host = environment
            };

            return CreateAuthority(uriBuilder.Uri.AbsoluteUri, authorityInfo.ValidateAuthority);
        }

        #endregion Builders

        #region Abstract
        internal abstract string TenantId { get; }

        /// <summary>
        /// Gets a tenanted authority if the current authority is tenant-less.
        /// Returns the original authority on B2C and ADFS
        /// </summary>
        internal abstract string GetTenantedAuthority(string tenantId, bool forceTenantless = false);

        internal abstract string GetTokenEndpoint();

        internal abstract string GetAuthorizationEndpoint();

        internal abstract string GetDeviceCodeEndpoint();
        #endregion

        private static async Task ValidateSameHostAsync(AuthorityInfo requestAuthorityInfo, RequestContext requestContext)
        {
            var configAuthorityInfo = requestContext.ServiceBundle.Config.Authority.AuthorityInfo;

            if (!requestContext.ServiceBundle.Config.MultiCloudSupportEnabled && 
                requestAuthorityInfo != null &&
                !string.Equals(requestAuthorityInfo.Host, configAuthorityInfo.Host, StringComparison.OrdinalIgnoreCase))
            {
                if (requestAuthorityInfo.AuthorityType == AuthorityType.B2C)
                {
                    throw new MsalClientException(MsalError.B2CAuthorityHostMismatch, MsalErrorMessage.B2CAuthorityHostMisMatch);
                }

                // This check should be done when validating the request parameters, however we've allowed
                // this configuration to run for a while, so this is the better place for it.
                bool usesRegional = !string.IsNullOrEmpty(requestContext.ServiceBundle.Config.AzureRegion);
                if (usesRegional)
                {
                    throw new MsalClientException(MsalError.RegionalAndAuthorityOverride, MsalErrorMessage.RegionalAndAuthorityOverride);
                }

                var authorityAliased = await IsAuthorityAliasedAsync(requestContext, requestAuthorityInfo).ConfigureAwait(false);
                if (authorityAliased)
                {
                    return;
                }

                if (configAuthorityInfo.IsDefaultAuthority)
                {
                    throw new MsalClientException(
                        MsalError.AuthorityHostMismatch,
                        $"You did not define an authority at the application level, so it defaults to the https://login.microsoftonline.com/common. " +
                        $"\n\rHowever, the request is for a different cloud {requestAuthorityInfo.Host}. This is not supported - the app and the request must target the same cloud. " +
                        $"\n\r\n\r Add .WithAuthority(\"https://{requestAuthorityInfo.Host}/common\") in the app builder. " +
                        $"\n\rSee https://aka.ms/msal-net-authority-override for details");
                }

                throw new MsalClientException(
                    MsalError.AuthorityHostMismatch,
                    $"\n\r The application is configured for cloud {configAuthorityInfo.Host} and the request for a different cloud - {requestAuthorityInfo.Host}. This is not supported - the app and the request must target the same cloud. " +
                    $"\n\rSee https://aka.ms/msal-net-authority-override for details");
            }
        }

        private static async Task<bool> IsAuthorityAliasedAsync(RequestContext requestContext, AuthorityInfo requestAuthorityInfo)
        {
            var instanceDiscoveryManager = requestContext.ServiceBundle.InstanceDiscoveryManager;
            var result = await instanceDiscoveryManager.GetMetadataEntryAsync(requestContext.ServiceBundle.Config.Authority.AuthorityInfo, requestContext).ConfigureAwait(false);

            return result.Aliases.Any(alias => alias.Equals(requestAuthorityInfo.Host));
        }

        internal static string GetEnvironment(string authority)
        {
            return new Uri(authority).Host;
        }
    }
}
